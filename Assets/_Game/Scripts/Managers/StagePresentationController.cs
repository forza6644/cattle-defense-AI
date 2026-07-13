using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Applies stage-specific lighting, atmosphere, and environment dressing without
    /// mutating source asset materials or gameplay objects.
    /// </summary>
    public sealed class StagePresentationController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private const string NatureDressingName = "Stage1NatureDressing";
        private const string RuntimeDressingName = "StageIdentity_Runtime";

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

            ApplyNatureVariation(environment.transform, stageIndex);
            BuildStageIdentity(environment.transform, stageIndex);

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

        private static void ApplyNatureVariation(Transform environment, int stageIndex)
        {
            Transform nature = FindDescendant(environment, NatureDressingName);
            if (nature == null)
            {
                return;
            }

            int treeIndex = 0;
            for (int i = 0; i < nature.childCount; i++)
            {
                Transform prop = nature.GetChild(i);
                bool isTree = prop.name.StartsWith("Tree_", System.StringComparison.OrdinalIgnoreCase);
                bool isRock = prop.name.StartsWith("Rock_", System.StringComparison.OrdinalIgnoreCase);

                if (isTree)
                {
                    bool visible = stageIndex != 2 || treeIndex % 2 == 0;
                    prop.gameObject.SetActive(visible);

                    if (stageIndex == 1)
                    {
                        prop.localScale *= 0.92f;
                    }
                    else if (stageIndex == 2)
                    {
                        prop.localScale *= 0.86f;
                    }

                    treeIndex++;
                }
                else if (isRock && stageIndex > 0)
                {
                    prop.localScale *= stageIndex == 1 ? 1.14f : 1.06f;
                }
            }
        }

        private static void BuildStageIdentity(Transform environment, int stageIndex)
        {
            Transform existing = environment.Find(RuntimeDressingName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject root = new GameObject(RuntimeDressingName);
            root.transform.SetParent(environment, false);

            BuildAtmosphere(root.transform, stageIndex);
            if (stageIndex == 2)
            {
                BuildFrostCrystals(root.transform);
            }
        }

        private static void BuildAtmosphere(Transform parent, int stageIndex)
        {
            GameObject atmosphere = new GameObject("StageAtmosphere");
            atmosphere.transform.SetParent(parent, false);
            atmosphere.transform.localPosition = new Vector3(0f, 8f, 1f);

            ParticleSystem particles = atmosphere.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = stageIndex == 2 ? 72 : 36;
            main.startLifetime = stageIndex == 2 ? 6.5f : 4.5f;
            main.startSpeed = stageIndex == 2 ? 0.18f : 0.12f;
            main.startSize = stageIndex == 2 ? 0.09f : 0.055f;
            main.gravityModifier = stageIndex == 2 ? 0.035f : 0.015f;
            main.startColor = GetAtmosphereColor(stageIndex);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = stageIndex == 2 ? 10f : stageIndex == 1 ? 4f : 3f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 0.5f, 18f);
            shape.randomDirectionAmount = 0.22f;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader != null)
            {
                Material material = new Material(shader)
                {
                    name = "StageAtmosphere_RuntimeMaterial"
                };
                Color color = GetAtmosphereColor(stageIndex);
                if (material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, color);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }

                particleRenderer.sharedMaterial = material;
            }
        }

        private static void BuildFrostCrystals(Transform parent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                return;
            }

            Material crystalMaterial = new Material(shader)
            {
                name = "FrostCrystal_RuntimeMaterial"
            };
            Color crystalColor = new Color(0.32f, 0.82f, 1f);
            crystalMaterial.SetColor(BaseColorId, crystalColor);
            crystalMaterial.EnableKeyword("_EMISSION");
            crystalMaterial.SetColor(EmissionColorId, crystalColor * 0.55f);

            Vector3[] positions =
            {
                new Vector3(-7.1f, 0.35f, 5.8f),
                new Vector3(7.2f, 0.35f, 4.6f),
                new Vector3(-6.8f, 0.35f, -0.4f),
                new Vector3(7f, 0.35f, -2.6f),
                new Vector3(-7.2f, 0.35f, -5.4f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject cluster = new GameObject($"FrostCrystalCluster_{i + 1:00}");
                cluster.transform.SetParent(parent, false);
                cluster.transform.localPosition = positions[i];
                cluster.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);

                CreateCrystal(cluster.transform, crystalMaterial, new Vector3(0f, 0.45f, 0f), 0.18f, 0.9f, -8f);
                CreateCrystal(cluster.transform, crystalMaterial, new Vector3(-0.2f, 0.28f, 0.05f), 0.13f, 0.56f, 18f);
                CreateCrystal(cluster.transform, crystalMaterial, new Vector3(0.19f, 0.24f, -0.04f), 0.11f, 0.48f, -22f);
            }
        }

        private static void CreateCrystal(
            Transform parent,
            Material material,
            Vector3 localPosition,
            float width,
            float height,
            float tilt)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.name = "Crystal";
            crystal.transform.SetParent(parent, false);
            crystal.transform.localPosition = localPosition;
            crystal.transform.localScale = new Vector3(width, height, width);
            crystal.transform.localRotation = Quaternion.Euler(0f, 45f, tilt);
            crystal.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = crystal.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static Color GetAtmosphereColor(int stageIndex)
        {
            switch (stageIndex)
            {
                case 1:
                    return new Color(1f, 0.72f, 0.38f, 0.38f);
                case 2:
                    return new Color(0.82f, 0.94f, 1f, 0.78f);
                default:
                    return new Color(0.72f, 1f, 0.56f, 0.28f);
            }
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDescendant(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
