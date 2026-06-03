# BUILD_LOG.md — TowerGuard Phase 1

**Phase 1 — PARTIAL (file deliverables complete, Unity Editor steps pending)**

Date: 2026-04-23
Unity version: not captured (Unity Editor was never launched in this session)

## Summary

Phase 1 splits into two halves: (A) on-disk deliverables — folder structure, C# scripts,
editor tool, placeholder sprites, and package manifest — and (B) Unity Editor GUI work —
create project, install packages, switch to iOS, set Player Settings, build three scenes,
and verify Play mode. Half (A) is fully complete and delivered. Half (B) could not be
executed: three consecutive `request_access` calls for Unity Hub timed out after 60 s
each, so computer-use control of the Mac desktop was never granted.

Because Unity Hub refuses to create a project inside a non-empty directory, nothing was
written to `~/Desktop/TowerGuard` directly. Instead, everything is staged at
**`~/Desktop/TowerGuard_DropIn`** ready to copy into the project once it exists.

## What shipped on disk

Location: `~/Desktop/TowerGuard_DropIn/`

```
Assets/
  Editor/
    CreatePlaceholderSprites.cs        # Tools > Create Placeholder Sprites
  Scripts/Core/
    GameManager.cs                     # Singleton, DontDestroyOnLoad, state machine, currency, events
    WaveManager.cs                     # TotalWaves, CurrentWaveIndex, StartNextWave, SpawnCoroutine stub
    AudioManager.cs                    # Pooled SFX + music source, PlayerPrefs-backed volumes
    PathManager.cs                     # Waypoints[] + yellow OnDrawGizmos
    ObjectPool.cs                      # ObjectPool + ObjectPoolRegistry (MonoBehaviour host)
  Sprites/
    Enemies/ Enemy_Basic.png Enemy_Fast.png Enemy_Tank.png Enemy_Boss.png
    Towers/  Tower_Basic.png Tower_Sniper.png Tower_Slow.png Tower_Area.png
             Projectile_Bullet.png Projectile_Laser.png Projectile_AOE.png
  Scenes/ Prefabs/... Audio/... ScriptableObjects/... Animations/ Tilemaps/ Resources/
                                       # all Step 4 subfolders present, .keep files mark the empty ones
Packages/
  manifest.json                        # Input System, Cinemachine, TMP, 2D Tilemap Editor,
                                       # Unity Ads, Unity IAP, URP. LeanTween not included (Asset Store).
PACKAGE_NOTES.md                       # How to apply this drop-in
```

All 11 placeholder PNGs were **pre-generated via Python/PIL** so they exist on disk
right now. Running `Tools > Create Placeholder Sprites` in Unity would regenerate them
identically; it is kept as a script for Phase 2+ iteration.

## Script design notes

- All five Phase-1 scripts live in the `TowerGuard.Core` namespace.
- `GameManager` exposes every field and event from the spec. `EnemyReachedEnd()` never
  drives `PlayerHP` below zero and triggers `GameOver()` exactly once. A `StartNewGame()`
  helper resets HP/currency/wave to the Step-5 starting values; `RestartGame()` calls it
  before reloading the scene so counters don't leak across runs.
- `WaveManager.waveDataList` is typed `List<ScriptableObject>` per spec. The Phase-2 swap
  to the real `WaveData` type is one line.
- `AudioManager` steals the oldest SFX source if the pool is saturated — documented in
  the method body — so `PlaySFX` is never a no-op when something is always playing.
- `ObjectPool` is intentionally NOT a MonoBehaviour. The MonoBehaviour host is a separate
  class `ObjectPoolRegistry` which owns `public static Dictionary<string, ObjectPool> Pools`.
  Splitting the pure-C# pool from the scene host means systems can hold pool references
  without needing a scene reference.
