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

            float charge = hero.AbilityCharge01;
            RefreshGeometry(charge);
            pulse = hero.IsAbilityReady ? (Mathf.Sin(Time.unscaledTime * 7f) + 1f) * 0.5f : 0f;
            Color color = Color.Lerp(identityColor * 0.55f, Color.white, pulse * 0.65f);
            color.a = hero.HasSignatureAbility ? Mathf.Lerp(0.35f, 1f, charge) : 0f;
            ring.startColor = color;
            ring.endColor = color;
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
