using UnityEngine;

namespace TowerGuard.Utils
{
    /// <summary>
    /// Phase 5 — simple parallax background. Each layer moves at a fraction of the
    /// camera's horizontal position. Two layers (far + mid) cover the dark fantasy
    /// horizon look. Both should use SpriteRenderer.drawMode = Tiled so the texture
    /// extends past the camera bounds.
    /// </summary>
    public class Parallax : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public Transform transform;
            [Range(0f, 1f)] public float scrollFactor = 0.1f;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Layer[] layers = new Layer[0];

        private Vector3 lastCameraPos;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null) lastCameraPos = targetCamera.transform.position;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            Vector3 delta = targetCamera.transform.position - lastCameraPos;
            for (int i = 0; i < layers.Length; i++)
            {
                var l = layers[i];
                if (l == null || l.transform == null) continue;
                l.transform.position += new Vector3(delta.x * l.scrollFactor, delta.y * l.scrollFactor * 0.5f, 0f);
            }
            lastCameraPos = targetCamera.transform.position;
        }

        public void SetLayers(Layer[] newLayers, Camera cam = null)
        {
            layers = newLayers;
            if (cam != null) targetCamera = cam;
        }
    }
}
