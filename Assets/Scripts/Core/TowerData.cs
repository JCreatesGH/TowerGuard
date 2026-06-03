using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Data-only description of a tower: cost, base stats, and upgraded stats.
    /// The actual runtime behaviour lives on TowerBase.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "TowerGuard/Tower Data")]
    public class TowerData : ScriptableObject
    {
        public string towerName;
        public Sprite icon;
        public int cost;
        public int upgradeCost;
        public string description;

        [Header("Base Stats")]
        public float damage;
        public float range;
        public float fireRate;
        public float projectileSpeed;
        public GameObject projectilePrefab;

        [Header("Upgraded Stats")]
        public float upgradedDamage;
        public float upgradedRange;
        public float upgradedFireRate;
    }
}
