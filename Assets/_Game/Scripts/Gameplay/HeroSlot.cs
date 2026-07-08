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
