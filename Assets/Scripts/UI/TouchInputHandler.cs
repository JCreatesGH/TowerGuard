using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TowerGuard.Towers;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TowerGuard.UI
{
    /// <summary>
    /// Singleton that polls the new Input System for a single tap-this-frame and
    /// routes it to game-world consumers. Replaces OnMouseDown (which does NOT
    /// fire on iOS hardware) for tower selection. TowerPlacement also queries
    /// this singleton for its tap.
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        public static TouchInputHandler Instance { get; private set; }

        [SerializeField] private Camera mainCamera;
        [Tooltip("If true, taps over UI (as reported by EventSystem) are swallowed and no world-space event fires.")]
        [SerializeField] private bool blockTapsOverUI = true;

        /// <summary>Fires once per tap that lands on the game world (not on UI). World position on z=0.</summary>
        public static event Action<Vector3> OnWorldTap;

        public bool TappedThisFrame { get; private set; }
        public Vector3 LastWorldTap { get; private set; }
        public bool PointerOverUIThisFrame { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            TappedThisFrame = false;
            PointerOverUIThisFrame = false;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            if (!IsTapThisFrame()) return;

            PointerOverUIThisFrame = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (blockTapsOverUI && PointerOverUIThisFrame) return;

            Vector2 screen = ReadPointerScreenPos();
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(mainCamera.transform.position.z)));
            world.z = 0f;

            TappedThisFrame = true;
            LastWorldTap = world;
            OnWorldTap?.Invoke(world);

            // Tower-selection hit test: if the tap lands on a collider with a TowerBase, select it.
            Collider2D hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit != null)
            {
                TowerBase tower = hit.GetComponentInParent<TowerBase>();
                if (tower != null && TowerPlacement.Instance != null)
                {
                    TowerPlacement.Instance.SelectTower(tower);
                }
            }
        }

        private bool IsTapThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            return false;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
            return Input.GetMouseButtonDown(0);
#endif
        }

        private Vector2 ReadPointerScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
            {
                Vector2 t = Touchscreen.current.primaryTouch.position.ReadValue();
                if (t != Vector2.zero) return t;
            }
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            if (Input.touchCount > 0) return Input.GetTouch(0).position;
            return Input.mousePosition;
#endif
        }
    }
}
