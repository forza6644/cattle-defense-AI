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

        [Header("Gameplay Presentation")]
        [Tooltip("Optional final gameplay calibration on top of the renderer-bounds envelope. 1 = envelope only. Values <= 0 are treated as 1. Does not affect Main Menu showcase scale.")]
        public float gameplayVisualScaleMultiplier = 1f;
    }
}
