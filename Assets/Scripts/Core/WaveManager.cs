using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Orchestrates wave start/complete signalling and the actual enemy-spawn loop.
    /// Enemies are fetched from the "Enemies" ObjectPool and placed at the first waypoint.
    /// A wave is considered complete when every spawned enemy has returned to the pool
    /// (either killed or reached the end), which we detect by counting active Enemy-tagged
    /// GameObjects in the scene.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private List<WaveData> waveDataList = new List<WaveData>();
        [SerializeField] private PathManager pathManager;
        [Tooltip("If true, the next wave starts automatically a few seconds after the previous one completes. Leave OFF to require a UI button press.")]
        [SerializeField] private bool autoAdvanceWaves = false;
        [SerializeField] private float autoAdvanceDelay = 3f;
        [Tooltip("Tag used to find active enemies when determining wave completion.")]
        [SerializeField] private string enemyTag = "Enemy";

        public int TotalWaves => waveDataList != null ? waveDataList.Count : 0;
        public int CurrentWaveIndex { get; private set; } = -1;
        public bool IsSpawning { get; private set; }

        public static event Action<int> OnWaveStart;
        public static event Action<int> OnWaveComplete;
        public static event Action OnAllWavesComplete;

        private Coroutine spawnRoutine;

        private void Awake()
        {
            if (pathManager == null)
            {
                pathManager = GetComponent<PathManager>();
                if (pathManager == null)
                {
                    pathManager = FindFirstObjectByType<PathManager>();
                }
            }
        }

        /// <summary>Advance to the next wave. Fires OnAllWavesComplete + Victory when exhausted.</summary>
        public void StartNextWave()
        {
            if (IsSpawning) return;

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

        private IEnumerator SpawnCoroutine()
        {
            IsSpawning = true;

            WaveData wave = waveDataList[CurrentWaveIndex];
            Transform[] waypoints = pathManager != null ? pathManager.GetWaypoints() : null;

            if (wave == null)
            {
                Debug.LogError($"[WaveManager] Wave at index {CurrentWaveIndex} is null.");
                IsSpawning = false;
                yield break;
            }
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogError("[WaveManager] PathManager has no waypoints — cannot spawn enemies.");
                IsSpawning = false;
                yield break;
            }

            // Lead-in delay.
            if (wave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(wave.delayBeforeWave);
            }

            Vector3 spawnPos = waypoints[0].position;

            // Per-entry spawn loop.
            if (wave.enemies != null)
            {
                foreach (var entry in wave.enemies)
                {
                    if (entry == null || entry.enemyPrefab == null || entry.count <= 0) continue;

                    string poolName = $"Enemy_{entry.enemyPrefab.name}";
                    ObjectPool pool;
                    if (!ObjectPoolRegistry.TryGetPool(poolName, out pool))
                    {
                        // Fall back to the generic "Enemies" pool if a specific one wasn't registered.
                        ObjectPoolRegistry.TryGetPool("Enemies", out pool);
                    }

                    for (int i = 0; i < entry.count; i++)
                    {
                        GameObject enemy;
                        if (pool != null)
                        {
                            enemy = pool.Get();
                        }
                        else
                        {
                            enemy = Instantiate(entry.enemyPrefab);
                        }

                        enemy.transform.position = spawnPos;
                        enemy.transform.rotation = Quaternion.identity;
                        // OnEnable on the pooled instance resets its per-spawn state.
                        yield return new WaitForSeconds(wave.spawnInterval);
                    }
                }
            }

            // Wait until every spawned enemy has returned to its pool (or been destroyed).
            while (CountActiveEnemies() > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            IsSpawning = false;
            OnWaveComplete?.Invoke(CurrentWaveIndex);

            if (autoAdvanceWaves)
            {
                yield return new WaitForSeconds(autoAdvanceDelay);
                StartNextWave();
            }
        }

        private int CountActiveEnemies()
        {
            // GameObject.FindGameObjectsWithTag returns only active objects, which matches our "pooled & disabled = gone" model.
            GameObject[] active = GameObject.FindGameObjectsWithTag(enemyTag);
            return active != null ? active.Length : 0;
        }
    }
}
