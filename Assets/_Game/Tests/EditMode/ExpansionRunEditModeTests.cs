using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public class ExpansionRunEditModeTests
    {
        private const string StagePath = "Assets/_Game/ScriptableObjects/ExpansionRunQualification/StoneholdExpansionTrial.asset";
        private StageData stage;
        private CardPoolDefinition pool;

        [SetUp]
        public void SetUp()
        {
            stage = AssetDatabase.LoadAssetAtPath<StageData>(StagePath);
            Assert.That(stage, Is.Not.Null);
            pool = stage.cardPoolOverride;
            Assert.That(pool, Is.Not.Null);
        }

        [Test] public void Stage_HasStableId() => Assert.That(stage.stageId, Is.EqualTo(ExpansionRunValidation.StageId));
        [Test] public void Stage_HasExactlyTenWaves() => Assert.That(stage.waves, Has.Length.EqualTo(10));
        [Test] public void Stage_UsesExactWaveCounts() => Assert.That(stage.useExactWaveCounts, Is.True);
        [Test] public void Stage_StartsWithArcher() => Assert.That(stage.startingHeroId, Is.EqualTo("archer"));
        [Test] public void Stage_HasAnchorFixture() => Assert.That(stage.battlefieldFixturePrefab, Is.Not.Null);
        [Test] public void Stage_ContainsSevenEnemyTypes() => Assert.That(stage.expectedEnemyTypes.Select(x => x.stableId).Distinct().Count(), Is.EqualTo(7));

        [TestCase(1, 12, "Foundation")]
        [TestCase(2, 20, "Speed Pressure")]
        [TestCase(3, 18, "Durable Frontline")]
        [TestCase(4, 20, "Ranged Introduction")]
        [TestCase(5, 26, "Mixed Midpoint")]
        [TestCase(6, 19, "Elite Introduction")]
        [TestCase(7, 39, "Combined Counterplay")]
        [TestCase(8, 36, "Heavy Pressure")]
        [TestCase(9, 70, "Peak Encounter")]
        [TestCase(10, 22, "Warlord Finale")]
        public void Wave_InventoryIsExact(int number, int count, string label)
        {
            WaveData wave = stage.waves[number - 1];
            Assert.That(ExpansionRunValidation.TotalEnemies(wave), Is.EqualTo(count));
            Assert.That(wave.waveLabel, Is.EqualTo(label));
        }

        [Test] public void Boss_AppearsExactlyOnceInFinalWave()
        {
            int early = stage.waves.Take(9).Sum(CountBosses);
            Assert.That(early, Is.Zero);
            Assert.That(CountBosses(stage.waves[9]), Is.EqualTo(1));
        }

        [Test] public void Shaman_CountNeverExceedsTwo() =>
            Assert.That(stage.waves.All(w => Count(w, "elite_war_shaman") <= 2), Is.True);

        [Test] public void Raider_FirstAppearsInWaveFour() =>
            Assert.That(Array.FindIndex(stage.waves, w => Count(w, "crossbow_raider") > 0), Is.EqualTo(3));

        [Test] public void Shaman_FirstAppearsInWaveSix() =>
            Assert.That(Array.FindIndex(stage.waves, w => Count(w, "elite_war_shaman") > 0), Is.EqualTo(5));

        [Test] public void WaveNine_HasSeventyEnemiesAndTwoShamans()
        {
            Assert.That(ExpansionRunValidation.TotalEnemies(stage.waves[8]), Is.EqualTo(70));
            Assert.That(Count(stage.waves[8], "elite_war_shaman"), Is.EqualTo(2));
        }

        [Test] public void EverySpawnEntry_HasFiniteTiming()
        {
            foreach (WaveData.SpawnEntry entry in stage.waves.SelectMany(w => w.spawns))
            {
                Assert.That(float.IsNaN(entry.spawnInterval) || float.IsInfinity(entry.spawnInterval), Is.False);
                Assert.That(float.IsNaN(entry.startDelay) || float.IsInfinity(entry.startDelay), Is.False);
                Assert.That(entry.spawnInterval, Is.GreaterThan(0f));
                Assert.That(entry.startDelay, Is.GreaterThanOrEqualTo(0f));
            }
        }

        [Test] public void ExpansionRun20_HasExactlyTwentyUniqueCards()
        {
            Assert.That(pool.stableId, Is.EqualTo(ExpansionRunValidation.PoolId));
            Assert.That(pool.cards, Has.Count.EqualTo(20));
            Assert.That(pool.cards.Select(x => x.card.id).Distinct().Count(), Is.EqualTo(20));
        }

        [TestCase(CardCategory.RecruitHero, 3)]
        [TestCase(CardCategory.HeroUpgrade, 8)]
        [TestCase(CardCategory.Modifier, 6)]
        [TestCase(CardCategory.Trap, 2)]
        [TestCase(CardCategory.BattlefieldDefense, 1)]
        public void Pool_CategoryInventoryIsExact(CardCategory category, int expected) =>
            Assert.That(pool.cards.Count(x => x.card.cardCategory == category), Is.EqualTo(expected));

        [Test] public void Pool_ContainsAllRequiredRecruits()
        {
            CollectionAssert.AreEquivalent(new[] { "bombardier", "frost_mage", "electric_engineer" },
                pool.cards.Where(x => x.card.cardCategory == CardCategory.RecruitHero).Select(x => x.card.recruitHeroId));
        }

        [Test] public void Pool_ContainsAllEightBehaviorUpgrades()
        {
            string[] expected =
            {
                "archer_twin_volley", "archer_piercing_arrows", "bombardier_cluster_shells", "bombardier_wide_blast",
                "frost_mage_shard_volley", "frost_mage_echoing_nova", "electric_engineer_extended_circuit", "electric_engineer_forked_current"
            };
            CollectionAssert.IsSubsetOf(expected, pool.cards.Select(x => x.card.id).ToArray());
        }

        [Test] public void Pool_ContainsAllBattlefieldCards()
        {
            CollectionAssert.AreEquivalent(new[] { "deploy_caltrops", "deploy_burning_oil", "deploy_wooden_barricade" },
                pool.cards.Where(x => x.card.cardCategory == CardCategory.Trap || x.card.cardCategory == CardCategory.BattlefieldDefense).Select(x => x.card.id));
        }

        [Test] public void Pool_ExcludesWatchtowerExpansion() =>
            Assert.That(pool.cards.Any(x => x.card.id == "watchtower_expansion"), Is.False);

        [Test] public void ProductionPool_RemainsExactlyThirtyNine() =>
            Assert.That(Resources.LoadAll<CardDefinition>("Cards"), Has.Length.EqualTo(39));

        [Test] public void VerticalSlice18_RemainsExactlyEighteen()
        {
            CardPoolDefinition vertical = AssetDatabase.LoadAssetAtPath<CardPoolDefinition>("Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset");
            Assert.That(vertical.cards, Has.Count.EqualTo(18));
        }

        [Test] public void ExpansionAssets_RemainOutsideProductionResources()
        {
            Assert.That(AssetDatabase.GetAssetPath(stage), Does.Not.Contain("/Resources/"));
            Assert.That(AssetDatabase.GetAssetPath(pool), Does.Not.Contain("/Resources/"));
            foreach (WaveData wave in stage.waves) Assert.That(AssetDatabase.GetAssetPath(wave), Does.Not.Contain("/Resources/"));
        }

        [Test] public void AnchorFixture_HasTwoTrapAndOneDefenseAnchor()
        {
            BattlefieldAnchor[] anchors = stage.battlefieldFixturePrefab.GetComponentsInChildren<BattlefieldAnchor>(true);
            Assert.That(anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Trap), Is.EqualTo(2));
            Assert.That(anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Defense), Is.EqualTo(1));
        }

        [Test] public void ExpansionValidator_AcceptsStage()
        {
            List<GameplayValidationIssue> issues = ExpansionRunValidation.Validate(stage);
            Assert.That(issues.Any(x => x.Severity == ValidationSeverity.Error), Is.False, Format(issues));
        }

        [Test] public void FixedSeed_DraftIsDeterministic()
        {
            DraftSelectionState state = State(true, true);
            CollectionAssert.AreEqual(Ids(Draft(state, 42)), Ids(Draft(state, 42)));
        }

        [Test] public void Draft_HasNoDuplicatesAcrossOneHundredSeeds()
        {
            DraftSelectionState state = State(true, true);
            for (int seed = 0; seed < 100; seed++)
            {
                string[] ids = Ids(Draft(state, seed));
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            }
        }

        [Test] public void RecruitOption_IsGuaranteedWhileAvailable()
        {
            DraftSelectionState state = State(true, true);
            for (int seed = 0; seed < 100; seed++)
                Assert.That(Draft(state, seed).Any(x => x.Card.cardCategory == CardCategory.RecruitHero), Is.True);
        }

        [Test] public void BattlefieldCards_AreFilteredWhenAnchorsUnavailable()
        {
            DraftSelectionState state = State(false, false);
            Assert.That(pool.cards.Where(x => x.card.cardCategory == CardCategory.Trap || x.card.cardCategory == CardCategory.BattlefieldDefense)
                .All(x => !CardDraftSelector.IsEligible(x.card, state)), Is.True);
        }

        [Test] public void OneHundredSeedSimulation_MeetsQualificationWindow()
        {
            ExpansionBalanceReport report = ExpansionRunBalanceSimulator.Run(stage, 100);
            TestContext.WriteLine($"Runs={report.Runs}; Wins={report.Wins}; WinRate={report.WinRate:P0}; Invalid={report.InvalidDrafts}; SoftLocks={report.SoftLocks}; MedianWave={report.MedianWave}; AvgWave={report.AverageWave:F2}; AvgDuration={report.AverageDuration:F1}");
            Assert.That(report.WinRate, Is.InRange(0.45d, 0.75d));
            Assert.That(report.InvalidDrafts, Is.Zero);
            Assert.That(report.SoftLocks, Is.Zero);
            Assert.That(report.Offers.Values.All(x => x > 0), Is.True);
            CollectionAssert.IsSubsetOf(new[] { "bombardier", "frost_mage", "electric_engineer" }, report.Recruits.Keys);
            Assert.That(report.AverageDuration, Is.InRange(360d, 600d));
        }

        private List<DraftCardChoice> Draft(DraftSelectionState state, int seed) =>
            CardDraftSelector.Generate(pool.cards, state, 3, pool.recruitOptionPolicy, seed);

        private static DraftSelectionState State(bool trap, bool defense) =>
            new DraftSelectionState(new[] { "archer" }, new[] { AttackType.SingleTarget }, 3, new Dictionary<string, int>(), trap, defense);

        private static string[] Ids(IEnumerable<DraftCardChoice> choices) => choices.Select(x => x.Card.id).ToArray();
        private static int Count(WaveData wave, string id) => wave.spawns.Where(x => x.enemy != null && x.enemy.stableId == id).Sum(x => x.count);
        private static int CountBosses(WaveData wave) => wave.spawns.Where(x => x.enemy != null && x.enemy.classification == EnemyClassification.Boss).Sum(x => x.count);
        private static string Format(IEnumerable<GameplayValidationIssue> issues) => string.Join("\n", issues.Select(x => $"{x.Severity}:{x.Code}:{x.Message}"));
    }
}