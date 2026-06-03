using UnityEngine;
using UnityEngine.UI;
using TowerGuard.Core;

namespace TowerGuard.UI
{
    /// <summary>
    /// Wires the three pause-menu buttons (Resume / Restart / Main Menu) and the
    /// inline SettingsPanel. Also owns the "confirm restart" mini-overlay.
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button confirmRestartYesButton;
        [SerializeField] private Button confirmRestartNoButton;
        [SerializeField] private GameObject confirmRestartOverlay;
        [SerializeField] private SettingsPanel inlineSettingsPanel;

        private void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (confirmRestartYesButton != null) confirmRestartYesButton.onClick.AddListener(ConfirmRestart);
            if (confirmRestartNoButton != null) confirmRestartNoButton.onClick.AddListener(CancelRestart);
            if (confirmRestartOverlay != null) confirmRestartOverlay.SetActive(false);
            // The inline settings panel is reachable but NOT auto-shown on pause. Auto-showing
            // it would cover the Resume/Restart/MainMenu buttons. Settings is opened on demand
            // (e.g. via a future Settings button); when not in use it must stay fully hidden,
            // including the dim outer SettingsOverlay parent which would block raycasts.
            if (inlineSettingsPanel != null)
            {
                var outer = inlineSettingsPanel.transform.parent;
                if (outer != null) outer.gameObject.SetActive(false);
                inlineSettingsPanel.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
            if (confirmRestartYesButton != null) confirmRestartYesButton.onClick.RemoveListener(ConfirmRestart);
            if (confirmRestartNoButton != null) confirmRestartNoButton.onClick.RemoveListener(CancelRestart);
        }

        private void OnResume()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            if (UIManager.Instance != null) UIManager.Instance.HidePause();
        }

        private void OnRestart()
        {
            if (confirmRestartOverlay != null) confirmRestartOverlay.SetActive(true);
        }

        private void ConfirmRestart()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }

        private void CancelRestart()
        {
            if (confirmRestartOverlay != null) confirmRestartOverlay.SetActive(false);
        }

        private void OnMainMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }
    }
}
