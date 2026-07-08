using UnityEngine;

namespace Stonehold
{
    public enum CardRarity
    {
        Common,
        Rare,
        Epic
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
        ShockEnable
    }

    public enum CardCategory
    {
        Modifier,
        RecruitHero
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
    }
}
