using System.Collections.Generic;
using UnityEngine;
using TowerGuard.Towers;

namespace TowerGuard.Gameplay
{
    /// <summary>
    /// Phase 5 mechanic — Power Nodes.
    /// A short list of world-space cell positions that grant +50% range when a tower
    /// is placed on top of them. Set the cells in the inspector (or via Phase 5 setup).
    /// Re-scans the scene on a 0.5s tick — placement is rare so this is cheap.
    /// </summary>
    public class PowerNodeManager : MonoBehaviour
    {
        public static PowerNodeManager Instance { get; private set; }

        [Tooltip("World positions of the Power Node tiles (cell centres).")]
        [SerializeField] private List<Vector2> nodes = new List<Vector2>();
        [Tooltip("Distance threshold to count a tower as 'on' a node.")]
        [SerializeField] private float snapRadius = 0.45f;
        [SerializeField] private float scanInterval = 0.5f;

        private float timer;
        private readonly HashSet<TowerBase> currentlyOnNode = new HashSet<TowerBase>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetNodes(IEnumerable<Vector2> positions)
        {
            nodes.Clear();
            if (positions == null) return;
            foreach (var p in positions) nodes.Add(p);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = scanInterval;
            Rescan();
        }

        public void Rescan()
        {
            var towers = FindObjectsByType<TowerBase>(FindObjectsSortMode.None);
            var nowOnNode = new HashSet<TowerBase>();
            float r2 = snapRadius * snapRadius;

            foreach (var t in towers)
            {
                if (t == null) continue;
                Vector2 p = t.transform.position;
                bool onNode = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if ((p - nodes[i]).sqrMagnitude <= r2) { onNode = true; break; }
                }
                if (onNode) nowOnNode.Add(t);
            }

            foreach (var t in nowOnNode) t.SetPowerNodeBonus(true);
            foreach (var t in currentlyOnNode)
            {
                if (t != null && !nowOnNode.Contains(t)) t.SetPowerNodeBonus(false);
            }
            currentlyOnNode.Clear();
            foreach (var t in nowOnNode) currentlyOnNode.Add(t);
        }

        // Scene-view helper — draws each node so the level-builder can place towers on them.
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.486f, 0.302f, 1f, 0.6f);
            foreach (var n in nodes)
            {
                Gizmos.DrawWireSphere(new Vector3(n.x, n.y, 0f), snapRadius);
            }
        }
    }
}
