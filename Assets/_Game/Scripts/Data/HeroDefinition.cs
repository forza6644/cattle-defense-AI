using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "HeroDefinition", menuName = "Stonehold/Heroes/Hero Definition")]
    public class HeroDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject heroPrefab;
        public WeaponDefinition weapon;
        public float baseDamage;
        public float baseFireRate;
        public float baseRange;
    }
}
