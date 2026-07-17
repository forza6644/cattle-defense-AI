using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Stores the active cards drafted during the current run and provides methods to query cumulative stat modifiers.
    /// Cleared automatically when a new run begins (GameManager Awake).
    /// </summary>
    public class RunModifierManager : MonoBehaviour
    {
        public static RunModifierManager Instance { get; private set; }

        private readonly List<CardDefinition> activeCards = new List<CardDefinition>();
        private readonly Dictionary<BehaviorKey, ActiveBehaviorState> behaviorUpgrades = new Dictionary<BehaviorKey, ActiveBehaviorState>();

        public IReadOnlyList<CardDefinition> ActiveCards => activeCards;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddCard(CardDefinition card)
        {
            if (card != null)
            {
                activeCards.Add(card);
                if (card.cardCategory == CardCategory.HeroUpgrade && card.behaviorUpgrade != null)
                {
                    ApplyBehaviorUpgrade(card.behaviorUpgrade);
                }
                Debug.Log($"[RunModifierManager] Card modifier added: {card.displayName} ({card.modifierType} +{card.modifierValue})");
            }
        }

        public void ClearModifiers()
        {
            activeCards.Clear();
            behaviorUpgrades.Clear();
            Debug.Log("[RunModifierManager] Cleared all run modifiers and behavior upgrades.");
        }

        public float GetDamageMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.DamageMultiplier)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult += card.modifierValue;
                    }
                }
            }
            return mult;
        }

        public float GetFireRateMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.FireRateMultiplier)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult += card.modifierValue;
                    }
                }
            }
            return mult;
        }

        public float GetRangeMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.RangeMultiplier)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult += card.modifierValue;
                    }
                }
            }
            return mult;
        }

        public float GetBurnDamageAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.BurnDamageAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.DoT))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        public float GetSlowStrengthAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.SlowStrengthAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.Slow))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        public bool IsShockEnabled(string heroId)
        {
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.ShockEnable)
                {
                    if (IsTargetMatch(card, heroId, AttackType.Chain))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public float GetAbilityCooldownMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.AbilityCooldownReduction)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult -= card.modifierValue;
                    }
                }
            }
            return Mathf.Max(0.1f, mult);
        }

        public float GetAbilityDamageMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.AbilityDamageMultiplier)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult += card.modifierValue;
                    }
                }
            }
            return mult;
        }

        public float GetAbilityRadiusMultiplier(string heroId)
        {
            float mult = 1.0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.AbilityRadiusMultiplier)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        mult += card.modifierValue;
                    }
                }
            }
            return mult;
        }

        public int GetAbilityExtraProjOrChain(string heroId)
        {
            int add = 0;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.AbilityExtraProjOrChain)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        add += Mathf.RoundToInt(card.modifierValue);
                    }
                }
            }
            return add;
        }

        public float GetBurnDurationAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.BurnDurationAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.DoT))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        public float GetSlowDurationAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.SlowDurationAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.Slow))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        public float GetCritChanceAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.CritChanceAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        public float GetCritMultiplierAdd(string heroId)
        {
            float add = 0f;
            foreach (var card in activeCards)
            {
                if (card.modifierType == CardModifierType.CritMultiplierAdd)
                {
                    if (IsTargetMatch(card, heroId, AttackType.SingleTarget))
                    {
                        add += card.modifierValue;
                    }
                }
            }
            return add;
        }

        private bool IsTargetMatch(CardDefinition card, string heroId, AttackType actionAttackType)
        {
            switch (card.targetType)
            {
                case CardTargetType.Global:
                    return true;
                case CardTargetType.HeroById:
                    return card.targetHeroId == heroId;
                case CardTargetType.AttackType:
                    return IsHeroAttackTypeMatch(heroId, card.targetAttackType);
                default:
                    return false;
            }
        }

        private bool IsHeroAttackTypeMatch(string heroId, AttackType requiredType)
        {
            var heroes = FindObjectsByType<HeroAttack>(FindObjectsSortMode.None);
            foreach (var h in heroes)
            {
                if (h.Definition != null && h.Definition.id == heroId && h.Definition.weapon != null)
                {
                    return h.Definition.weapon.attackType == requiredType;
                }
            }
            return false;
        }

        private void ApplyBehaviorUpgrade(HeroBehaviorUpgradeData upgrade)
        {
            if (upgrade == null || upgrade.effectType == HeroBehaviorEffectType.None)
            {
                return;
            }

            string heroId = upgrade.targetType == CardTargetType.HeroById ? upgrade.targetHeroId : "";
            if (string.IsNullOrEmpty(heroId))
            {
                return;
            }

            var key = new BehaviorKey(heroId, upgrade.effectType);
            if (!behaviorUpgrades.TryGetValue(key, out var state))
            {
                state = new ActiveBehaviorState();
                behaviorUpgrades[key] = state;
            }

            if (state.stacks < upgrade.maxStacks)
            {
                state.stacks++;
                state.integerValue += upgrade.integerValue;
                state.floatValue += upgrade.floatValue;
                state.percentageValue = Mathf.Clamp01(state.percentageValue + upgrade.percentageValue);
                state.duration += upgrade.duration;
                state.count += upgrade.count;
                state.secondaryValue += upgrade.secondaryValue;

                Debug.Log($"[RunModifierManager] Applied behavior upgrade: {heroId} {upgrade.effectType} Stack: {state.stacks}/{upgrade.maxStacks}");
            }
            else
            {
                Debug.LogWarning($"[RunModifierManager] Exceeded max stacks ({upgrade.maxStacks}) for behavior upgrade {heroId} {upgrade.effectType}. Rejected.");
            }
        }

        public int GetBehaviorStacks(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return 0;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) ? state.stacks : 0;
        }

        public int GetBehaviorCount(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return 0;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) ? state.count : 0;
        }

        public float GetBehaviorFloat(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return 0f;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) ? state.floatValue : 0f;
        }

        public float GetBehaviorPercentage(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return 0f;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) ? state.percentageValue : 0f;
        }

        public float GetBehaviorSecondaryValue(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return 0f;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) ? state.secondaryValue : 0f;
        }

        public bool HasBehavior(string heroId, HeroBehaviorEffectType effectType)
        {
            if (string.IsNullOrEmpty(heroId)) return false;
            var key = new BehaviorKey(heroId, effectType);
            return behaviorUpgrades.TryGetValue(key, out var state) && state.stacks > 0;
        }

        private struct BehaviorKey : System.IEquatable<BehaviorKey>
        {
            public readonly string heroId;
            public readonly HeroBehaviorEffectType effectType;

            public BehaviorKey(string heroId, HeroBehaviorEffectType effectType)
            {
                this.heroId = heroId ?? "";
                this.effectType = effectType;
            }

            public bool Equals(BehaviorKey other)
            {
                return effectType == other.effectType && string.Equals(heroId, other.heroId, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is BehaviorKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((heroId != null ? heroId.GetHashCode() : 0) * 397) ^ (int)effectType;
                }
            }
        }

        private class ActiveBehaviorState
        {
            public int stacks;
            public int integerValue;
            public float floatValue;
            public float percentageValue;
            public float duration;
            public int count;
            public float secondaryValue;
        }
    }
}
