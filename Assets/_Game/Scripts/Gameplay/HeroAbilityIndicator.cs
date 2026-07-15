using UnityEngine;

namespace Stonehold
{
    /// <summary>World-space segmented ring showing signature ability charge.</summary>
    public sealed class HeroAbilityIndicator : MonoBehaviour
    {
        private const int Segments = 32;
        private HeroAttack hero;
        private LineRenderer ring;
        private Color identityColor;
        private float pulse;

        private float lastCharge = 0f;
        private float activationPulseTimer = 0f;
        private const float activationPulseDuration = 0.4f;

        public void Configure(HeroAttack owner, Color color)
        {
            hero = owner;
            identityColor = color;

            GameObject ringObject = new GameObject("AbilityChargeRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ringObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = false;
            ring.widthMultiplier = 0.075f;
            ring.numCapVertices = 2;
            ring.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ring.material.hideFlags = HideFlags.HideAndDontSave;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            RefreshGeometry(0f);
        }

        private void Update()
        {
            if (hero == null || ring == null)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                // Freeze visual timer decrement, but still draw the current ring state
                if (activationPulseTimer > 0f)
                {
                    float progress = 1f - (activationPulseTimer / activationPulseDuration);
                    DrawCircle(Mathf.Lerp(0.56f, 1.3f, progress));
                }
                else
                {
                    RefreshGeometry(hero.AbilityCharge01);
                }
                return;
            }

            float charge = hero.AbilityCharge01;

            // Detect activation: charge goes from 1.0 down to a low value
            if (lastCharge >= 0.95f && charge < 0.2f && activationPulseTimer <= 0f)
            {
                activationPulseTimer = activationPulseDuration;
            }
            lastCharge = charge;

            if (activationPulseTimer > 0f)
            {
                activationPulseTimer -= Time.deltaTime;
                float progress = 1f - (activationPulseTimer / activationPulseDuration);
                float currentRadius = Mathf.Lerp(0.56f, 1.3f, progress);
                DrawCircle(currentRadius);
                Color col = identityColor;
                col.a = Mathf.Lerp(1f, 0f, progress);
                ring.startColor = col;
                ring.endColor = col;
            }
            else
            {
                RefreshGeometry(charge);
                pulse = hero.IsAbilityReady ? (Mathf.Sin(Time.unscaledTime * 7f) + 1f) * 0.5f : 0f;
                Color color = Color.Lerp(identityColor * 0.55f, Color.white, pulse * 0.65f);
                color.a = hero.HasSignatureAbility ? Mathf.Lerp(0.35f, 1f, charge) : 0f;
                ring.startColor = color;
                ring.endColor = color;
            }
        }

        private void DrawCircle(float radius)
        {
            ring.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = Mathf.PI * 2f * i / Segments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void RefreshGeometry(float charge)
        {
            int visibleSegments = Mathf.Clamp(Mathf.CeilToInt(Segments * charge), 1, Segments);
            ring.positionCount = visibleSegments + 1;
            const float radius = 0.56f;
            for (int i = 0; i <= visibleSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / Segments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
