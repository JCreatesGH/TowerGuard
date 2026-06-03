using System.Collections.Generic;
using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Generic GameObject pool. Not a MonoBehaviour itself — it is owned by
    /// <see cref="ObjectPoolRegistry"/>, the scene-level host that keeps a named
    /// registry of pools and parents the pooled instances under itself.
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<GameObject> available = new Queue<GameObject>();

        public GameObject Prefab => prefab;
        public int CountAvailable => available.Count;

        public ObjectPool(GameObject prefab, int initialSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            if (prefab == null)
            {
                Debug.LogError("[ObjectPool] Cannot create pool: prefab is null.");
                return;
            }

            for (int i = 0; i < initialSize; i++)
            {
                var obj = Object.Instantiate(prefab, parent);
                obj.SetActive(false);
                available.Enqueue(obj);
            }
        }

        /// <summary>Fetch an instance from the pool, instantiating a new one if the queue is empty.</summary>
        public GameObject Get()
        {
            GameObject obj;
            if (available.Count > 0)
            {
                obj = available.Dequeue();
                if (obj == null)
                {
                    // The pooled instance was destroyed externally — fall through and create a new one.
                    obj = Object.Instantiate(prefab, parent);
                }
            }
            else
            {
                obj = Object.Instantiate(prefab, parent);
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>Return an instance back to the pool. It is deactivated and re-parented to the pool host.</summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            available.Enqueue(obj);
        }
    }

    /// <summary>
    /// Scene-hosted singleton that keeps the named pool registry. Other systems
    /// register and fetch pools via the static <see cref="Pools"/> dictionary.
    /// </summary>
    public class ObjectPoolRegistry : MonoBehaviour
    {
        public static readonly Dictionary<string, ObjectPool> Pools = new Dictionary<string, ObjectPool>();

        public static ObjectPoolRegistry Instance { get; private set; }

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
            if (Instance == this)
            {
                Pools.Clear();
                Instance = null;
            }
        }

        /// <summary>Create and register a pool under a given name. Returns the existing pool if one is already registered.</summary>
        public static ObjectPool CreatePool(string poolName, GameObject prefab, int initialSize)
        {
            if (Pools.TryGetValue(poolName, out var existing))
            {
                return existing;
            }

            Transform parent = Instance != null ? Instance.transform : null;
            var pool = new ObjectPool(prefab, initialSize, parent);
            Pools[poolName] = pool;
            return pool;
        }

        public static bool TryGetPool(string poolName, out ObjectPool pool)
        {
            return Pools.TryGetValue(poolName, out pool);
        }
    }
}
