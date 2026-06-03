# TowerGuard — Phase 1 Drop-in Package

This folder mirrors what should land inside the Unity project at `~/Desktop/TowerGuard`
once Unity Hub has created it with the **2D (URP)** template. Everything under `Assets/`
and the `Packages/manifest.json` snippet is ready to copy in.

## What's inside

```
Assets/
  Scripts/Core/          # 5 Phase-1 scripts, all in the TowerGuard.Core namespace
    GameManager.cs
    WaveManager.cs
    AudioManager.cs
    PathManager.cs
    ObjectPool.cs        # Defines ObjectPool + ObjectPoolRegistry (MonoBehaviour host)
  Editor/
    CreatePlaceholderSprites.cs   # Menu: Tools > Create Placeholder Sprites
  Sprites/Enemies/       # 4 pre-generated PNGs (Basic/Fast/Tank/Boss)
  Sprites/Towers/        # 4 tower PNGs + 3 projectile PNGs
  (empty subfolders for Scenes, Prefabs, Sprites/UI, etc. — Unity will pick them up)
Packages/
  manifest.json          # Packages for Step 2 (Input System, Cinemachine, TMP, Tilemap,
                         # Ads, IAP, URP, etc.). LeanTween is NOT included — it is an
                         # Asset Store package that must be imported manually.
```

## Manual Unity GUI steps still required

Computer-use access could not be granted in this session (three `request_access` timeouts),
so the Unity Editor work could not be automated. The following still needs to be done in
Unity after you create the project:

1. **Create project** — Unity Hub > New project > **2D (URP)**, name `TowerGuard`,
   location `~/Desktop`. Wait for initial import.
2. **Drop in files** — copy `Assets/` from this package over the project's `Assets/`.
   Either replace `Packages/manifest.json` or merge the new dependencies into yours.
3. **TextMeshPro essentials** — first time TMP is referenced, Unity pops up an import
   dialog. Click **Import TMP Essentials**.
4. **LeanTween** — open the Asset Store page in your browser
   (https://assetstore.unity.com/packages/tools/animation/leantween-3595),
   add it to My Assets, then Window > Package Manager > My Assets > Download > Import.
5. **iOS platform + Player Settings** — see the checklist in `BUILD_LOG.md`.
6. **Scenes** — see the checklist in `BUILD_LOG.md`. The scene files are NOT included
   because hand-authored .unity YAML with custom component GUIDs is too risky to ship
   without Unity's own serializer.
7. **Generate placeholder sprites** — `Tools > Create Placeholder Sprites` will emit
   the PNGs. They're ALSO pre-bundled under `Assets/Sprites/` so the step is optional.
8. **Verify** — Console clean, iOS active, 3 scenes in Build Settings, Play Level_01.
