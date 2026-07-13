using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Applies a lightweight stage identity without mutating source asset materials.
    /// Stage-specific art can replace this layer later without touching gameplay.
    /// </summary>
    public sealed class StagePresentationController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Start()
        {
            ApplyTheme(Mathf.Clamp(SaveManager.SelectedStageIndex, 0, 2));
        }

        private void ApplyTheme(int stageIndex)
        {
            Color sky;
            Color lightColor;
            Color environmentTint;
            Color fogColor;
            float lightIntensity;

            switch (stageIndex)
            {
                case 1:
                    sky = new Color(0.48f, 0.62f, 0.68f);
                    lightColor = new Color(1f, 0.82f, 0.62f);
                    environmentTint = new Color(0.78f, 0.72f, 0.58f);
                    fogColor = new Color(0.52f, 0.58f, 0.56f);
                    lightIntensity = 1.15f;
                    break;
                case 2:
                    sky = new Color(0.55f, 0.7f, 0.82f);
                    lightColor = new Color(0.78f, 0.9f, 1f);
                    environmentTint = new Color(0.68f, 0.82f, 0.9f);
                    fogColor = new Color(0.68f, 0.78f, 0.86f);
                    lightIntensity = 1.05f;
                    break;
                default:
                    sky = new Color(0.48f, 0.72f, 0.84f);
                    lightColor = new Color(1f, 0.94f, 0.78f);
                    environmentTint = Color.white;
                    fogColor = new Color(0.63f, 0.76f, 0.72f);
                    lightIntensity = 1.2f;
                    break;
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = sky;
            }

            Light sun = FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = lightColor;
                sun.intensity = lightIntensity;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = 52f;
            RenderSettings.fogEndDistance = 95f;

            GameObject environment = GameObject.Find("Environment");
            if (environment == null)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material material = renderer.sharedMaterial;
                if (material == null || !material.HasProperty(BaseColorId))
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                Color sourceColor = material.GetColor(BaseColorId);
                block.SetColor(BaseColorId, Color.Lerp(sourceColor, sourceColor * environmentTint, 0.28f));
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
