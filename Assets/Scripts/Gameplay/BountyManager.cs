using System.Collections;
using UnityEngine;
using TowerGuard.Core;
using TowerGuard.Enemies;
using TowerGuard.UI;

namespace TowerGuard.Gameplay
{
    /// <summary>
    /// Phase 5 mechanic — Enemy Bounties.
    /// On every wave whose 1-based index is a multiple of 3, picks one freshly-spawned
    /// enemy at random and turns it into a Bounty Target: 3× soft reward + guaranteed
    /// hard currency on kill, with a small orbiting crown sprite floating above it.
    /// Surfaces a "BOUNTY!" toast via UIManager.
    /// </summary>
    public class BountyManager : MonoBehaviour
    {
        public static BountyManager Instance { get; private set; }

        [Tooltip("Optional crown prefab to attach to the bounty enemy.")]
        [SerializeField] private GameObject crownPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()  { WaveManager.OnWaveStart += OnWaveStart; }
        private void OnDisable() { WaveManager.OnWaveStart -= OnWaveStart; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnWaveStart(int waveIndex)
        {
            int oneBased = waveIndex + 1;
            if (oneBased % 3 != 0) return;
            StartCoroutine(TagBountyAfterSpawn());
        }

        private IEnumerator TagBountyAfterSpawn()
        {
            // Wait long enough for at least the first wave-entries to spawn.
            yield return new WaitForSeconds(1.0f);
            EnemyBase[] live = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            if (live == null || live.Length == 0)
            {
                // Late-spawn — give it one more chance.
                yield return new WaitForSeconds(1.0f);
                live = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                if (live == null || live.Length == 0) yield break;
            }
            var pick = live[Random.Range(0, live.Length)];
            if (pick != null)
            {
                pick.MarkAsBounty(crownPrefab);
                if (UIManager.Instance != null) UIManager.Instance.ShowToast("BOUNTY! Hunt the marked target.", 2.5f);
            }
        }
    }
}
