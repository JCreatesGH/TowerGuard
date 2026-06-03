using UnityEngine;

namespace TowerGuard.UI
{
    /// <summary>
    /// Reads Screen.safeArea once on Awake and each orientation change, then
    /// adjusts the RectTransform anchors so children stay inside the notch-safe
    /// region on iPhone-class devices. Attach to a full-screen RectTransform that
    /// acts as the root of any on-screen UI.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform rt;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;

        private void Awake()
        {
            rt = (RectTransform)transform;
            Apply(Screen.safeArea);
        }

        private void Update()
        {
            // Refresh on orientation or size change (for rotation / in-editor Game-view resize).
            if (Screen.safeArea != lastSafeArea
                || Screen.width != lastScreenSize.x
                || Screen.height != lastScreenSize.y
                || Screen.orientation != lastOrientation)
            {
                Apply(Screen.safeArea);
            }
        }

        private void Apply(Rect safeArea)
        {
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
