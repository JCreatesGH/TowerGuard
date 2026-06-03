using UnityEngine;
using TowerGuard.Core;
#if CINEMACHINE_PRESENT
using Unity.Cinemachine;
#endif

namespace TowerGuard.Utils
{
    /// <summary>
    /// Light/heavy camera shake. Prefers Cinemachine Impulse Source if one is assigned,
    /// otherwise falls back to a simple positional shake on the main camera transform.
    /// The Cinemachine integration is opt-in via the CINEMACHINE_PRESENT scripting define
    /// so the code compiles even when Cinemachine isn't referenced from an asmdef.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

#if CINEMACHINE_PRESENT
        [SerializeField] private CinemachineImpulseSource impulseSource;
#endif
        [SerializeField] private float lightForce = 0.15f;
        [SerializeField] private float heavyForce = 0.4f;
        [SerializeField] private float fallbackShakeDuration = 0.18f;

        private Camera shakeCamera;
        private Vector3 originalCamPos;
        private float shakeEndTime;
        private float currentAmplitude;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameManager.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameManager.OnGameOver -= HandleGameOver;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleGameOver() => ShakeHeavy();

        public void ShakeLight() => Shake(lightForce);
        public void ShakeHeavy() => Shake(heavyForce);

        private void Shake(float force)
        {
#if CINEMACHINE_PRESENT
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulseWithForce(force);
                return;
            }
#endif
            FallbackShake(force);
        }

        private void FallbackShake(float amplitude)
        {
            if (shakeCamera == null) shakeCamera = Camera.main;
            if (shakeCamera == null) return;

            if (Time.time > shakeEndTime)
            {
                originalCamPos = shakeCamera.transform.position;
            }
            shakeEndTime = Time.time + fallbackShakeDuration;
            currentAmplitude = amplitude;
        }

        private void LateUpdate()
        {
            if (shakeCamera == null) return;
            if (Time.time < shakeEndTime)
            {
                Vector3 jitter = (Vector3)(Random.insideUnitCircle * currentAmplitude);
                shakeCamera.transform.position = originalCamPos + jitter;
            }
            else if (currentAmplitude != 0f)
            {
                shakeCamera.transform.position = originalCamPos;
                currentAmplitude = 0f;
            }
        }
    }
}
