using UnityEngine;
using TowerGuard.Core;
using TowerGuard.Towers;

namespace TowerGuard.Utils
{
    /// <summary>
    /// Phase 2 verify / play-test HUD. OnGUI-based so it needs zero Canvas wiring.
    /// Shows HP, soft/hard currency, wave index, and offers buttons to start the
    /// next wave and pick a tower to place. Safe to remove once the real UICanvas
    /// is built out in a later phase.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] private TowerData basic;
        [SerializeField] private TowerData sniper;
        [SerializeField] private TowerData slow;
        [SerializeField] private TowerData area;

        private WaveManager waveManager;

        private void Start()
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        private void OnGUI()
        {
            const int pad = 10;
            const int lineH = 22;
            GUI.Box(new Rect(pad, pad, 240, 150), "TowerGuard — Debug");

            int y = pad + 22;
            if (GameManager.Instance != null)
            {
                GUI.Label(new Rect(pad + 10, y, 220, lineH), $"HP: {GameManager.Instance.PlayerHP}");
                y += lineH;
                GUI.Label(new Rect(pad + 10, y, 220, lineH), $"Soft: {GameManager.Instance.SoftCurrency}   Hard: {GameManager.Instance.HardCurrency}");
                y += lineH;
                GUI.Label(new Rect(pad + 10, y, 220, lineH), $"Wave: {GameManager.Instance.CurrentWave}   State: {GameManager.Instance.CurrentState}");
                y += lineH;
            }

            if (GUI.Button(new Rect(pad + 10, y, 220, lineH + 4), "Start Next Wave"))
            {
                if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
                if (waveManager != null) waveManager.StartNextWave();
            }

            // Tower picker row.
            int bx = pad + 260;
            int bw = 110;
            TowerButton(new Rect(bx,            pad + 22, bw, lineH + 4), basic,  "Basic 50");
            TowerButton(new Rect(bx + bw + 4,   pad + 22, bw, lineH + 4), sniper, "Sniper 100");
            TowerButton(new Rect(bx,            pad + 22 + lineH + 8, bw, lineH + 4), slow,  "Slow 75");
            TowerButton(new Rect(bx + bw + 4,   pad + 22 + lineH + 8, bw, lineH + 4), area,  "Area 125");
        }

        private void TowerButton(Rect r, TowerData data, string label)
        {
            GUI.enabled = data != null;
            if (GUI.Button(r, label) && TowerPlacement.Instance != null)
            {
                TowerPlacement.Instance.SelectTowerType(data);
            }
            GUI.enabled = true;
        }
    }
}
