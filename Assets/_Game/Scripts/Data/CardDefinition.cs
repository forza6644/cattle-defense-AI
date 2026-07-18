using UnityEngine;

namespace Stonehold
{
    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    public enum CardTargetType
    {
        Global,
        HeroById,
        AttackType
    }

    public enum CardModifierType
    {
        DamageMultiplier,
        FireRateMultiplier,
        RangeMultiplier,
        BurnDamageAdd,
        SlowStrengthAdd,
        ShockEnable,
        AbilityCooldownReduction,
        AbilityDamageMultiplier,
        AbilityRadiusMultiplier,
        AbilityExtraProjOrChain,
        BurnDurationAdd,
        SlowDurationAdd,
        CritChanceAdd,
        CritMultiplierAdd
    }

    public enum CardCategory
    {
        Modifier = 0,
        RecruitHero = 1,
        HeroUpgrade = 2,
        GlobalUpgrade = 3,
        Trap = 4,
        BattlefieldDefense = 5,
        CastleUpgrade = 6,
        LegendaryModifier = 7,
        Reroll = 8
    }

    [CreateAssetMenu(fileName = "CardDefinition", menuName = "Stonehold/Cards/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)]
        public string description;
        public Sprite icon;
        public CardCategory cardCategory = CardCategory.Modifier;
        public string recruitHeroId;
        public string requiredOwnedHeroId;
        public string blockedIfOwnedHeroId;
        public CardRarity rarity = CardRarity.Common;
        public CardTargetType targetType = CardTargetType.Global;
        public string targetHeroId;
        public AttackType targetAttackType = AttackType.SingleTarget;
        public CardModifierType modifierType = CardModifierType.DamageMultiplier;
        public float modifierValue;
        public float weight = 1f;
        [Min(1)] public int maxStacks = 1;

        [Header("Gameplay Expansion Contracts")]
        public HeroBehaviorUpgradeData behaviorUpgrade;
        public TrapDefinition trapDefinition;
        public BattlefieldDefenseDefinition battlefieldDefenseDefinition;
        public CastleUpgradeDefinition castleUpgradeDefinition;
        public RerollDefinition rerollDefinition;
    }
}
