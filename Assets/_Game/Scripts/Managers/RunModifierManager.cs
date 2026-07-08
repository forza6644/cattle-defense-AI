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
                Debug.Log($"[RunModifierManager] Card modifier added: {card.displayName} ({card.modifierType} +{card.modifierValue})");
            }
        }

        public void ClearModifiers()
        {
            activeCards.Clear();
            Debug.Log("[RunModifierManager] Cleared all run modifiers.");
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
    }
}
