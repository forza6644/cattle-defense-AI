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

            currentHero.Configure(hero);
            return true;
        }
    }
}
