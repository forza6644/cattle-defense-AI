using System;
using UnityEngine;

namespace Stonehold
{
    public enum HeroBehaviorEffectType
    {
        None = 0,
        Multishot = 1,
        ExtraProjectile = 2,
        SplitProjectile = 3,
        Ricochet = 4,
        Piercing = 5,
        BurnZone = 6,
        ExtraChain = 7,
        ExplosionRadius = 8,
        ExtraCast = 9,
        CriticalBehavior = 10
    }

    public enum PlacementMode
    {
        Automatic = 0,
        FixedSlot = 1,
        LaneAnchor = 2,
        CastleFront = 3
    }

    public enum BattlefieldEffectType
    {
        None = 0,
        Damage = 1,
        Slow = 2,
        Burn = 3,
        Block = 4,
        Support = 5
    }

    public enum TrapRuntimeType
    {
        Caltrops = 0,
        BurningOil = 1
    }

    public enum CastleUpgradeType
    {
        MaxHealth = 0,
        Regeneration = 1,
        DamageReduction = 2,
        Repair = 3
    }

    public enum RerollCostType
    {
        Free = 0,
        RunCurrency = 1,
        MetaCurrency = 2
    }

    public enum EnemyClassification
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }

    public enum DamageType
    {
        Physical = 0,
        Fire = 1,
        Frost = 2,
        Electric = 3,
        Explosive = 4
    }

    [Serializable]
    public class HeroBehaviorUpgradeData
    {
        public HeroBehaviorEffectType effectType = HeroBehaviorEffectType.None;
        public CardTargetType targetType = CardTargetType.HeroById;
        public string targetHeroId;
        public AttackType targetAttackType = AttackType.SingleTarget;
        public int integerValue;
        public float floatValue;
        [Range(0f, 1f)] public float percentageValue;
        [Min(0f)] public float duration;
        [Min(0)] public int count;
        public float secondaryValue;
        [Min(1)] public int maxStacks = 1;
    }

    [Serializable]
    public struct ElementalResistanceProfile
    {
        [Range(0f, 1f)] public float physical;
        [Range(0f, 1f)] public float fire;
        [Range(0f, 1f)] public float frost;
        [Range(0f, 1f)] public float electric;
        [Range(0f, 1f)] public float explosive;

        public float Get(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical: return physical;
                case DamageType.Fire: return fire;
                case DamageType.Frost: return frost;
                case DamageType.Electric: return electric;
                case DamageType.Explosive: return explosive;
                default: return 0f;
            }
        }
    }

    public readonly struct DamageContext
    {
        public DamageContext(float rawDamage, DamageType damageType, string sourceHeroId, bool isCritical = false, float armorPiercing = 0f)
        {
            RawDamage = rawDamage;
            DamageType = damageType;
            SourceHeroId = sourceHeroId;
            IsCritical = isCritical;
            ArmorPiercing = armorPiercing;
        }

        public float RawDamage { get; }
        public DamageType DamageType { get; }
        public string SourceHeroId { get; }
        public bool IsCritical { get; }
        public float ArmorPiercing { get; }
    }

    public abstract class BattlefieldContentDefinition : ScriptableObject
    {
        public string stableId;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public CardRarity rarity = CardRarity.Common;
        public GameObject prefab;
        public PlacementMode placementMode = PlacementMode.Automatic;
        [Min(0f)] public float duration;
        [Min(0)] public int charges;
        [Min(0f)] public float damage;
        [Min(0f)] public float effectRadius;
        [Min(0f)] public float triggerInterval;
        public BattlefieldEffectType effectType;
        public StatusEffectType statusEffectType = StatusEffectType.None;
        public float statusEffectValue;
        [Min(0f)] public float statusEffectDuration;
    }

    [CreateAssetMenu(fileName = "CastleUpgradeDefinition", menuName = "Stonehold/Expansion/Castle Upgrade Definition")]
    public class CastleUpgradeDefinition : ScriptableObject
    {
        public string stableId;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public CardRarity rarity = CardRarity.Common;
        public CastleUpgradeType upgradeType;
        public float value;
        [Min(1)] public int maxStacks = 1;
    }

    [CreateAssetMenu(fileName = "RerollDefinition", menuName = "Stonehold/Expansion/Reroll Definition")]
    public class RerollDefinition : ScriptableObject
    {
        public string stableId;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public RerollCostType costType = RerollCostType.Free;
        [Min(0)] public int baseCost;
        [Min(0)] public int costIncreasePerUse;
        [Min(1)] public int maxUsesPerDraft = 1;
    }
}
