using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerGuard.Core
{
    /// <summary>
    /// Global game state singleton. Persists across scenes via DontDestroyOnLoad.
    /// Holds the authoritative HP, currency, wave, and run state for the current session.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public int PlayerHP { get; private set; } = 20;
        public int SoftCurrency { get; private set; } = 150;
        public int HardCurrency { get; private set; } = 0;
        public int CurrentWave { get; private set; } = 0;

        // Per-run stats (reset by StartNewGame, read by the Phase 3 Victory screen).
        public int EnemiesDefeatedThisRun { get; private set; } = 0;
        public int TotalSoftCurrencyEarnedThisRun { get; private set; } = 0;

        // Static events for UI / systems to subscribe to without direct references.
        public static event Action<int> OnHPChanged;
        public static event Action<int> OnSoftCurrencyChanged;
        public static event Action<int> OnHardCurrencyChanged;
        public static event Action<int> OnWaveChanged;
        public static event Action OnGameOver;
        public static event Action OnVictory;
        /// <summary>Phase 5: fired whenever an enemy dies. KillComboManager listens.</summary>
        public static event Action OnEnemyKilled;

        /// <summary>Encapsulated trigger so callers don't poke the static event directly.</summary>
        public static void NotifyEnemyKilled() => OnEnemyKilled?.Invoke();

        private const int StartingHP = 20;
        private const int StartingSoftCurrency = 150;
        private const int StartingHardCurrency = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ----- Currency -----

        public void EarnSoftCurrency(int amount)
        {
            if (amount <= 0) return;
            SoftCurrency += amount;
            TotalSoftCurrencyEarnedThisRun += amount;
            OnSoftCurrencyChanged?.Invoke(SoftCurrency);
        }

        /// <summary>Called by EnemyBase on kill so the Victory screen can show run totals.</summary>
        public void NoteEnemyDefeated()
        {
            EnemiesDefeatedThisRun += 1;
        }

        public bool SpendSoftCurrency(int amount)
        {
            if (amount <= 0) return true;
            if (SoftCurrency < amount) return false;
            SoftCurrency -= amount;
            OnSoftCurrencyChanged?.Invoke(SoftCurrency);
            return true;
        }

        public void EarnHardCurrency(int amount)
        {
            if (amount <= 0) return;
            HardCurrency += amount;
            OnHardCurrencyChanged?.Invoke(HardCurrency);
        }

        public bool SpendHardCurrency(int amount)
        {
            if (amount <= 0) return true;
            if (HardCurrency < amount) return false;
            HardCurrency -= amount;
            OnHardCurrencyChanged?.Invoke(HardCurrency);
            return true;
        }

        // ----- Combat / Wave state -----

        public void EnemyReachedEnd()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Victory)
                return;

            PlayerHP -= 1;
            if (PlayerHP < 0) PlayerHP = 0;
            OnHPChanged?.Invoke(PlayerHP);

            if (PlayerHP <= 0)
            {
                GameOver();
            }
        }

        public void SetWave(int wave)
        {
            CurrentWave = wave;
            OnWaveChanged?.Invoke(CurrentWave);
        }

        /// <summary>
        /// Force-set the player's HP. Used by the rewarded-ad continue flow
        /// (Phase 4) to grant N HP after a successful ad-watch. Clamps to 1+
        /// so the call can't accidentally re-trigger GameOver.
        /// </summary>
        public void SetHP(int hp)
        {
            PlayerHP = Mathf.Max(1, hp);
            OnHPChanged?.Invoke(PlayerHP);
        }

        // ----- Run lifecycle -----

        public void GameOver()
        {
            if (CurrentState == GameState.GameOver) return;
            CurrentState = GameState.GameOver;
            OnGameOver?.Invoke();
        }

        public void Victory()
        {
            if (CurrentState == GameState.Victory) return;
            CurrentState = GameState.Victory;
            OnVictory?.Invoke();
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void StartNewGame()
        {
            PlayerHP = StartingHP;
            SoftCurrency = StartingSoftCurrency;
            HardCurrency = StartingHardCurrency;
            CurrentWave = 0;
            EnemiesDefeatedThisRun = 0;
            TotalSoftCurrencyEarnedThisRun = 0;
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            OnHPChanged?.Invoke(PlayerHP);
            OnSoftCurrencyChanged?.Invoke(SoftCurrency);
            OnHardCurrencyChanged?.Invoke(HardCurrency);
            OnWaveChanged?.Invoke(CurrentWave);
        }

        /// <summary>
        /// Restore HP and return to the Playing state after a GameOver. Used by the
        /// Phase 3 rewarded-ad continue flow. Keeps currency, wave, and per-run stats
        /// intact; only HP and run state change.
        /// </summary>
        public void ContinueAfterDeath(int restoredHP)
        {
            if (CurrentState != GameState.GameOver) return;
            PlayerHP = Mathf.Max(1, restoredHP);
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            OnHPChanged?.Invoke(PlayerHP);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            StartNewGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.MainMenu;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
