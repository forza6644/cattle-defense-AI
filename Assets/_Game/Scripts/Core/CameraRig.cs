using UnityEngine;
using UnityEngine.EventSystems;

namespace Stonehold
{
    /// <summary>
    /// Adds life to the main camera: a slow idle sway plus a decaying trauma-based
    /// shake for impactful moments (cannon blasts, castle hits). Sits on the camera
    /// and offsets from its authored transform, so framing is preserved.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [SerializeField] private float swayAmplitude = 0.12f;
        [SerializeField] private float swaySpeed = 0.35f;
        [SerializeField] private float maxShakeOffset = 0.6f;
        [SerializeField] private float maxShakeAngle = 2.5f;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float trauma;

        private void Awake()
        {
            Instance = this;
            Camera cam = GetComponent<Camera>();
            if (GetComponent<PhysicsRaycaster>() == null)
            {
                gameObject.AddComponent<PhysicsRaycaster>();
            }
            if (cam != null)
            {
                const float referenceAspect = 9f / 16f;
                // Portrait composition: full-width wall in the lower quarter, with
                // enough vertical battlefield to read large enemy formations. Keep
                // this composition in editor Free Aspect too, so changing the Game
                // view does not fall back to the old close-up camera.
                const float referenceVerticalFov = 44f;

                transform.localPosition = new Vector3(0f, 34f, -11.5f);
                transform.localRotation = Quaternion.Euler(66f, 0f, 0f);

                float aspect = Mathf.Max(0.01f, (float)Screen.width / Screen.height);
                float referenceHorizontalFov = Camera.VerticalToHorizontalFieldOfView(referenceVerticalFov, referenceAspect);
                cam.fieldOfView = aspect < referenceAspect
                    ? Camera.HorizontalToVerticalFieldOfView(referenceHorizontalFov, aspect)
                    : referenceVerticalFov;
            }
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Add shake energy (0..1). Bigger events add more.</summary>
        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void LateUpdate()
        {
            float t = Time.unscaledTime;
            Vector3 sway = new Vector3(
                Mathf.Sin(t * swaySpeed) * swayAmplitude,
                Mathf.Cos(t * swaySpeed * 0.8f) * swayAmplitude * 0.5f,
                0f);

            Vector3 shakePos = Vector3.zero;
            Quaternion shakeRot = Quaternion.identity;
            if (trauma > 0f)
            {
                float shake = trauma * trauma; // ease
                shakePos = new Vector3(
                    (Mathf.PerlinNoise(t * 25f, 0f) * 2f - 1f) * maxShakeOffset * shake,
                    (Mathf.PerlinNoise(0f, t * 25f) * 2f - 1f) * maxShakeOffset * shake,
                    0f);
                float angle = (Mathf.PerlinNoise(t * 25f, 10f) * 2f - 1f) * maxShakeAngle * shake;
                shakeRot = Quaternion.Euler(0f, 0f, angle);
                trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.6f);
            }

            transform.localPosition = basePosition + sway + shakePos;
            transform.localRotation = baseRotation * shakeRot;
        }
    }
}
