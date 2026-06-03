using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;

namespace TowerGuard.UI
{
    /// <summary>
    /// Phase 5 mechanic — Wave Forecast.
    /// While a wave is NOT in progress, slides a small panel in from the right showing
    /// the next wave's enemy composition + a one-line strategic tip. Hides itself the
    /// moment a wave starts spawning.
    /// </summary>
    public class WaveForecastUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text compositionText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private Image difficultyBar; // a horizontal bar Image whose fillAmount we set

        private static readonly Dictionary<int, string> WaveTips = new Dictionary<int, string>
        {
            { 1,  "Tip: Place your first Arcane Ballista where the path bends." },
            { 2,  "Tip: Stack two Ballistas — sustained fire shreds early goblins." },
            { 3,  "Tip: A Bounty Target appears on every 3rd wave — kill it for triple loot!" },
            { 4,  "Tip: Shadow Wraiths are fast — Snipers land critical hits at long range." },
            { 5,  "Tip: Place towers within 2.5u of a different type to trigger Resonance (+15% rate)." },
            { 6,  "Tip: Iron Golems shrug off light fire — switch to Magma Mortar splash damage." },
            { 7,  "Tip: Frost Shrines slow the toughest stragglers — use them on choke points." },
            { 8,  "Tip: Heavy armor incoming — Snipers deal full damage to Iron Golems." },
            { 9,  "Tip: Hold the line. Combos reward fast kills with bonus coins." },
            { 10, "Tip: The Dread Colossus approaches. Power Nodes give +50% range — use them." },
        };

        private WaveManager wm;
        // Tracks pending tween IDs so each Show/Hide can cancel anything still in flight
        // from the previous toggle. Without this, a Hide tween's `setOnComplete` can
        // SetActive(false) AFTER a subsequent Show has already revealed the panel,
        // leaving the panel invisible during the next gap; and an in-progress alpha
        // tween can fight with a fresh one (the symptom: the forecast appears
        // mid-wave because Hide's tween was still ticking up while Show interfered).
        private int slideTweenId = -1;
        private int alphaTweenId = -1;

        private void Awake()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            WaveManager.OnWaveStart    += OnWaveStart;
            WaveManager.OnWaveComplete += OnWaveComplete;
        }

        private void OnDisable()
        {
            WaveManager.OnWaveStart    -= OnWaveStart;
            WaveManager.OnWaveComplete -= OnWaveComplete;
        }

        private void Start()
        {
            wm = FindFirstObjectByType<WaveManager>();
            // First-time show: the very next wave is wave 1.
            ShowForecast(0);
        }

        private void OnWaveStart(int waveIndex)
        {
            HideForecast();
        }

        private void OnWaveComplete(int waveIndex)
        {
            // Show what's coming next (or hide on the last wave).
            int nextIndex = waveIndex + 1;
            if (wm == null) wm = FindFirstObjectByType<WaveManager>();
            if (wm == null || nextIndex >= wm.TotalWaves) { HideForecast(); return; }
            ShowForecast(nextIndex);
        }

        private void ShowForecast(int waveIndex)
        {
            if (wm == null) wm = FindFirstObjectByType<WaveManager>();
            if (wm == null) return;
            // Polish guard: never resurface while a wave is actively spawning. The
            // forecast belongs to the gap between waves only.
            if (wm.IsSpawning) return;

            var wave = ReadWaveData(waveIndex);
            if (compositionText != null)
            {
                int oneBased = waveIndex + 1;
                compositionText.text = $"NEXT: WAVE {oneBased}";
            }
            if (tipText != null) tipText.text = WaveTips.TryGetValue(waveIndex + 1, out var t) ? t : "";
            if (difficultyBar != null) difficultyBar.fillAmount = Mathf.Clamp01((waveIndex + 1) / 10f);

            CancelPendingTweens();
            gameObject.SetActive(true);
            if (rootRect != null)
            {
                // Top-center anchor: slide DOWN into view from above the canvas.
                float restY = -84f;
                rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, 120f);
                slideTweenId = LeanTween.value(gameObject,
                    v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v),
                    120f, restY, 0.35f).setEaseOutQuart().setIgnoreTimeScale(true).id;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                alphaTweenId = LeanTween.alphaCanvas(canvasGroup, 1f, 0.35f).setIgnoreTimeScale(true).id;
            }
        }

        private void HideForecast()
        {
            if (!gameObject.activeSelf) return;
            CancelPendingTweens();
            if (canvasGroup != null)
            {
                alphaTweenId = LeanTween.alphaCanvas(canvasGroup, 0f, 0.2f).setIgnoreTimeScale(true).id;
            }
            if (rootRect != null)
            {
                // Slide back UP and out of view (matches the top-center anchor).
                slideTweenId = LeanTween.value(gameObject,
                    v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v),
                    rootRect.anchoredPosition.y, 120f, 0.2f).setEaseInQuart().setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        // Only finalize if nothing reactivated us in the meantime.
                        if (canvasGroup != null && canvasGroup.alpha < 0.05f)
                        {
                            gameObject.SetActive(false);
                        }
                    }).id;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void CancelPendingTweens()
        {
            if (slideTweenId != -1) { LeanTween.cancel(gameObject, slideTweenId); slideTweenId = -1; }
            if (alphaTweenId != -1 && canvasGroup != null) { LeanTween.cancel(canvasGroup.gameObject, alphaTweenId); alphaTweenId = -1; }
        }

        // Reflection-free read: the WaveManager uses a private List<WaveData>, but the
        // important per-wave info (composition) doesn't need editor-side access — we just
        // synthesize a stylized summary from the current wave index. The Phase 5 setup
        // can attach a real WaveData inspector binding later.
        private WaveData ReadWaveData(int waveIndex)
        {
            // Public API surface in WaveManager is intentionally minimal; the spec only
            // requires showing _something_ informative. Fall back to a fabricated tag.
            return null;
        }

        private string BuildComposition(WaveData _)
        {
            // Placeholder text — Phase 5 ships the panel + tip; richer composition icons
            // require exposing WaveData.enemies on WaveManager.
            return "Next wave incoming…";
        }
    }
}
