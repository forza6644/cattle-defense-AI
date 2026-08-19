using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    public class DifficultyModePlayModeTests
    {
        private GameObject root;
        private AscensionManager manager;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(DifficultyRuleset.PrefsSelectedMode);
            PlayerPrefs.SetInt("campaign_stars_stage_0", 1);
            PlayerPrefs.SetInt("stats_stage_1_completed", 1);
            AscensionManager.ResetForTesting();
            root = new GameObject("DifficultyModeRoot");
            manager = root.AddComponent<AscensionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            PlayerPrefs.DeleteKey(DifficultyRuleset.PrefsSelectedMode);
            AscensionManager.ResetForTesting();
        }

        [UnityTest]
        public IEnumerator NormalDifficulty_HasNoChallengeMutators()
        {
            manager.ApplyDifficulty(DifficultyMode.Normal);
            yield return null;
            Assert.That(manager.CurrentDifficulty, Is.EqualTo(DifficultyMode.Normal));
            Assert.That(manager.IsHardActive(), Is.False);
            Assert.That(manager.IsMutatorActive("fast_enemies"), Is.False);
            Assert.That(manager.GetEnemySpeedMultiplier(), Is.EqualTo(1f).Within(0.001f));
            Assert.That(manager.GetGoldMultiplier(), Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HardDifficulty_ActivatesCuratedMutatorsAndGoldBonus()
        {
            manager.LoadAllMutators();
            manager.ApplyDifficulty(DifficultyMode.Hard);
            yield return null;
            Assert.That(manager.CurrentDifficulty, Is.EqualTo(DifficultyMode.Hard));
            Assert.That(manager.IsHardActive(), Is.True);
            Assert.That(manager.IsMutatorActive("fast_enemies"), Is.True);
            Assert.That(manager.IsMutatorActive("armored_horde"), Is.True);
            Assert.That(manager.IsMutatorActive("empowered_elites"), Is.True);
            Assert.That(manager.IsMutatorActive("hyper_waves"), Is.True);
            Assert.That(manager.IsMutatorActive("costly_economy"), Is.False);
            Assert.That(manager.GetEnemySpeedMultiplier(), Is.EqualTo(1.20f).Within(0.001f));
            Assert.That(manager.GetEliteHealthMultiplier(), Is.EqualTo(1.50f).Within(0.001f));
            Assert.That(manager.GetGoldMultiplier(), Is.EqualTo(1.50f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SwitchingToNormal_ClearsHardModifiers()
        {
            manager.LoadAllMutators();
            manager.ApplyDifficulty(DifficultyMode.Hard);
            manager.ApplyDifficulty(DifficultyMode.Normal);
            yield return null;
            Assert.That(manager.IsHardActive(), Is.False);
            Assert.That(manager.IsMutatorActive("fast_enemies"), Is.False);
            Assert.That(manager.GetEnemySpeedMultiplier(), Is.EqualTo(1f).Within(0.001f));
            Assert.That(manager.GetGoldMultiplier(), Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HardRewards_ApplyThroughEconomy()
        {
            manager.LoadAllMutators();
            manager.ApplyDifficulty(DifficultyMode.Hard);
            GameObject economyObject = new GameObject("DifficultyEconomy");
            EconomyManager economy = economyObject.AddComponent<EconomyManager>();
            economy.AddGold(100);
            Assert.That(economy.Gold, Is.EqualTo(150));
            Object.DestroyImmediate(economyObject);
            yield return null;
        }
    }
}
