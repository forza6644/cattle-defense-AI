using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Configuration component for project-owned prefab wrappers.
    /// Keeps gameplay components separate from visual scale, rotation, and offsets.
    /// </summary>
    public class ArtAdapter : MonoBehaviour
    {
        [Header("Visual Configuration")]
        public Vector3 visualScale = Vector3.one;
        public Vector3 visualRotation = Vector3.zero;
        public Vector3 visualOffset = Vector3.zero;

        [Header("References")]
        public Transform visualRoot;
        public Transform muzzleTransform;
        public Transform abilityOrigin;
        public Transform impactPoint;
        public Animator animatorReference;

        private void Awake()
        {
            ApplyVisualTransform();
        }

        public void ApplyVisualTransform()
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = visualScale;
                visualRoot.localRotation = Quaternion.Euler(visualRotation);
                visualRoot.localPosition = visualOffset;
            }
        }
    }
}
