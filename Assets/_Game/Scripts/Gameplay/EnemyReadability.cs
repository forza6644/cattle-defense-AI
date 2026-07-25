using System.Collections;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Presentation-only component attached to enemies for combat readability.
    /// Provides non-mutating hit flashes, target rim highlights via MaterialPropertyBlock,
    /// and a small grounded shadow blob without modifying base material assets.
    /// </summary>
    public class EnemyReadability : MonoBehaviour
    {
        private static Material sharedShadowMaterial;

        private Enemy ownerEnemy;
        private Renderer[] childRenderers;
        private MaterialPropertyBlock propBlock;
        private Coroutine flashRoutine;
        private Transform shadowTransform;
        private Color[] originalBaseColors;
        private bool isBossOrBrute;

        private void Awake()
        {
            ownerEnemy = GetComponent<Enemy>();
            childRenderers = GetComponentsInChildren<Renderer>(true);
            propBlock = new MaterialPropertyBlock();

            CacheOriginalColors();
            EnsureGroundShadow();
        }

        public void Configure(bool isBossOrBruteType)
        {
            isBossOrBrute = isBossOrBruteType;
            if (shadowTransform != null)
            {
                float shadowScale = isBossOrBrute ? 1.5f : 1.0f;
                shadowTransform.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }
        }

        public void PlayHitFlash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            if (gameObject.activeInHierarchy)
            {
                flashRoutine = StartCoroutine(DoHitFlash());
            }
        }

        public void ResetReadability()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            ClearPropertyBlock();
            if (shadowTransform != null)
            {
                shadowTransform.gameObject.SetActive(true);
            }
        }

        public void OnDeath()
        {
            if (shadowTransform != null)
            {
                shadowTransform.gameObject.SetActive(false);
            }
        }

        private void CacheOriginalColors()
        {
            if (childRenderers == null || childRenderers.Length == 0) return;
            originalBaseColors = new Color[childRenderers.Length];
            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] != null && childRenderers[i].sharedMaterial != null)
                {
                    if (childRenderers[i].sharedMaterial.HasProperty("_BaseColor"))
                    {
                        originalBaseColors[i] = childRenderers[i].sharedMaterial.GetColor("_BaseColor");
                    }
                    else if (childRenderers[i].sharedMaterial.HasProperty("_Color"))
                    {
                        originalBaseColors[i] = childRenderers[i].sharedMaterial.GetColor("_Color");
                    }
                    else
                    {
                        originalBaseColors[i] = Color.white;
                    }
                }
            }
        }

        private IEnumerator DoHitFlash()
        {
            const float duration = 0.08f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / duration);
                Color flashColor = new Color(0.85f * t, 0.85f * t, 0.85f * t, 1f);

                for (int i = 0; i < childRenderers.Length; i++)
                {
                    Renderer rend = childRenderers[i];
                    if (rend == null || rend == shadowTransform?.GetComponent<Renderer>()) continue;

                    rend.GetPropertyBlock(propBlock);
                    Color baseCol = originalBaseColors != null && i < originalBaseColors.Length ? originalBaseColors[i] : Color.white;
                    Color blended = Color.Lerp(baseCol, Color.white, t * 0.7f);
                    propBlock.SetColor("_BaseColor", blended);
                    propBlock.SetColor("_EmissionColor", flashColor);
                    rend.SetPropertyBlock(propBlock);
                }
                yield return null;
            }

            ClearPropertyBlock();
            flashRoutine = null;
        }

        private void ClearPropertyBlock()
        {
            if (childRenderers == null) return;
            for (int i = 0; i < childRenderers.Length; i++)
            {
                Renderer rend = childRenderers[i];
                if (rend == null || rend == shadowTransform?.GetComponent<Renderer>()) continue;
                rend.GetPropertyBlock(propBlock);
                propBlock.Clear();
                rend.SetPropertyBlock(propBlock);
            }
        }

        private static Texture2D sharedRadialTexture;

        private static Texture2D GetOrCreateRadialTexture()
        {
            if (sharedRadialTexture != null) return sharedRadialTexture;

            sharedRadialTexture = Resources.Load<Texture2D>("CombatReadability/RadialShadow");
            if (sharedRadialTexture == null)
            {
                int res = 64;
                sharedRadialTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
                {
                    name = "RuntimeRadialShadowTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                Color[] pixels = new Color[res * res];
                float center = (res - 1) * 0.5f;

                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        float dx = (x - center) / center;
                        float dy = (y - center) / center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(1f - dist);
                        alpha = Mathf.Pow(alpha, 1.8f) * 0.55f;
                        pixels[y * res + x] = new Color(0f, 0f, 0f, alpha);
                    }
                }
                sharedRadialTexture.SetPixels(pixels);
                sharedRadialTexture.Apply();
            }
            return sharedRadialTexture;
        }

        private void EnsureGroundShadow()
        {
            Transform existing = transform.Find("GroundShadow");
            if (existing != null)
            {
                shadowTransform = existing;
                return;
            }

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "GroundShadow";
            shadowTransform = shadow.transform;
            shadowTransform.SetParent(transform, false);
            shadowTransform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadowTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float scale = isBossOrBrute ? 1.5f : 1.0f;
            shadowTransform.localScale = new Vector3(scale, scale, 1f);

            Destroy(shadow.GetComponent<Collider>());

            Renderer rend = shadow.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            if (sharedShadowMaterial == null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) unlit = Shader.Find("Sprites/Default");

                sharedShadowMaterial = new Material(unlit)
                {
                    name = "RuntimeGroundShadowMaterial",
                    renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
                };

                sharedShadowMaterial.SetOverrideTag("RenderType", "Transparent");
                sharedShadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedShadowMaterial.DisableKeyword("_ALPHATEST_ON");

                if (sharedShadowMaterial.HasProperty("_Surface")) sharedShadowMaterial.SetFloat("_Surface", 1f);
                if (sharedShadowMaterial.HasProperty("_Blend")) sharedShadowMaterial.SetFloat("_Blend", 0f);
                if (sharedShadowMaterial.HasProperty("_SrcBlend")) sharedShadowMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (sharedShadowMaterial.HasProperty("_DstBlend")) sharedShadowMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (sharedShadowMaterial.HasProperty("_ZWrite")) sharedShadowMaterial.SetFloat("_ZWrite", 0f);

                Texture2D radialTex = GetOrCreateRadialTexture();
                if (sharedShadowMaterial.HasProperty("_BaseMap")) sharedShadowMaterial.SetTexture("_BaseMap", radialTex);
                if (sharedShadowMaterial.HasProperty("_MainTex")) sharedShadowMaterial.SetTexture("_MainTex", radialTex);
                if (sharedShadowMaterial.HasProperty("_BaseColor")) sharedShadowMaterial.SetColor("_BaseColor", Color.white);
                if (sharedShadowMaterial.HasProperty("_Color")) sharedShadowMaterial.SetColor("_Color", Color.white);
            }
            rend.sharedMaterial = sharedShadowMaterial;

            if (sharedShadowMaterial.shader != null && sharedShadowMaterial.shader.name.Contains("Universal") && !sharedShadowMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
            {
                rend.enabled = false;
            }
        }
    }
}
