using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Orchestrates wave start/complete signalling. Phase 1 stub: the actual spawning logic
    /// is implemented in Phase 2, which will replace the ScriptableObject list element type
    /// with the concrete WaveData type.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        // Phase 2 will replace ScriptableObject with the real WaveData type.
        [SerializeField] private List<ScriptableObject> waveDataList = new List<ScriptableObject>();

        public int TotalWaves => waveDataList != null ? waveDataList.Count : 0;
        public int CurrentWaveIndex { get; private set; } = -1;
        public bool IsSpawning { get; private set; }

        public static event Action<int> OnWaveStart;
        public static event Action<int> OnWaveComplete;
        public static event Action OnAllWavesComplete;

        private Coroutine spawnRoutine;

        /// <summary>
        /// Advance to the next wave. If no waves remain, fires OnAllWavesComplete and
        /// reports victory through the GameManager.
        /// </summary>
        public void StartNextWave()
        {
            CurrentWaveIndex++;

            if (CurrentWaveIndex >= TotalWaves)
            {
                OnAllWavesComplete?.Invoke();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Victory();
                }
                return;
            }

            OnWaveStart?.Invoke(CurrentWaveIndex);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetWave(CurrentWaveIndex + 1);
            }

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }
            spawnRoutine = StartCoroutine(SpawnCoroutine());
        }

        /// <summary>
        /// Phase 1 stub. Phase 2 fills this in with per-wave enemy spawning.
        /// </summary>
        private IEnumerator SpawnCoroutine()
        {
            IsSpawning = true;
            // Stub: yield once and immediately fire wave-complete so the state machine is wired end-to-end.
            yield return null;
            IsSpawning = false;

            OnWaveComplete?.Invoke(CurrentWaveIndex);
        }
    }
}
