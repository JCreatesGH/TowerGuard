using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerGuard.Core;
using TowerGuard.Utils;

namespace TowerGuard.Enemies
{
    /// <summary>
    /// Pooled enemy. Walks the PathManager waypoints, takes damage from towers,
    /// and on death pays out currency through GameManager. Lives in the "Enemies" pool,
    /// or in a per-prefab pool registered as "Enemy_&lt;prefab-name&gt;".
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] private EnemyData data;
        [Tooltip("Optional: assigned automatically at enable if null. Used for HP % display.")]
        [SerializeField] private Slider hpBar;
        [Tooltip("World-space HP bar root — scaled to 0.01 by default so it reads at 1 unit/tile.")]
        [SerializeField] private Transform hpBarRoot;

        public EnemyData Data => data;
        public int WaypointIndex => waypointIndex;
        public bool IsAlive => currentHP > 0;

        private float currentHP;
        private float currentSpeed;
        private int waypointIndex;
        private Transform[] waypoints;
        private Coroutine slowRoutine;
        private Coroutine flashRoutine;
        private bool hasDied;

        // Phase 5 visuals
        private SpriteRenderer cachedRenderer;
        private Color originalColor = Color.white;
        public bool IsBounty { get; private set; }
        private GameObject bountyCrown;

        private static PathManager cachedPath;

        private void OnEnable()
        {
            if (data != null)
            {
                currentHP = data.maxHP;
                currentSpeed = data.speed;
            }
            waypointIndex = 0;
            hasDied = false;
            IsBounty = false;

            // Phase 5: reset transform/visual state in case the previous death animation
            // left the renderer scaled, rotated or faded inside the pool.
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (cachedRenderer == null) cachedRenderer = GetComponentInChildren<SpriteRenderer>();
            if (cachedRenderer != null)
            {
                originalColor = cachedRenderer.color;
                if (originalColor.a < 0.01f)
                {
                    originalColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
                }
                cachedRenderer.color = originalColor;
            }

            if (waypoints == null || waypoints.Length == 0)
            {
                CacheWaypoints();
            }

            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
                slowRoutine = null;
            }
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            if (bountyCrown != null)
            {
                Destroy(bountyCrown);
                bountyCrown = null;
            }

            UpdateHPBar();
        }

        private void OnDisable()
        {
            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
                slowRoutine = null;
            }
        }

        private void Start()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                CacheWaypoints();
            }
        }

        private void CacheWaypoints()
        {
            if (cachedPath == null)
            {
                cachedPath = FindFirstObjectByType<PathManager>();
            }
            if (cachedPath != null)
            {
                waypoints = cachedPath.GetWaypoints();
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            if (!IsAlive) return;

            if (waypointIndex >= waypoints.Length)
            {
                OnReachedEnd();
                return;
            }

            Transform target = waypoints[waypointIndex];
            if (target == null)
            {
                waypointIndex++;
                return;
            }

            Vector3 here = transform.position;
            Vector3 there = target.position;
            there.z = here.z; // stay on the 2D plane

            Vector3 delta = there - here;
            float step = currentSpeed * Time.deltaTime;
            if (delta.sqrMagnitude <= step * step || delta.sqrMagnitude < 0.01f)
            {
                transform.position = there;
                waypointIndex++;
                if (waypointIndex >= waypoints.Length)
                {
                    OnReachedEnd();
                }
            }
            else
            {
                transform.position = here + delta.normalized * step;
            }
        }

        private void OnReachedEnd()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnemyReachedEnd();
            }
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeLight();
            }
            ReturnToPool();
        }

        /// <summary>Deal damage; armor is applied as a flat reduction.</summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive || hasDied) return;

            float effective = Mathf.Max(0f, amount - (data != null ? data.armor : 0));
            currentHP -= effective;
            UpdateHPBar();

            // Phase 5: brief white hit flash so every shot feels impactful.
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlash());

            if (currentHP <= 0f)
            {
                OnDeath();
            }
        }

        private IEnumerator HitFlash()
        {
            if (cachedRenderer != null)
            {
                cachedRenderer.color = Color.white;
            }
            yield return new WaitForSeconds(0.05f);
            if (cachedRenderer != null)
            {
                cachedRenderer.color = originalColor;
            }
            flashRoutine = null;
        }

        /// <summary>Marks this enemy as a bounty target. BountyManager calls this and
        /// supplies a small crown GameObject to orbit above the enemy.</summary>
        public void MarkAsBounty(GameObject crownPrefab)
        {
            IsBounty = true;
            if (crownPrefab != null && bountyCrown == null)
            {
                bountyCrown = Instantiate(crownPrefab, transform);
                bountyCrown.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            }
        }

        private void OnDeath()
        {
            if (hasDied) return;
            hasDied = true;

            // Phase 5: bounty multiplier — 3× soft + guaranteed +1 hard.
            int softReward = data != null ? data.softCurrencyReward : 0;
            if (IsBounty) softReward *= 3;

            if (data != null && GameManager.Instance != null)
            {
                GameManager.Instance.EarnSoftCurrency(softReward);
                GameManager.Instance.NoteEnemyDefeated();
                if (IsBounty)
                {
                    GameManager.Instance.EarnHardCurrency(1);
                }
                else if (data.hardCurrencyChance > 0 && Random.Range(0, 100) < data.hardCurrencyChance)
                {
                    GameManager.Instance.EarnHardCurrency(1);
                }
            }

            if (data != null && data.deathParticlePrefab != null)
            {
                SpawnDeathEffect();
            }

            // Phase 5: notify the kill-combo manager + analytics.
            GameManager.NotifyEnemyKilled();

            // Boss death gets a heavy shake. Use a substring check so the enemy
            // can be renamed (e.g. "The Dread Colossus") without breaking detection.
            if (data != null && CameraShake.Instance != null)
            {
                string n = data.enemyName != null ? data.enemyName.ToLowerInvariant() : "";
                if (n.Contains("boss") || n.Contains("colossus"))
                {
                    CameraShake.Instance.ShakeHeavy();
                }
            }

            // Phase 5: scale-down + rotate-fall + fade animation, then return to pool.
            StartCoroutine(DeathAnimThenReturn(0.2f));
        }

        private IEnumerator DeathAnimThenReturn(float duration)
        {
            // Stop forward motion while we play the death anim (Update bails because hasDied=true).
            float t = 0f;
            Vector3 startScale = transform.localScale;
            Quaternion startRot = transform.localRotation;
            Quaternion endRot   = startRot * Quaternion.Euler(0f, 0f, 90f); // fall over
            Color startColor = cachedRenderer != null ? cachedRenderer.color : Color.white;

            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, u);
                transform.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, u);
                if (cachedRenderer != null)
                {
                    cachedRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - u);
                }
                yield return null;
            }

            ReturnToPool();
        }

        private void SpawnDeathEffect()
        {
            GameObject fx;
            if (ObjectPoolRegistry.TryGetPool("Effects", out ObjectPool pool))
            {
                fx = pool.Get();
            }
            else
            {
                fx = Instantiate(data.deathParticlePrefab);
            }
            fx.transform.position = transform.position;
            // Auto-return after 1.5s.
            var host = ObjectPoolRegistry.Instance != null ? ObjectPoolRegistry.Instance : null;
            if (host != null)
            {
                host.StartCoroutine(ReturnEffectAfter(fx, 1.5f));
            }
        }

        private IEnumerator ReturnEffectAfter(GameObject fx, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (ObjectPoolRegistry.TryGetPool("Effects", out ObjectPool pool))
            {
                pool.Return(fx);
            }
            else if (fx != null)
            {
                Destroy(fx);
            }
        }

        /// <summary>Temporarily reduce movement speed to data.speed * multiplier for `duration` seconds.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
            }
            slowRoutine = StartCoroutine(SlowRoutine(multiplier, duration));
        }

        private IEnumerator SlowRoutine(float multiplier, float duration)
        {
            float baseSpeed = data != null ? data.speed : currentSpeed;
            currentSpeed = baseSpeed * multiplier;
            yield return new WaitForSeconds(duration);
            currentSpeed = baseSpeed;
            slowRoutine = null;
        }

        private void UpdateHPBar()
        {
            if (hpBar == null || data == null || data.maxHP <= 0) return;
            hpBar.value = Mathf.Clamp01(currentHP / data.maxHP);
        }

        private void ReturnToPool()
        {
            // Try a per-prefab pool first, then the generic pool.
            string poolName = $"Enemy_{gameObject.name.Replace("(Clone)", string.Empty).Trim()}";
            if (ObjectPoolRegistry.TryGetPool(poolName, out ObjectPool pool))
            {
                pool.Return(gameObject);
                return;
            }
            if (ObjectPoolRegistry.TryGetPool("Enemies", out ObjectPool fallback))
            {
                fallback.Return(gameObject);
                return;
            }
            gameObject.SetActive(false);
        }
    }
}
