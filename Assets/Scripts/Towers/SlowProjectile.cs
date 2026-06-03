using UnityEngine;
using TowerGuard.Enemies;

namespace TowerGuard.Towers
{
    /// <summary>
    /// Applies a 50% slow for 2 seconds plus the small base damage to the target.
    /// </summary>
    public class SlowProjectile : ProjectileBase
    {
        [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
        [SerializeField] private float slowDuration = 2f;

        protected override void OnHit()
        {
            if (target != null)
            {
                EnemyBase enemy = target.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.ApplySlow(slowMultiplier, slowDuration);
                    enemy.TakeDamage(damage);
                }
            }
            ReturnToPool();
        }
    }
}
