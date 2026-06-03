using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using TowerGuard.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TowerGuard.Towers
{
    /// <summary>
    /// Drives tower placement: choosing a tower type (from UI), showing a ghost snapped
    /// to the Tilemap_Ground grid, and placing a real tower on the first tap that lands
    /// on a buildable tile.
    /// </summary>
    public class TowerPlacement : MonoBehaviour
    {
        public static TowerPlacement Instance { get; private set; }

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Grid towerGrid;
        [SerializeField] private Tilemap buildableTilemap;
        [SerializeField] private TileBase buildableTile;
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] private Color ghostValidColor = new Color(0.4f, 1f, 0.4f, 0.55f);
        [SerializeField] private Color ghostInvalidColor = new Color(1f, 0.3f, 0.3f, 0.55f);

        public TowerBase SelectedTower { get; private set; }

        private readonly Dictionary<Vector3Int, TowerBase> placedTowers = new Dictionary<Vector3Int, TowerBase>();
        private TowerData selectedTowerData;
        private GameObject ghostTower;
        private SpriteRenderer ghostRenderer;

        public static event System.Action<TowerBase> OnTowerSelected;
        public static event System.Action OnTowerDeselected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by the UI when the user taps a tower button. Spawns a ghost preview.</summary>
        public void SelectTowerType(TowerData data)
        {
            selectedTowerData = data;
            DestroyGhost();
            if (data == null) return;

            ghostTower = new GameObject("GhostTower");
            ghostRenderer = ghostTower.AddComponent<SpriteRenderer>();
            ghostRenderer.sortingOrder = 5;
            if (data.icon != null)
            {
                ghostRenderer.sprite = data.icon;
            }
            ghostRenderer.color = ghostValidColor;
        }

        /// <summary>Called by clicks on an already-placed tower.</summary>
        public void SelectTower(TowerBase tower)
        {
            SelectedTower = tower;
            OnTowerSelected?.Invoke(tower);
        }

        public void DeselectTower()
        {
            SelectedTower = null;
            OnTowerDeselected?.Invoke();
        }

        public void RemoveTower(Vector3Int cell)
        {
            if (placedTowers.ContainsKey(cell))
            {
                placedTowers.Remove(cell);
            }
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 worldPos = ReadPointerWorldPos();
            if (ghostTower != null && towerGrid != null)
            {
                Vector3Int cell = towerGrid.WorldToCell(worldPos);
                Vector3 snapped = towerGrid.GetCellCenterWorld(cell);
                ghostTower.transform.position = new Vector3(snapped.x, snapped.y, 0f);
                if (ghostRenderer != null)
                {
                    ghostRenderer.color = CanPlaceAt(cell) ? ghostValidColor : ghostInvalidColor;
                }
            }

            if (IsTapThisFrame())
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (selectedTowerData != null)
                {
                    TryPlaceTower(worldPos);
                }
            }
        }

        private bool CanPlaceAt(Vector3Int cell)
        {
            if (placedTowers.ContainsKey(cell)) return false;
            if (buildableTilemap == null) return true; // permissive fallback
            TileBase here = buildableTilemap.GetTile(cell);
            if (here == null) return false;
            if (buildableTile != null && here != buildableTile) return false;
            if (obstacleTilemap != null && obstacleTilemap.GetTile(cell) != null) return false;
            return true;
        }

        private void TryPlaceTower(Vector3 worldPos)
        {
            if (towerGrid == null || selectedTowerData == null) return;

            Vector3Int cell = towerGrid.WorldToCell(worldPos);
            if (!CanPlaceAt(cell)) return;
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.SpendSoftCurrency(selectedTowerData.cost)) return;

            GameObject towerGO = new GameObject($"Tower_{selectedTowerData.towerName}");
            Vector3 cellCenter = towerGrid.GetCellCenterWorld(cell);
            towerGO.transform.position = new Vector3(cellCenter.x, cellCenter.y, 0f);

            SpriteRenderer sr = towerGO.AddComponent<SpriteRenderer>();
            sr.sprite = selectedTowerData.icon;
            sr.sortingOrder = 3;

            var collider = towerGO.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.9f);

            TowerBase tower = towerGO.AddComponent<TowerBase>();
            // Data must be assigned AFTER AddComponent ran Awake, so use the explicit
            // Initialize() hook which reloads stats and (re)starts the fire coroutine
            // with the real range / fire-rate values.
            tower.Initialize(selectedTowerData);
            tower.GridPosition = cell;

            placedTowers[cell] = tower;

            DestroyGhost();
            selectedTowerData = null;
        }

        private void DestroyGhost()
        {
            if (ghostTower != null)
            {
                Destroy(ghostTower);
                ghostTower = null;
                ghostRenderer = null;
            }
        }

        // ---- Input helpers that work with both the old and new Input System ----

        private Vector3 ReadPointerWorldPos()
        {
            Vector2 screen = ReadPointerScreenPos();
            Vector3 w = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(mainCamera.transform.position.z)));
            w.z = 0f;
            return w;
        }

        private Vector2 ReadPointerScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private bool IsTapThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
