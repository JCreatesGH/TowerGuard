using UnityEngine;
using TowerGuard.Core;
using TowerGuard.Enemies;

namespace TowerGuard.Towers
{
    /// <summary>
    /// Homing projectile. Flies toward the target Transform; if the target dies
    /// mid-flight, finishes flying to the last-known position, then resolves a hit.
    /// Pool name: "Projectiles".
    /// </summary>
    public class ProjectileBase : MonoBehaviour
    {
        [Tooltip("Damage applied on hit — set at spawn time by the firing tower.")]
        public float damage;
        [Tooltip("Units per second — set at spawn time by the firing tower.")]
        public float speed = 8f;
        [Tooltip("Current homing target — cleared if the target dies.")]
        public Transform target;

        [Tooltip("If the projectile travels more than this many units without hitting, return to pool.")]
        [SerializeField] protected float maxLifetime = 3f;

        protected Vector3 lastKnownPosition;
        protected float spawnTime;

        protected virtual void OnEnable()
        {
            spawnTime = Time.time;
            if (target != null)
            {
                lastKnownPosition = target.position;
            }
            else
            {
                lastKnownPosition = transform.position + transform.right;
            }
        }

        protected virtual void Update()
        {
            if (Time.time - spawnTime > maxLifetime)
            {
                ReturnToPool();
                return;
            }

            Vector3 targetPos;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                targetPos = target.position;
                lastKnownPosition = targetPos;
            }
            else
            {
                targetPos = lastKnownPosition;
            }

            Vector3 here = transform.position;
            Vector3 delta = targetPos - here;
            delta.z = 0f;

            float step = speed * Time.deltaTime;
            if (delta.sqrMagnitude <= 0.15f * 0.15f || delta.sqrMagnitude <= step * step)
            {
                transform.position = targetPos;
                OnHit();
                return;
            }

            transform.position = here + delta.normalized * step;

            // Face the direction of travel (2D z-rotation).
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>Override for AOE / slow / special hit behaviours.</summary>
        protected virtual void OnHit()
        {
            if (target != null)
            {
                EnemyBase enemy = target.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(damage);
                }
            }
            ReturnToPool();
        }

        protected void ReturnToPool()
        {
            if (ObjectPoolRegistry.TryGetPool("Projectiles", out ObjectPool pool))
            {
                pool.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
