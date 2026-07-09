using UnityEngine;

namespace Stonehold
{
    public class HeroSlot : MonoBehaviour
    {
        public HeroDefinition startingHero;

        private HeroAttack currentHero;

        public HeroAttack CurrentHero => currentHero;
        public bool IsOccupied => currentHero != null;

        private void Start()
        {
            // Create a small, thin slate pad under the slot for visual clarity
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "SlotPad_Visual";
            pad.transform.SetParent(transform);
            pad.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            pad.transform.localRotation = Quaternion.identity;
            pad.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);

            Renderer r = pad.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = new Color(0.2f, 0.25f, 0.3f);
            }

            Collider c = pad.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c);
            }

            if (HeroRosterManager.Instance != null)
            {
                HeroRosterManager.Instance.RegisterSlot(this);
                return;
            }

            if (startingHero != null)
            {
                SpawnHero(startingHero);
            }
        }

        public bool SpawnHero(HeroDefinition hero)
        {
            if (hero == null || hero.heroPrefab == null || IsOccupied)
            {
                return false;
            }

            GameObject instance = Instantiate(hero.heroPrefab, transform.position, transform.rotation, transform);
            instance.name = hero.displayName + " Hero";
            instance.transform.localScale = Vector3.one * 1.35f; // Scale up for better visibility

            Tower[] legacyTowers = instance.GetComponentsInChildren<Tower>();
            for (int i = 0; i < legacyTowers.Length; i++)
            {
                legacyTowers[i].enabled = false;
            }

            currentHero = instance.GetComponent<HeroAttack>();
            if (currentHero == null)
            {
                currentHero = instance.AddComponent<HeroAttack>();
            }

            if (hero.id == "fire_mage")
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0.2f));
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            mat.SetColor("_Color", new Color(1f, 0.2f, 0.2f));
                        }
                    }
                }
            }

            currentHero.Configure(hero);
            return true;
        }

        public void ClearHero()
        {
            if (currentHero == null)
            {
                return;
            }

            Destroy(currentHero.gameObject);
            currentHero = null;
        }
    }
}
