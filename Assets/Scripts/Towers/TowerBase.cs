using System.Collections;
using UnityEngine;
using TowerGuard.Core;
using TowerGuard.Enemies;

namespace TowerGuard.Towers
{
    /// <summary>
    /// Runtime behaviour for a placed tower. Scans for enemies within range every
    /// 1/fireRate seconds, targets the enemy furthest along the path, and fires a
    /// projectile from the "Projectiles" pool (or a specific-per-prefab pool if registered).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TowerBase : MonoBehaviour
    {
        [SerializeField] private TowerData data;
        [Header("Phase 5 — feel")]
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private GameObject muzzleFlashPrefab;

        public TowerData Data => data;
        public bool IsUpgraded => isUpgraded;
        public Vector3Int GridPosition { get; set; }

        private bool isUpgraded = false;
        private float currentDamage;
        private float currentRange;
        private float currentFireRate;
        // Phase 5: stat multipliers stacked on top of the data's base values.
        private bool resonanceActive;
        private bool powerNodeActive;
        private GameObject powerNodeRing;

        private SpriteRenderer spriteRenderer;
        private Coroutine fireRoutine;
        private int idleBobTweenId = -1;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadBaseStats();
        }

        private void OnEnable()
        {
            RestartFireRoutine();
            StartIdleBob();
        }

        /// <summary>Phase 5 idle-bob: gentle ±0.03 unit float on a 1.5 s ping-pong loop.</summary>
        private void StartIdleBob()
        {
            if (idleBobTweenId != -1) LeanTween.cancel(idleBobTweenId);
            Vector3 start = transform.localPosition;
            Vector3 up = start + new Vector3(0f, 0.03f, 0f);
            idleBobTweenId = LeanTween.moveLocal(gameObject, up, 1.5f)
                .setEaseInOutSine()
                .setLoopPingPong()
                .id;
        }

