using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Lightweight world-space health bar. It uses shared instanced materials and
    /// only stays visible at full health for enemies that need extra readability.
    /// </summary>
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        private const float Width = 1.25f;
        private const float Height = 0.11f;

        private static Material backgroundMaterial;
        private static Material healthyMaterial;
        private static Material warningMaterial;
        private static Material bossMaterial;

        private Enemy enemy;
        private Transform barRoot;
        private Transform fill;
        private Renderer fillRenderer;
        private bool alwaysVisible;

        public void Configure(Enemy owner)
        {
            enemy = owner;
            string enemyName = owner != null && owner.Data != null
                ? owner.Data.enemyName.ToLowerInvariant()
                : string.Empty;
            alwaysVisible = enemyName.Contains("boss") || enemyName.Contains("brute") || enemyName.Contains("armor");
            Build(enemyName.Contains("boss"));
            Refresh();
        }

        private void LateUpdate()
        {
            if (enemy == null || barRoot == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                barRoot.rotation = camera.transform.rotation;
            }

            Refresh();
        }

        private void Build(bool isBoss)
        {
            EnsureMaterials();

            Transform existing = transform.Find("HealthBar");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            barRoot = new GameObject("HealthBar").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = Vector3.up * (isBoss ? 2.65f : 2.05f);
            barRoot.localScale = isBoss ? Vector3.one * 1.35f : Vector3.one;

            CreateBarPart("Background", backgroundMaterial, Width + 0.12f, Height + 0.08f, 0.02f);
            fill = CreateBarPart("Fill", isBoss ? bossMaterial : healthyMaterial, Width, Height, 0f);
            fillRenderer = fill.GetComponent<Renderer>();
        }

        private Transform CreateBarPart(string objectName, Material material, float width, float height, float z)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = objectName;
            part.transform.SetParent(barRoot, false);
            part.transform.localPosition = new Vector3(0f, 0f, z);
            part.transform.localScale = new Vector3(width, height, 0.035f);
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private void Refresh()
        {
            if (enemy == null || fill == null)
            {
                return;
            }

            float maxHealth = Mathf.Max(1f, enemy.MaxHealth);
            float ratio = Mathf.Clamp01(enemy.CurrentHealth / maxHealth);
            bool visible = !enemy.IsDead && (alwaysVisible || ratio < 0.995f);
            if (barRoot.gameObject.activeSelf != visible)
            {
                barRoot.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            fill.localScale = new Vector3(Width * ratio, Height, 0.035f);
            fill.localPosition = new Vector3(-Width * (1f - ratio) * 0.5f, 0f, 0f);
            if (!alwaysVisible && fillRenderer != null)
            {
                fillRenderer.sharedMaterial = ratio <= 0.35f ? warningMaterial : healthyMaterial;
            }
        }

        private static void EnsureMaterials()
        {
            if (backgroundMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            backgroundMaterial = CreateMaterial(shader, new Color(0.04f, 0.05f, 0.06f, 0.92f));
            healthyMaterial = CreateMaterial(shader, new Color(0.25f, 0.9f, 0.28f, 1f));
            warningMaterial = CreateMaterial(shader, new Color(1f, 0.22f, 0.12f, 1f));
            bossMaterial = CreateMaterial(shader, new Color(0.95f, 0.18f, 0.12f, 1f));
        }

        private static Material CreateMaterial(Shader shader, Color color)
        {
            Material material = new Material(shader)
            {
                color = color,
                enableInstancing = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }
    }
}