- The editor script is wrapped in `#if UNITY_EDITOR` and lives in `Assets/Editor/`
  (Unity's reserved Editor assembly folder) so it never ships in an iOS build.

## Assumptions and substitutions

1. **Access not granted.** `request_access` for Unity Hub timed out three times in a row.
   Rather than keep retrying a 60-second request that the user wasn't seeing/approving,
   the work was redirected to pure file delivery. No desktop automation was performed.
2. **No hand-authored `.unity` scene files.** Scene YAML with custom component GUIDs is
   brittle to hand-author correctly across Unity versions — a malformed scene file
   produces silent data loss inside Unity. Scenes are therefore left for the GUI pass.
3. **No hand-authored `ProjectSettings/*.asset`.** The iOS Player Settings change requires
   platform-aware serialization that Unity handles via its own switch-platform pipeline.
   These remain GUI steps.
4. **Pre-generated PNGs.** Delivered as real PNG files using Pillow rather than relying
   on the editor script to produce them after the project exists. The editor script is
   still included — it's needed in Phase 2+ if any color/sizing changes are requested.
5. **Package versions in `manifest.json`.** Pinned to versions that are verified-compatible
   with Unity 2022.3 LTS. If the installed Unity Editor is a different version, the
   Package Manager will resolve to the closest compatible version on first open.
6. **LeanTween.** Included as a comment in `PACKAGE_NOTES.md` only. LeanTween ships from
   the Asset Store, not the UPM registry, so no line in `manifest.json` can install it.

## Unity Editor steps still required

These need to be done inside the Unity Editor. The drop-in package is designed to make
each step fast.

### A. Create the project (Step 1)
- Unity Hub > **New project** > Template **2D (URP)**.
- Name: `TowerGuard`  Location: `~/Desktop`  > Create Project.
- Wait for the initial reimport to finish (one-time, can be several minutes).

### B. Merge the drop-in (Steps 4 + 5 + 7)
- Copy `~/Desktop/TowerGuard_DropIn/Assets/` over the project's `Assets/`. Unity will
  detect new scripts and recompile; the TMP Essentials import popup appears once.
  Click **Import TMP Essentials**.
- Replace (or merge into) `Packages/manifest.json` with the version in the drop-in, then
  focus the Unity window — UPM re-resolves automatically.

### C. Install LeanTween (Step 2)
- https://assetstore.unity.com/packages/tools/animation/leantween-3595 → **Add to My Assets**.
- Window > Package Manager > **Packages: My Assets** > LeanTween > Download > Import.

### D. Platform + Player Settings (Step 3)
- File > Build Settings > select **iOS** > **Switch Platform**. Wait for reimport.
- Edit > Project Settings > Player > iOS tab:
  - Bundle Identifier: `com.towerguard.game`
  - Version: `1.0.0`    Build: `1`    Display Name: `Tower Guard`
  - Other Settings > Target minimum iOS Version: `16.0`
  - Architecture: `ARM64`
  - Scripting Backend: `IL2CPP`    Managed Stripping Level: `Medium`    Strip Engine Code: **ON**
  - Resolution and Presentation > Default Orientation: `Portrait`; disable all Auto Rotation checkboxes
  - Allow downloads over HTTP: **Not Allowed**    Requires Fullscreen: **ON**    Enable Bitcode: **OFF**
- Edit > Project Settings > Quality > iOS column > set default to the **Medium** row.

### E. Scenes (Step 6)
Create each scene via File > New Scene > Basic 2D, save into `Assets/Scenes/`.

**MainMenu.unity** — Main Camera (Clear Flags: Solid Color, Background `#1A1A2E`),
empty `GameManager` with `Scripts/Core/GameManager.cs`, empty `AudioManager` with
`Scripts/Core/AudioManager.cs`.

**LevelSelect.unity** — Main Camera with the same settings. (No managers required in
the spec — they re-spawn from MainMenu via `DontDestroyOnLoad`.)

**Level_01.unity** — Main Camera (Orthographic, Size `6`, Background `#1A1A2E`),
empty GameObjects `GameManager`, `WaveManager`, `AudioManager`, `PathManager`,
`UICanvas` (Canvas: Render Mode `Screen Space - Overlay`; CanvasScaler: UI Scale Mode
`Scale With Screen Size`, Reference Resolution `390 × 844`, Match `0.5`), and a `Grid`
GameObject with three Tilemap children named `Tilemap_Ground`, `Tilemap_Path`,
`Tilemap_Obstacles`. Attach the matching script to each manager GameObject.

Then File > Build Settings > **Add Open Scenes** for each, with the order:
`0 MainMenu`, `1 LevelSelect`, `2 Level_01`.

### F. Placeholder sprites (Step 7, optional)
The PNGs are already on disk. If you want to regenerate them, run
**Tools > Create Placeholder Sprites**.

### G. Verify (Step 8)
- Console shows zero errors and zero warnings about missing scripts.
- Build Settings shows iOS active and three scenes in the correct order.
- Open `Level_01.unity`, press Play, let it run 10 seconds — no exceptions.

When all of G passes, replace this log's top line with
`**Phase 1 — COMPLETE**` and fill in the Unity version from Unity > About Unity.
