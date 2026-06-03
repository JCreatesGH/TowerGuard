using System.Collections.Generic;
using UnityEngine;
using TowerGuard.Towers;

namespace TowerGuard.Gameplay
{
    /// <summary>
    /// Phase 5 mechanic — Arcane Resonance.
    /// When two or more towers of DIFFERENT types are within 2.5 units of each other,
    /// they enter a Resonance state: +15% fire rate, plus a thin LineRenderer link
    /// pulsing between their two signature colors.
    ///
    /// Place one of these on a scene-level GameObject in Level_01. It scans for towers
    /// every 0.25s — cheap because the count is tiny. Towers expose a SetResonanceBonus
    /// method and a SignatureColor property.
    /// </summary>
    public class ResonanceManager : MonoBehaviour
    {
        public static ResonanceManager Instance { get; private set; }

        [SerializeField] private float resonanceRadius = 2.5f;
        [SerializeField] private float scanInterval = 0.25f;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float pulseHz = 1f;

        private readonly Dictionary<TowerPair, LineRenderer> activeLinks = new Dictionary<TowerPair, LineRenderer>();
        private readonly HashSet<TowerBase> currentlyResonating = new HashSet<TowerBase>();
        private float scanTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var lr in activeLinks.Values)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            activeLinks.Clear();
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                Rescan();
                scanTimer = scanInterval;
            }
            // Pulse line colors at `pulseHz`.
            float t = (Mathf.Sin(Time.time * Mathf.PI * 2f * pulseHz) + 1f) * 0.5f;
            foreach (var kv in activeLinks)
            {
                var lr = kv.Value;
                if (lr == null) continue;
                lr.startColor = Color.Lerp(kv.Key.A.SignatureColor, kv.Key.B.SignatureColor, t);
                lr.endColor   = Color.Lerp(kv.Key.B.SignatureColor, kv.Key.A.SignatureColor, t);
                lr.SetPosition(0, kv.Key.A.transform.position);
                lr.SetPosition(1, kv.Key.B.transform.position);
            }
        }

        public void Rescan()
        {
            // Phase 5 keeps things simple — find every active tower in the scene.
            var all = FindObjectsByType<TowerBase>(FindObjectsSortMode.None);
            float r2 = resonanceRadius * resonanceRadius;
            var newPairs = new HashSet<TowerPair>();
            var nowResonating = new HashSet<TowerBase>();

            for (int i = 0; i < all.Length; i++)
            {
                var a = all[i];
                if (a == null || a.Data == null) continue;
                for (int j = i + 1; j < all.Length; j++)
                {
                    var b = all[j];
                    if (b == null || b.Data == null) continue;
                    if (a.Data == b.Data) continue; // same type — no resonance
                    float d2 = (a.transform.position - b.transform.position).sqrMagnitude;
                    if (d2 > r2) continue;
                    newPairs.Add(new TowerPair(a, b));
                    nowResonating.Add(a);
                    nowResonating.Add(b);
                }
            }

            // Apply / unset bonus.
            foreach (var t in nowResonating) t.SetResonanceBonus(true);
            foreach (var t in currentlyResonating)
            {
                if (!nowResonating.Contains(t) && t != null) t.SetResonanceBonus(false);
            }
            currentlyResonating.Clear();
            foreach (var t in nowResonating) currentlyResonating.Add(t);

            // Reconcile line renderers.
            var toRemove = new List<TowerPair>();
            foreach (var kv in activeLinks)
            {
                if (!newPairs.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.gameObject);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove) activeLinks.Remove(k);

            foreach (var pair in newPairs)
            {
                if (activeLinks.ContainsKey(pair)) continue;
                var lr = MakeLink();
                activeLinks[pair] = lr;
            }
        }

        private LineRenderer MakeLink()
        {
            var go = new GameObject("ResonanceLink");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = lineWidth;
            lr.numCapVertices = 4;
            // A simple unlit material so we see the colors regardless of URP settings.
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 4;
            return lr;
        }

        // Pair key that's order-independent (a,b == b,a) for the dictionary.
        private struct TowerPair : System.IEquatable<TowerPair>
        {
            public TowerBase A;
            public TowerBase B;

            public TowerPair(TowerBase x, TowerBase y)
            {
                if (x.GetInstanceID() < y.GetInstanceID()) { A = x; B = y; }
                else { A = y; B = x; }
            }

            public bool Equals(TowerPair other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is TowerPair p && Equals(p);
            public override int GetHashCode()
            {
                int hA = A != null ? A.GetInstanceID() : 0;
                int hB = B != null ? B.GetInstanceID() : 0;
                return hA ^ (hB << 1);
            }
        }
    }
}