        private void RestartFireRoutine()
        {
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }
            if (isActiveAndEnabled)
            {
                fireRoutine = StartCoroutine(FireCoroutine());
            }
        }

        /// <summary>
        /// Called by code that instantiates a TowerBase via AddComponent — Awake runs with
        /// the serialized data field still null, so we provide a post-construction hook to
        /// assign data, reload base stats, and (re)start the fire coroutine with valid numbers.
        /// </summary>
        public void Initialize(TowerData towerData)
        {
            data = towerData;
            LoadBaseStats();
            RestartFireRoutine();
        }

        private void OnDisable()
        {
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }
        }

        private void LoadBaseStats()
        {
            if (data == null) return;
            float baseDamage = isUpgraded ? data.upgradedDamage : data.damage;
            float baseRange  = isUpgraded ? data.upgradedRange  : data.range;
            float baseRate   = isUpgraded ? data.upgradedFireRate : data.fireRate;
            currentDamage = baseDamage;
            currentRange  = baseRange  * (powerNodeActive ? 1.5f : 1f); // +50% range on Power Nodes
            currentFireRate = Mathf.Max(0.01f, baseRate * (resonanceActive ? 1.15f : 1f)); // +15% rate while resonating
        }

        // ===== Phase 5: stat-bonus hooks =====

        /// <summary>Phase 5 mechanic: Arcane Resonance gives +15% fire rate while at least one
        /// neighbouring tower of a different type is within 2.5 units.</summary>
        public void SetResonanceBonus(bool active)
        {
            if (resonanceActive == active) return;
            resonanceActive = active;
            LoadBaseStats();
        }

        /// <summary>Phase 5 mechanic: Power Nodes give +50% range and a purple ring aura.</summary>
        public void SetPowerNodeBonus(bool active)
        {
            if (powerNodeActive == active) return;
            powerNodeActive = active;
            LoadBaseStats();

            if (active && powerNodeRing == null)
            {
                powerNodeRing = new GameObject("PowerNodeRing");
                powerNodeRing.transform.SetParent(transform, false);
                var sr = powerNodeRing.AddComponent<SpriteRenderer>();
                // The setup script can swap in a circle sprite; until then, scale the
                // tower's own sprite tinted purple at 25% alpha to suggest a ring.
                sr.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
                sr.color = new Color(0.486f, 0.302f, 1f, 0.25f); // #7C4DFF @ 25%
                sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) - 1;
                powerNodeRing.transform.localScale = Vector3.one * 1.6f;
            }
            else if (!active && powerNodeRing != null)
            {
                Destroy(powerNodeRing);
                powerNodeRing = null;
            }
        }

        /// <summary>The signature color for this tower type — used by ResonanceManager's
        /// pulsing line and by ambient lighting. Falls back to white if not set.</summary>
        public Color SignatureColor
        {
            get
            {
                if (data == null) return Color.white;
                string n = data.towerName != null ? data.towerName.ToLowerInvariant() : "";
                if (n.Contains("sniper") || n.Contains("void"))   return new Color(0f, 0.898f, 1f);     // #00E5FF
                if (n.Contains("slow")   || n.Contains("frost"))  return new Color(0.502f, 0.871f, 0.918f); // #80DEEA
                if (n.Contains("area")   || n.Contains("magma"))  return new Color(1f, 0.427f, 0f);     // #FF6D00
                return new Color(0.267f, 0.541f, 1f); // basic / ballista — #448AFF
            }
        }

        private IEnumerator FireCoroutine()
        {
            // Small warmup so the first iteration happens after Initialize() has set real stats,
            // but short enough that a tower placed next to the path still reacts within a frame or two.
            yield return null;

            while (true)
            {
                if (data != null && data.projectilePrefab != null)
                {
                    EnemyBase target = AcquireTarget();
                    if (target != null)
                    {
                        // Rotate sprite to face target (2D z-axis).
                        Vector3 delta = target.transform.position - transform.position;
                        if (delta.sqrMagnitude > 0.0001f)
                        {
                            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                            transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        }

                        Fire(target);
                    }
                }

                float wait = 1f / Mathf.Max(0.01f, currentFireRate);
                yield return new WaitForSeconds(wait);
            }
        }

        private EnemyBase AcquireTarget()
        {
            // Unity 6 defaults Physics2D.autoSyncTransforms to false. Enemies move via
            // `transform.position = ...` in EnemyBase.Update, which bypasses the physics sync,
            // so without this call the physics scene still sees each enemy at its previous
            // (often off-screen) position and OverlapCircleAll returns zero hits.
            Physics2D.SyncTransforms();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRange);
            EnemyBase best = null;
            int bestIndex = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBase e = hits[i].GetComponentInParent<EnemyBase>();
                if (e == null || !e.IsAlive) continue;
                if (e.WaypointIndex > bestIndex)
                {
                    bestIndex = e.WaypointIndex;
                    best = e;
                }
            }
            return best;
        }

        private void Fire(EnemyBase target)
        {
            GameObject projGO;
            string poolName = $"Projectile_{data.projectilePrefab.name}";
            if (ObjectPoolRegistry.TryGetPool(poolName, out ObjectPool specific))
            {
                projGO = specific.Get();
            }
            else if (ObjectPoolRegistry.TryGetPool("Projectiles", out ObjectPool generic))
            {
                // Generic pool uses whatever prefab it was created with. If the tower's projectile
                // differs from the generic prefab, instantiate instead so the correct visuals/script run.
                if (generic.Prefab == data.projectilePrefab)
                {
                    projGO = generic.Get();
                }
                else
                {
                    projGO = Instantiate(data.projectilePrefab);
                }
            }
            else
            {
                projGO = Instantiate(data.projectilePrefab);
            }

            projGO.transform.position = transform.position;
            projGO.transform.rotation = Quaternion.identity;

            ProjectileBase proj = projGO.GetComponent<ProjectileBase>();
            if (proj != null)
            {
                proj.damage = currentDamage;
                proj.speed = data.projectileSpeed;
                proj.target = target.transform;
            }

            // Phase 5: SFX + muzzle flash on every shot.
            if (fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(fireClip);
            }
            if (muzzleFlashPrefab != null)
            {
                var fx = Instantiate(muzzleFlashPrefab, transform.position, transform.rotation);
                Destroy(fx, 0.5f);
            }
        }

        /// <summary>Pay upgrade cost and apply upgraded stats. Returns false if already upgraded / insufficient funds.</summary>
        public bool Upgrade()
        {
            if (data == null) return false;
            if (isUpgraded) return false;
            if (GameManager.Instance == null) return false;
            if (GameManager.Instance.SoftCurrency < data.upgradeCost) return false;

            if (!GameManager.Instance.SpendSoftCurrency(data.upgradeCost)) return false;

            isUpgraded = true;
            LoadBaseStats(); // re-derive stats with all bonuses re-applied on top of upgraded base values

            // Phase 5: gold tint as a fallback when no upgraded-sprite swap is wired.
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.84f, 0f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.84f, 0f); // gold tint
            }
            return true;
        }

        /// <summary>Refund half the total invested cost and remove the tower.</summary>
        public void Sell()
        {
            int refund = data != null
                ? (data.cost + (isUpgraded ? data.upgradeCost : 0)) / 2
                : 0;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EarnSoftCurrency(refund);
            }
            if (TowerPlacement.Instance != null)
            {
                TowerPlacement.Instance.RemoveTower(GridPosition);
            }
            Destroy(gameObject);
        }

        // OnMouseDown removed — it does NOT fire on iOS hardware.
        // Tower selection is now driven by TouchInputHandler (Assets/Scripts/UI/TouchInputHandler.cs),
        // which OverlapPoints the tap world position and routes the hit to TowerPlacement.SelectTower.

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            float r = currentRange > 0f ? currentRange : (data != null ? data.range : 0f);
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}
