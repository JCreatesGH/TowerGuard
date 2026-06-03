using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Data-only description of an enemy archetype: HP, speed, armor, and rewards.
    /// Runtime behaviour (pathing, damage application, death) lives on EnemyBase.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TowerGuard/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;
        public int maxHP;
        public float speed;
        public int armor;
        public int softCurrencyReward;
        [Range(0, 100)] public int hardCurrencyChance;
        public GameObject deathParticlePrefab;
    }
}
