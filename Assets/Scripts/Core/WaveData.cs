using System.Collections.Generic;
using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Data-only description of a single wave: which enemies spawn, how many,
    /// the interval between spawns, and the lead-in delay before the wave starts.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveData", menuName = "TowerGuard/Wave Data")]
    public class WaveData : ScriptableObject
    {
        public string waveName;
        public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
        public float spawnInterval = 1.5f;
        public float delayBeforeWave = 2f;

        [System.Serializable]
        public class EnemySpawnEntry
        {
            public GameObject enemyPrefab;
            public int count;
        }
    }
}
