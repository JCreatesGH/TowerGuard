using UnityEngine;
using UnityEngine.UI;
using TowerGuard.Core;

namespace TowerGuard.UI
{
    /// <summary>
    /// Wires the two always-present buttons on the Game Over panel. The slide-in
    /// animation and the rewarded-ad follow-up are driven by UIManager.ShowGameOver.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnDisable()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }

        private void OnRetry()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }

        private void OnMainMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }
    }
}
