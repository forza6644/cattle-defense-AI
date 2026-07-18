using System;
using System.Collections.Generic;

namespace Stonehold
{
    public sealed class DraftSelectionState
    {
        private readonly HashSet<string> activeHeroIds;
        private readonly HashSet<AttackType> activeAttackTypes;
        private readonly Dictionary<string, int> cardStacks;

        public DraftSelectionState(
            IEnumerable<string> activeHeroes,
            IEnumerable<AttackType> attackTypes,
            int openHeroSlots,
            IDictionary<string, int> stacks = null)
        {
            activeHeroIds = new HashSet<string>(activeHeroes ?? Array.Empty<string>(), StringComparer.Ordinal);
            activeAttackTypes = new HashSet<AttackType>(attackTypes ?? Array.Empty<AttackType>());
            OpenHeroSlots = Math.Max(0, openHeroSlots);
            cardStacks = stacks != null
                ? new Dictionary<string, int>(stacks, StringComparer.Ordinal)
                : new Dictionary<string, int>(StringComparer.Ordinal);
        }

        public int OpenHeroSlots { get; }
        public IReadOnlyCollection<string> ActiveHeroIds => activeHeroIds;
        public bool HasHero(string heroId) => !string.IsNullOrEmpty(heroId) && activeHeroIds.Contains(heroId);
        public bool HasAttackType(AttackType attackType) => activeAttackTypes.Contains(attackType);
        public int GetStacks(string cardId) => !string.IsNullOrEmpty(cardId) && cardStacks.TryGetValue(cardId, out int stacks) ? stacks : 0;
    }

    public readonly struct DraftCardChoice
    {
        public DraftCardChoice(CardDefinition card, CardRarity rarity, float weight)
        {
            Card = card;
            Rarity = rarity;
            Weight = weight;
        }

        public CardDefinition Card { get; }
        public CardRarity Rarity { get; }
        public float Weight { get; }
    }

    public static class CardDraftSelector
    {
        public static List<DraftCardChoice> Generate(
            IReadOnlyList<CardPoolEntry> entries,
            DraftSelectionState state,
            int count,
            RecruitOptionPolicy recruitPolicy,
            int? seed = null)
        {
            var eligible = new List<DraftCardChoice>(entries != null ? entries.Count : 0);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            if (entries != null && state != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    CardPoolEntry entry = entries[i];
                    CardDefinition card = entry?.card;
                    if (!IsEligible(card, state) || !IsFinitePositive(entry.weight) || !seenIds.Add(card.id))
                    {
                        continue;
                    }

                    eligible.Add(new DraftCardChoice(card, entry.rarity, entry.weight));
                }
            }

            var selected = new List<DraftCardChoice>(Math.Max(0, count));
            if (count <= 0 || eligible.Count == 0)
            {
                return selected;
            }

            var rng = seed.HasValue ? new Random(seed.Value) : new Random();
            if (recruitPolicy == RecruitOptionPolicy.GuaranteeWhileAvailable && state.OpenHeroSlots > 0)
            {
                var recruits = new List<DraftCardChoice>();
                for (int i = 0; i < eligible.Count; i++)
                {
                    if (eligible[i].Card.cardCategory == CardCategory.RecruitHero)
                    {
                        recruits.Add(eligible[i]);
                    }
                }

                if (recruits.Count > 0)
                {
                    DraftCardChoice recruit = PickWeighted(recruits, rng);
                    selected.Add(recruit);
                    RemoveCard(eligible, recruit.Card.id);
                }
            }

            while (selected.Count < count && eligible.Count > 0)
            {
                DraftCardChoice choice = PickWeighted(eligible, rng);
                selected.Add(choice);
                RemoveCard(eligible, choice.Card.id);
            }

            return selected;
        }

        public static bool IsEligible(CardDefinition card, DraftSelectionState state)
        {
            if (card == null || state == null || string.IsNullOrWhiteSpace(card.id))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(card.requiredOwnedHeroId) && !state.HasHero(card.requiredOwnedHeroId))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(card.blockedIfOwnedHeroId) && state.HasHero(card.blockedIfOwnedHeroId))
            {
                return false;
            }

            if (card.cardCategory == CardCategory.RecruitHero)
            {
                return state.OpenHeroSlots > 0
                    && !string.IsNullOrEmpty(card.recruitHeroId)
                    && !state.HasHero(card.recruitHeroId);
            }

            if (card.targetType == CardTargetType.HeroById && !state.HasHero(card.targetHeroId))
            {
                return false;
            }
            if (card.targetType == CardTargetType.AttackType && !state.HasAttackType(card.targetAttackType))
            {
                return false;
            }

            if (card.cardCategory == CardCategory.HeroUpgrade)
            {
                return card.behaviorUpgrade != null
                    && card.behaviorUpgrade.maxStacks > 0
                    && state.GetStacks(card.id) < card.behaviorUpgrade.maxStacks;
            }

            return true;
        }

        private static DraftCardChoice PickWeighted(List<DraftCardChoice> pool, Random rng)
        {
            double total = 0d;
            for (int i = 0; i < pool.Count; i++) total += pool[i].Weight;
            double roll = rng.NextDouble() * total;
            double running = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                running += pool[i].Weight;
                if (roll <= running) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private static void RemoveCard(List<DraftCardChoice> pool, string cardId)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (string.Equals(pool[i].Card.id, cardId, StringComparison.Ordinal)) pool.RemoveAt(i);
            }
        }

        private static bool IsFinitePositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
