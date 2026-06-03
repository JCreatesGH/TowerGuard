using UnityEngine;

namespace TowerGuard.Utils
{
    /// <summary>
    /// 1x/2x time-scale toggle with explicit Pause / Resume. Meant to be called by UI buttons.
    /// Note: GameManager also flips Time.timeScale in PauseGame/ResumeGame. Treat this script
    /// as the user-facing speed toggle and keep the game-state pauses in GameManager.
    /// </summary>
    public class SpeedController : MonoBehaviour
    {
        public static SpeedController Instance { get; private set; }

        public bool IsFast { get; private set; }
        private float savedScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ToggleSpeed()
        {
            IsFast = !IsFast;
            Time.timeScale = IsFast ? 2f : 1f;
        }

        public void SetFast(bool fast)
        {
            IsFast = fast;
            Time.timeScale = IsFast ? 2f : 1f;
        }

        public void Pause()
        {
            savedScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            Time.timeScale = savedScale > 0f ? savedScale : 1f;
        }
    }
}
