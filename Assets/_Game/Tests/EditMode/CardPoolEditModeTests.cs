using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public class CardPoolEditModeTests
    {
        private const string PoolPath = "Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset";
        private CardPoolDefinition pool;

        [SetUp]
        public void SetUp()
        {
            pool = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>(PoolPath);
            Assert.That(pool, Is.Not.Null);
        }

        [Test] public void VerticalSlice18_HasExactly18UniqueCards() => Assert.That(pool.cards.Select(x => x.card.id).Distinct().Count(), Is.EqualTo(18));
        [Test] public void ProductionPool_RemainsExactly39() => Assert.That(Resources.LoadAll<CardDefinition>("Cards").Length, Is.EqualTo(39));
        [Test] public void ControlledStartingHero_IsArcher() => Assert.That(pool.startingHeroId, Is.EqualTo("archer"));

        [Test]
        public void IntendedRecruitCards_ArePresent()
        {
            CollectionAssert.AreEquivalent(new[] { "bombardier", "frost_mage", "electric_engineer" },
                pool.cards.Where(x => x.card.cardCategory == CardCategory.RecruitHero).Select(x => x.card.recruitHeroId));
        }

        [Test]
        public void AllTask13CUpgrades_ArePresent()
        {
            string[] expected =
            {
                "archer_twin_volley", "archer_piercing_arrows", "bombardier_cluster_shells", "bombardier_wide_blast",
                "frost_mage_shard_volley", "frost_mage_echoing_nova", "electric_engineer_extended_circuit", "electric_engineer_forked_current"
            };
            CollectionAssert.IsSubsetOf(expected, pool.cards.Select(x => x.card.id).ToArray());
        }

        [Test]
        public void SevenSupportModifiers_ArePresent()
        {
            string[] expected = { "war_training", "battle_rhythm", "watchtower_expansion", "fast_casting", "empowered_abilities", "frostbite", "wide_blast" };
            CollectionAssert.AreEquivalent(expected, pool.cards.Where(x => x.card.cardCategory == CardCategory.Modifier).Select(x => x.card.id));
        }

        [Test]
        public void CategoryDistribution_IsThreeEightSeven()
        {
            Assert.That(pool.cards.Count(x => x.card.cardCategory == CardCategory.RecruitHero), Is.EqualTo(3));
            Assert.That(pool.cards.Count(x => x.card.cardCategory == CardCategory.HeroUpgrade), Is.EqualTo(8));
            Assert.That(pool.cards.Count(x => x.card.cardCategory == CardCategory.Modifier), Is.EqualTo(7));
        }

        [Test]
        public void EffectiveRarityDistribution_IsTenSixTwo()
        {
            Assert.That(pool.cards.Count(x => x.rarity == CardRarity.Common), Is.EqualTo(10));
            Assert.That(pool.cards.Count(x => x.rarity == CardRarity.Rare), Is.EqualTo(6));
            Assert.That(pool.cards.Count(x => x.rarity == CardRarity.Epic), Is.EqualTo(2));
            Assert.That(pool.cards.Count(x => x.rarity == CardRarity.Legendary), Is.Zero);
        }

        [Test]
        public void Validator_AcceptsQualifiedPool()
        {
            List<GameplayValidationIssue> issues = GameplayDataValidation.ValidateCardPool(pool);
            Assert.That(GameplayDataValidation.HasErrors(issues), Is.False, Format(issues));
        }

        [Test]
        public void FixedSeed_IsDeterministic()
        {
            DraftSelectionState state = State(new[] { "archer" }, 3);
            CollectionAssert.AreEqual(Ids(Generate(state, 42)), Ids(Generate(state, 42)));
        }

        [Test]
        public void DifferentSeeds_CanVaryChoices()
        {
            DraftSelectionState state = State(new[] { "archer" }, 3);
            string baseline = string.Join("|", Ids(Generate(state, 1)));
            Assert.That(Enumerable.Range(2, 20).Any(seed => string.Join("|", Ids(Generate(state, seed))) != baseline), Is.True);
        }

        [Test]
        public void Draft_NeverContainsDuplicateCards()
        {
            DraftSelectionState state = State(new[] { "archer" }, 3);
            for (int seed = 0; seed < 100; seed++) Assert.That(Ids(Generate(state, seed)).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void HeroUpgrade_IsExcludedWhileHeroAbsent()
        {
            DraftSelectionState state = State(new[] { "archer" }, 3);
            Assert.That(CardDraftSelector.IsEligible(Card("bombardier_cluster_shells"), state), Is.False);
        }

        [Test]
        public void HeroUpgrade_BecomesEligibleAfterRecruitment()
        {
            DraftSelectionState state = State(new[] { "archer", "bombardier" }, 2);
            Assert.That(CardDraftSelector.IsEligible(Card("bombardier_cluster_shells"), state), Is.True);
        }

        [Test]
        public void MaxStackCard_IsExcluded()
        {
            CardDefinition card = Card("archer_twin_volley");
            var stacks = new Dictionary<string, int> { [card.id] = card.behaviorUpgrade.maxStacks };
            Assert.That(CardDraftSelector.IsEligible(card, State(new[] { "archer" }, 3, stacks)), Is.False);
        }

        [Test]
        public void RecruitCard_IsExcludedAfterRecruitment()
        {
            Assert.That(CardDraftSelector.IsEligible(Card("recruit_bombardier"), State(new[] { "archer", "bombardier" }, 2)), Is.False);
        }

        [Test]
        public void RecruitCards_AreExcludedWhenSlotsAreFull()
        {
            DraftSelectionState state = State(new[] { "archer" }, 0);
            Assert.That(pool.cards.Where(x => x.card.cardCategory == CardCategory.RecruitHero).All(x => !CardDraftSelector.IsEligible(x.card, state)), Is.True);
        }

        [Test]
        public void RecruitOption_IsGuaranteedWhileAvailable()
        {
            DraftSelectionState state = State(new[] { "archer" }, 3);
            for (int seed = 0; seed < 100; seed++) Assert.That(Generate(state, seed).Any(x => x.Card.cardCategory == CardCategory.RecruitHero), Is.True);
        }

        [Test]
        public void RecruitCards_DisappearAfterAllFourHeroesActive()
        {
            DraftSelectionState state = State(new[] { "archer", "bombardier", "frost_mage", "electric_engineer" }, 2);
            for (int seed = 0; seed < 50; seed++) Assert.That(Generate(state, seed).Any(x => x.Card.cardCategory == CardCategory.RecruitHero), Is.False);
        }

        [Test]
        public void TargetedModifier_RequiresActiveHero()
        {
            CardDefinition frostbite = Card("frostbite");
            Assert.That(CardDraftSelector.IsEligible(frostbite, State(new[] { "archer" }, 3)), Is.False);
            Assert.That(CardDraftSelector.IsEligible(frostbite, State(new[] { "archer", "frost_mage" }, 2)), Is.True);
        }

        [Test]
        public void InvalidPoolData_ReportsErrors()
        {
            CardPoolDefinition invalid = ScriptableObject.CreateInstance<CardPoolDefinition>();
            invalid.expectedCardCount = 18;
            invalid.cards.Add(new CardPoolEntry { card = pool.cards[0].card, weight = 0f });
            Assert.That(GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateCardPool(invalid)), Is.True);
            UnityEngine.Object.DestroyImmediate(invalid);
        }

        [Test]
        public void FallbackState_ReturnsEveryAvailableValidChoiceWithoutDuplicates()
        {
            var entries = pool.cards.Where(x => x.card.id == "war_training" || x.card.id == "battle_rhythm").ToList();
            List<DraftCardChoice> choices = CardDraftSelector.Generate(entries, State(new[] { "archer" }, 0), 3, RecruitOptionPolicy.Weighted, 9);
            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(choices.Select(x => x.Card.id).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void PrototypeAndCuratedAssets_StayOutsideProductionResources()
        {
            Assert.That(AssetDatabase.GetAssetPath(pool), Does.Not.Contain("Resources/Cards"));
            foreach (CardPoolEntry entry in pool.cards.Where(x => x.card.cardCategory == CardCategory.HeroUpgrade))
                Assert.That(AssetDatabase.GetAssetPath(entry.card), Does.Not.Contain("Resources/Cards"));
        }

        [Test]
        public void FiveHundredRunSimulation_HasNoInvalidDraftsAndEveryCardIsReachable()
        {
            const int runs = 500;
            const int draftsPerRun = 10;
            int invalid = 0, duplicates = 0, shortDrafts = 0, recruitFailures = 0, maxViolations = 0;
            var offers = pool.cards.ToDictionary(x => x.card.id, _ => 0, StringComparer.Ordinal);
            var selectable = new HashSet<string>(StringComparer.Ordinal);

            for (int run = 0; run < runs; run++)
            {
                var heroes = new HashSet<string>(StringComparer.Ordinal) { "archer" };
                var stacks = new Dictionary<string, int>(StringComparer.Ordinal);
                int slots = 3;
                for (int draft = 0; draft < draftsPerRun; draft++)
                {
                    List<DraftCardChoice> choices = Generate(State(heroes, slots, stacks), run * 97 + draft);
                    if (choices.Count == 0) invalid++;
                    if (choices.Count < 3) shortDrafts++;
                    if (choices.Select(x => x.Card.id).Distinct().Count() != choices.Count) duplicates++;
                    bool recruitsRemain = slots > 0 && pool.cards.Any(x => x.card.cardCategory == CardCategory.RecruitHero && !heroes.Contains(x.card.recruitHeroId));
                    if (recruitsRemain && !choices.Any(x => x.Card.cardCategory == CardCategory.RecruitHero)) recruitFailures++;
                    foreach (DraftCardChoice choice in choices) { offers[choice.Card.id]++; selectable.Add(choice.Card.id); }

                    DraftCardChoice selected = choices.FirstOrDefault(x => x.Card.cardCategory == CardCategory.RecruitHero);
                    if (selected.Card == null) selected = choices[0];
                    if (selected.Card.cardCategory == CardCategory.RecruitHero)
                    {
                        if (heroes.Add(selected.Card.recruitHeroId)) slots--;
                    }
                    else if (selected.Card.cardCategory == CardCategory.HeroUpgrade)
                    {
                        int current = stacks.TryGetValue(selected.Card.id, out int value) ? value : 0;
                        if (current >= selected.Card.behaviorUpgrade.maxStacks) maxViolations++;
                        else stacks[selected.Card.id] = current + 1;
                    }
                }
            }

            int min = offers.Values.Min(), max = offers.Values.Max();
            double average = offers.Values.Average();
            TestContext.WriteLine($"Runs={runs}; Drafts={runs * draftsPerRun}; Choices={offers.Values.Sum()}; Invalid={invalid}; Duplicates={duplicates}; Short={shortDrafts}; RecruitFailures={recruitFailures}; MaxViolations={maxViolations}; OfferMin={min}; OfferMax={max}; OfferAvg={average:F2}");
            foreach (var offer in offers.OrderBy(x => x.Key)) TestContext.WriteLine($"{offer.Key}={offer.Value}");

            Assert.That(invalid, Is.Zero);
            Assert.That(duplicates, Is.Zero);
            Assert.That(shortDrafts, Is.Zero);
            Assert.That(recruitFailures, Is.Zero);
            Assert.That(maxViolations, Is.Zero);
            Assert.That(offers.Values.All(value => value > 0), Is.True);
            Assert.That(selectable.Count, Is.EqualTo(18));
        }

        private CardDefinition Card(string id) => pool.cards.Single(x => x.card.id == id).card;
        private List<DraftCardChoice> Generate(DraftSelectionState state, int seed) => CardDraftSelector.Generate(pool.cards, state, 3, pool.recruitOptionPolicy, seed);
        private static string[] Ids(IEnumerable<DraftCardChoice> choices) => choices.Select(x => x.Card.id).ToArray();
        private static DraftSelectionState State(IEnumerable<string> heroes, int slots, IDictionary<string, int> stacks = null) =>
            new DraftSelectionState(heroes, new[] { AttackType.SingleTarget, AttackType.Splash, AttackType.Chain }, slots, stacks);
        private static string Format(IEnumerable<GameplayValidationIssue> issues) => string.Join("\n", issues.Select(x => $"{x.Severity}:{x.Code}:{x.Message}"));
    }
}
