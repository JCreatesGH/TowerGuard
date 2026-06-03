using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.UI;

namespace TowerGuard.Gameplay
{
    /// <summary>
    /// Phase 5 mechanic — Kill Combo.
    /// Tracks GameManager.OnEnemyKilled timestamps in a 3-second rolling window.
    /// Crossing 5/10/15 within the window pops a "COMBO!" / "RAMPAGE!" / "UNSTOPPABLE!"
    /// toast and awards bonus soft currency. Each tier only triggers once per window.
    /// </summary>
    public class KillComboManager : MonoBehaviour
    {
        public static KillComboManager Instance { get; private set; }

        [SerializeField] private float windowSeconds = 3f;
        [SerializeField] private RectTransform comboLabelRoot;
        [SerializeField] private TMP_Text comboLabelText;
        [SerializeField] private CanvasGroup comboLabelCanvasGroup;
        [Tooltip("Optional: full-screen white image whose alpha is briefly raised on a 5+ kill combo.")]
        [SerializeField] private Image flashImage;
        [SerializeField] private AudioClip comboClip;

        private readonly Queue<float> killTimes = new Queue<float>();
        private int lastTier;

        private static readonly (int threshold, string label, Color color, float scale)[] Tiers =
        {
            (5,  "COMBO!",      new Color(1f, 0.84f, 0.0f, 1f),    1.0f), // gold
            (10, "RAMPAGE!",    new Color(1f, 0.42f, 0.0f, 1f),    1.2f), // orange
            (15, "UNSTOPPABLE!",new Color(0.88f, 0.25f, 0.96f, 1f),1.45f),// purple
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()  { GameManager.OnEnemyKilled += OnEnemyKilled; }
        private void OnDisable() { GameManager.OnEnemyKilled -= OnEnemyKilled; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            float now = Time.time;
            while (killTimes.Count > 0 && now - killTimes.Peek() > windowSeconds)
            {
                killTimes.Dequeue();
            }
            // Reset tier when the window decays back below the lowest threshold.
            if (killTimes.Count < Tiers[0].threshold) lastTier = 0;
        }

        private void OnEnemyKilled()
        {
            killTimes.Enqueue(Time.time);

            // Check tiers from highest to lowest so 15 doesn't fire 5/10 first.
            for (int i = Tiers.Length - 1; i >= 0; i--)
            {
                int tierIndex = i + 1;
                if (killTimes.Count >= Tiers[i].threshold && lastTier < tierIndex)
                {
                    lastTier = tierIndex;
                    TriggerCombo(Tiers[i].label, Tiers[i].color, Tiers[i].scale);
                    break;
                }
            }
        }

        private void TriggerCombo(string label, Color color, float scale)
        {
            // Bonus soft currency (each tier rewards a fixed +5).
            if (GameManager.Instance != null) GameManager.Instance.EarnSoftCurrency(5);

            if (comboClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(comboClip);
            }

            // Mid-screen pop label.
            if (comboLabelRoot != null && comboLabelText != null)
            {
                comboLabelText.text = label;
                comboLabelText.color = color;
                comboLabelRoot.gameObject.SetActive(true);
                comboLabelRoot.localScale = Vector3.zero;
                LeanTween.scale(comboLabelRoot, Vector3.one * scale, 0.18f).setEaseOutBack().setIgnoreTimeScale(true);
                StartCoroutine(FloatAndHide(comboLabelRoot));
            }

            // Brief white vignette flash via Image alpha.
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                flashImage.color = new Color(1f, 1f, 1f, 0f);
                LeanTween.value(flashImage.gameObject, a => flashImage.color = new Color(1f, 1f, 1f, a), 0f, 0.35f, 0.1f)
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        LeanTween.value(flashImage.gameObject, a => flashImage.color = new Color(1f, 1f, 1f, a), 0.35f, 0f, 0.3f)
                            .setIgnoreTimeScale(true)
                            .setOnComplete(() => flashImage.gameObject.SetActive(false));
                    });
            }

            // Toast for accessibility / non-flash UI.
            if (UIManager.Instance != null) UIManager.Instance.ShowToast(label + " +5", 1.2f);
        }

        private IEnumerator FloatAndHide(RectTransform rt)
        {
            Vector2 start = rt.anchoredPosition;
            Vector2 end   = start + new Vector2(0f, 60f);
            float t = 0f;
            float dur = 0.7f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                rt.anchoredPosition = Vector2.Lerp(start, end, u);
                if (comboLabelCanvasGroup != null)
                {
                    comboLabelCanvasGroup.alpha = 1f - u;
                }
                yield return null;
            }
            rt.gameObject.SetActive(false);
            rt.anchoredPosition = start;
            if (comboLabelCanvasGroup != null) comboLabelCanvasGroup.alpha = 1f;
        }
    }
}
