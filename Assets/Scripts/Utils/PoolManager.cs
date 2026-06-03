using System.Collections.Generic;
using UnityEngine;
using TowerGuard.Core;

namespace TowerGuard.Utils
{
    /// <summary>
    /// Creates the "Enemies", "Projectiles", and "Effects" pools on Awake.
    /// Additional per-prefab pools can be registered through the `extraPools` list
    /// (e.g. one pool per enemy prefab, keyed "Enemy_&lt;prefab-name&gt;").
    /// Also initializes a fresh GameManager run so HP/currency/wave reset each time Level_01 loads.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [System.Serializable]
        public class PoolSpec
        {
            public string poolName;
            public GameObject prefab;
            public int size = 10;
        }

        [SerializeField] private GameObject enemiesPrefab;
        [SerializeField] private int enemiesPoolSize = 20;
        [SerializeField] private GameObject projectilesPrefab;
        [SerializeField] private int projectilesPoolSize = 30;
        [SerializeField] private GameObject effectsPrefab;
        [SerializeField] private int effectsPoolSize = 20;

        [Tooltip("Extra named pools — e.g. one per enemy prefab, named Enemy_<prefab-name>.")]
        [SerializeField] private List<PoolSpec> extraPools = new List<PoolSpec>();

        [Tooltip("If true, PoolManager calls GameManager.StartNewGame() in Awake so HP/currency reset on Level_01 load.")]
        [SerializeField] private bool startFreshRunOnAwake = true;

        private void Awake()
        {
            // Make sure there is a registry host in the scene.
            if (ObjectPoolRegistry.Instance == null)
            {
                gameObject.AddComponent<ObjectPoolRegistry>();
            }

            CreatePoolSafe("Enemies", enemiesPrefab, enemiesPoolSize);
            CreatePoolSafe("Projectiles", projectilesPrefab, projectilesPoolSize);
            CreatePoolSafe("Effects", effectsPrefab, effectsPoolSize);

            if (extraPools != null)
            {
                foreach (var spec in extraPools)
                {
                    if (spec == null || string.IsNullOrEmpty(spec.poolName)) continue;
                    CreatePoolSafe(spec.poolName, spec.prefab, spec.size);
                }
            }

            if (startFreshRunOnAwake && GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }

        private static void CreatePoolSafe(string name, GameObject prefab, int size)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] Pool '{name}' not created: prefab is null.");
                return;
            }
            ObjectPoolRegistry.CreatePool(name, prefab, size);
        }
    }
}
