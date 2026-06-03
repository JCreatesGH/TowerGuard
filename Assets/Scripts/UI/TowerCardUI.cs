using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Towers;

namespace TowerGuard.UI
{
    /// <summary>
    /// A single tower-picker card in the bottom bar. Shows icon, name, cost.
    /// Disabled-overlay is toggled based on whether the player can afford the tower.
    /// Clicking the card routes to TowerPlacement.SelectTowerType.
    /// </summary>
    public class TowerCardUI : MonoBehaviour
    {
        [SerializeField] private TowerData towerData;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button button;
        [SerializeField] private GameObject disabledOverlay;
        [SerializeField] private GameObject selectedHighlight;

        public TowerData Data => towerData;

        public void SetTowerData(TowerData data)
        {
            towerData = data;
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnTapped);
            }
            if (GameManager.Instance != null)
            {
                RefreshAffordability(GameManager.Instance.SoftCurrency);
            }
        }

        private void Refresh()
        {
            if (towerData == null) return;
            if (iconImage != null) iconImage.sprite = towerData.icon;
            if (nameText != null) nameText.text = towerData.towerName;
            if (costText != null) costText.text = towerData.cost.ToString();
        }

        public void RefreshAffordability(int softCurrency)
        {
            if (towerData == null) return;
            bool canAfford = softCurrency >= towerData.cost;
            if (disabledOverlay != null) disabledOverlay.SetActive(!canAfford);
            if (button != null) button.interactable = canAfford;
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        }

        private void OnTapped()
        {
            if (towerData == null || TowerPlacement.Instance == null) return;
            TowerPlacement.Instance.SelectTowerType(towerData);
        }
    }
}
