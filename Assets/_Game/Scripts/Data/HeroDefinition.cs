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

        [Header("Targeting")]
        public TargetingMode defaultTargetingMode = TargetingMode.ClosestToGoal;

        [Header("Signature Ability")]
        public HeroAbilityType abilityType;
        [Min(1f)] public float abilityCooldown = 10f;
        [Min(1f)] public float abilityPowerMultiplier = 2f;
        [Min(0f)] public float abilityRadius = 3f;
        [Min(1)] public int abilityTargetCount = 3;
    }
}
