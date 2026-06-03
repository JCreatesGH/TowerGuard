using UnityEngine;
using TowerGuard.Enemies;

namespace TowerGuard.Towers
{
    /// <summary>
    /// Splash projectile: on hit, damages every enemy inside a 1.5-unit radius
    /// at 60% of base damage (the primary target gets the full amount implicitly
    /// because the OverlapCircle catches it too).
    /// </summary>
    public class AOEProjectile : ProjectileBase
    {
        [SerializeField] private float splashRadius = 1.5f;
        [SerializeField, Range(0f, 1f)] private float splashDamageMultiplier = 0.6f;

        protected override void OnHit()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBase enemy = hits[i].GetComponent<EnemyBase>();
                if (enemy == null) continue;
                if (!enemy.IsAlive) continue;
                enemy.TakeDamage(damage * splashDamageMultiplier);
            }
            ReturnToPool();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}
