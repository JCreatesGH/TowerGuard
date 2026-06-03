using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerGuard.Utils
{
    /// <summary>
    /// Phase 5 — animates a water Tilemap by cycling between an array of TileBase
    /// frames every <paramref name="frameDuration"/> seconds. Drop this on the water
    /// Tilemap object and assign the 3 generated water tile assets.
    /// </summary>
    public class WaterAnimator : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase[] frames;
        [SerializeField] private float frameDuration = 0.4f;

        private int frameIndex;
        private float timer;
        private Vector3Int[] waterCells;

        private void Start()
        {
            if (tilemap == null) tilemap = GetComponent<Tilemap>();
            if (tilemap == null || frames == null || frames.Length == 0) return;
            CacheWaterCells();
        }

        private void Update()
        {
            if (tilemap == null || frames == null || frames.Length == 0) return;
            timer += Time.deltaTime;
            if (timer < frameDuration) return;
            timer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            TileBase newTile = frames[frameIndex];
            if (waterCells == null) CacheWaterCells();
            for (int i = 0; i < waterCells.Length; i++)
            {
                tilemap.SetTile(waterCells[i], newTile);
            }
        }

        private void CacheWaterCells()
        {
            var bounds = tilemap.cellBounds;
            var list = new System.Collections.Generic.List<Vector3Int>();
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.GetTile(pos) != null) list.Add(pos);
            }
            waterCells = list.ToArray();
        }

        public void Setup(Tilemap t, TileBase[] f, float dur = 0.4f)
        {
            tilemap = t;
            frames = f;
            frameDuration = dur;
            frameIndex = 0;
            timer = 0f;
            CacheWaterCells();
        }
    }
}
