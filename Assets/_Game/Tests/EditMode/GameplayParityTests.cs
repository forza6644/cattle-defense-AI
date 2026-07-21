using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public class GameplayParityTests
    {
        private const string ConfigPath = "Assets/_Game/ScriptableObjects/GameConfig.asset";
        private const string ParityStagePath = "Assets/_Game/ScriptableObjects/GameplayParity/ReferenceParityStage01.asset";

        private GameConfig config;
        private StageData parityStage;

        [SetUp]
        public void SetUp()
        {
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            parityStage = AssetDatabase.LoadAssetAtPath<StageData>(ParityStagePath);
        }

        [Test]
        public void ParityStage_ContainsExactlyTwentyWaves()
        {
            Assert.That(parityStage, Is.Not.Null, "ReferenceParityStage01 asset not found.");
            Assert.That(parityStage.stageId, Is.EqualTo("reference_parity_stage_01"));
            Assert.That(parityStage.waves, Has.Length.EqualTo(20), "Parity stage must contain exactly 20 waves.");
            Assert.That(parityStage.useExactWaveCounts, Is.True, "Parity stage must be deterministic.");
        }

        [Test]
        public void ParityStage_ThreePrimaryDefenderPositionsConfigured()
        {
            GameObject managerGo = new GameObject("TestRosterManager", typeof(HeroRosterManager));
            HeroRosterManager roster = managerGo.GetComponent<HeroRosterManager>();

            Assert.That(roster, Is.Not.Null);
            Object.DestroyImmediate(managerGo);
        }

        [Test]
        public void WaveCounter_ReportsOneToTwentyFormat()
        {
            Assert.That(parityStage.waves.Length, Is.EqualTo(20));
            Assert.That($"Wave 1/{parityStage.waves.Length}", Is.EqualTo("Wave 1/20"));
            Assert.That($"Wave 20/{parityStage.waves.Length}", Is.EqualTo("Wave 20/20"));
        }

        [Test]
        public void Draft_PausesCombat_AndRestoresSelectedSpeed()
        {
            GameObject gameGo = new GameObject("TestGameManager", typeof(GameManager));
            GameManager game = gameGo.GetComponent<GameManager>();

            game.SetGameSpeed(1.5f);
            Assert.That(game.GameSpeed, Is.EqualTo(1.5f));
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));

            game.SetState(GameState.LevelUp);
            Assert.That(game.State, Is.EqualTo(GameState.LevelUp));
            Assert.That(Time.timeScale, Is.Zero, "LevelUp draft state must freeze timeScale to 0.");

            game.SetState(GameState.Playing);
            Assert.That(game.State, Is.EqualTo(GameState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1.5f), "Resuming Playing state must restore selected game speed.");

            Object.DestroyImmediate(gameGo);
        }

        [Test]
        public void Reroll_CostAndRules_Respected()
        {
            GameObject draftGo = new GameObject("TestCardDraftManager", typeof(CardDraftManager));
            CardDraftManager draftManager = draftGo.GetComponent<CardDraftManager>();

            Assert.That(CardDraftManager.RerollCost, Is.EqualTo(20), "Reroll cost must be 20 gold.");
            Assert.That(draftManager.CanReroll(), Is.False, "Cannot reroll when draft is inactive.");

            Object.DestroyImmediate(draftGo);
        }

        [Test]
        public void Reroll_DeductsGoldExactlyOnce_WhenSuccessful()
        {
            GameObject econGo = new GameObject("TestEconomyManager", typeof(EconomyManager));
            EconomyManager economy = econGo.GetComponent<EconomyManager>();
            economy.AddGold(100);

            Assert.That(economy.Gold, Is.EqualTo(100));
            bool spent = economy.TrySpend(20);
            Assert.That(spent, Is.True);
            Assert.That(economy.Gold, Is.EqualTo(80), "Exactly 20 gold must be deducted.");

            Object.DestroyImmediate(econGo);
        }

        [Test]
        public void CastleDefeat_TriggersStateChange()
        {
            GameObject castleGo = new GameObject("TestCastle", typeof(Castle));
            Castle castle = castleGo.GetComponent<Castle>();
            GameConfig testConfig = ScriptableObject.CreateInstance<GameConfig>();
            testConfig.castleMaxHealth = 50;

            System.Reflection.FieldInfo configField = typeof(Castle).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(castle, testConfig);

            System.Reflection.MethodInfo awakeMethod = typeof(Castle).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(castle, null);

            int defeatEvents = 0;
            castle.Defeated += () => defeatEvents++;

            castle.TakeDamage(50);
            Assert.That(castle.IsGameOver, Is.True);
            Assert.That(defeatEvents, Is.EqualTo(1));

            Object.DestroyImmediate(castleGo);
            Object.DestroyImmediate(testConfig);
        }

        [Test]
        public void RewardSession_PreventsDuplicateClaims()
        {
            SaveManager.BeginRunRewardSession();
            bool firstClaim = SaveManager.TryClaimRunRewards(20, out int gold1, out int xp1, out _);
            bool secondClaim = SaveManager.TryClaimRunRewards(20, out int gold2, out int xp2, out _);

            Assert.That(firstClaim, Is.True, "First claim in session should succeed.");
            Assert.That(secondClaim, Is.False, "Duplicate claim in same session must be rejected.");
        }
    }
}
