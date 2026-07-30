using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Presentation-only controller for a battlefield enemy portal. Wave and pooling
    /// ownership remain in their existing managers; this component only animates a
    /// configured portal and plays a short flare when notified of a spawn.
    /// </summary>
    public sealed class EnemySpawnPortal : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private bool isActivePortal;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private Renderer portalSurface;
        [SerializeField] private Renderer groundGlow;
        [SerializeField] private Renderer[] arcaneAccents;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material dormantMaterial;
        [SerializeField] private ParticleSystem spawnBurst;
        [SerializeField, Min(0.05f)] private float pulseFrequency = 1.4f;
        [SerializeField, Min(0.05f)] private float flareDuration = 0.28f;

        private MaterialPropertyBlock propertyBlock;
        private float pulseTime;
        private float flareRemaining;

        public bool IsActivePortal => isActivePortal;
        public Transform SpawnAnchor => spawnAnchor != null ? spawnAnchor : transform;

        private void Awake()
        {
            EnsurePropertyBlock();
            ApplyPortalState();
        }

        private void OnEnable()
        {
            EnsurePropertyBlock();
            ApplyPortalState();
        }

        private void Update()
        {
            if (!isActivePortal)
            {
                return;
            }

            pulseTime += Time.deltaTime * pulseFrequency;
            flareRemaining = Mathf.Max(0f, flareRemaining - Time.deltaTime);

            float pulse = 0.82f + (Mathf.Sin(pulseTime * Mathf.PI * 2f) + 1f) * 0.18f;
            float flareNormalized = flareDuration > 0f ? (flareRemaining / flareDuration) : 0f;
            float flareBoost = flareNormalized * 1.35f;

            Color baseSurfaceEmission = new Color(0.42f, 0.03f, 0.65f, 1f);
            Color baseGroundEmission = new Color(0.20f, 0.01f, 0.35f, 1f);
            Color baseAccentEmission = new Color(0.32f, 0.03f, 0.52f, 1f);

            ApplyEmission(portalSurface, baseSurfaceEmission * (pulse + flareBoost));
            ApplyEmission(groundGlow, baseGroundEmission * (0.75f + flareBoost * 0.7f));

            if (arcaneAccents != null)
            {
                for (int i = 0; i < arcaneAccents.Length; i++)
                {
                    ApplyEmission(arcaneAccents[i], baseAccentEmission * (pulse + flareBoost * 0.4f));
                }
            }
        }

        /// <summary>Called by WaveManager after an existing pooled enemy activates.</summary>
        public void PlaySpawnFlare()
        {
            if (!isActivePortal)
            {
                return;
            }

            flareRemaining = flareDuration;
            if (spawnBurst != null)
            {
                spawnBurst.Play(true);
            }
        }

        public void SetActiveState(bool active)
        {
            isActivePortal = active;
            EnsurePropertyBlock();
            ApplyPortalState();
        }

        private void ApplyPortalState()
        {
            Material material = isActivePortal ? activeMaterial : dormantMaterial;
            if (portalSurface != null && material != null)
            {
                portalSurface.sharedMaterial = material;
            }
            if (groundGlow != null && material != null)
            {
                groundGlow.sharedMaterial = material;
            }

            if (arcaneAccents != null)
            {
                for (int i = 0; i < arcaneAccents.Length; i++)
                {
                    if (arcaneAccents[i] != null && material != null)
                    {
                        arcaneAccents[i].sharedMaterial = material;
                    }
                }
            }

            if (!isActivePortal)
            {
                ApplyEmission(portalSurface, new Color(0.035f, 0.005f, 0.055f, 1f));
                ApplyEmission(groundGlow, new Color(0.012f, 0.002f, 0.022f, 1f));
            }
        }

        private void ApplyEmission(Renderer target, Color color)
        {
            if (target == null)
            {
                return;
            }

            EnsurePropertyBlock();
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, color);
            target.SetPropertyBlock(propertyBlock);
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
