using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerGuard.UI
{
    /// <summary>
    /// Level Select scene controller. Back button returns to MainMenu.
    /// Each LevelCardUI drives its own scene load and star-row update.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private string mainMenuScene = "MainMenu";

        private void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
        }

        private void OnBack()
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
