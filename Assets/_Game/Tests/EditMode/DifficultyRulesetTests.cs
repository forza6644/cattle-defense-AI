using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    public class DifficultyRulesetTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(DifficultyRuleset.PrefsSelectedMode);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(DifficultyRuleset.PrefsSelectedMode);
        }

        [Test]
        public void ClampSelectable_HidesNightmare()
        {
            Assert.That(DifficultyRuleset.ClampSelectable(DifficultyMode.Nightmare), Is.EqualTo(DifficultyMode.Hard));
            Assert.That(DifficultyRuleset.ClampSelectable(DifficultyMode.Normal), Is.EqualTo(DifficultyMode.Normal));
        }

        [Test]
        public void HardRuleset_UsesCuratedExistingMutators()
        {
            Assert.That(DifficultyRuleset.HardMutatorIds.Length, Is.EqualTo(4));
            Assert.That(DifficultyRuleset.ContainsHardMutator("fast_enemies"), Is.True);
            Assert.That(DifficultyRuleset.ContainsHardMutator("armored_horde"), Is.True);
            Assert.That(DifficultyRuleset.ContainsHardMutator("empowered_elites"), Is.True);
            Assert.That(DifficultyRuleset.ContainsHardMutator("hyper_waves"), Is.True);
            Assert.That(DifficultyRuleset.ContainsHardMutator("costly_economy"), Is.False);
            Assert.That(DifficultyRuleset.ContainsHardMutator("regenerating_monsters"), Is.False);
            Assert.That(DifficultyRuleset.ContainsHardMutator("brittle_castle"), Is.False);
            Assert.That(DifficultyRuleset.ContainsHardMutator("nullification_rifts"), Is.False);
        }

        [Test]
        public void HardGoldReward_IsFiftyPercent()
        {
            Assert.That(DifficultyRuleset.HardGoldRewardMultiplier, Is.EqualTo(1.50f).Within(0.001f));
        }

        [Test]
        public void SelectedMode_DefaultsToNormal()
        {
            Assert.That(DifficultyRuleset.GetSelectedMode(), Is.EqualTo(DifficultyMode.Normal));
        }

        [Test]
        public void HardUnlock_UsesStage1Clear()
        {
            PlayerPrefs.DeleteKey("stats_stage_1_completed");
            PlayerPrefs.DeleteKey("campaign_stars_stage_0");
            PlayerPrefs.SetInt("stats_stage_1_completed", 1);
            PlayerPrefs.Save();
            Assert.That(DifficultyRuleset.IsHardUnlocked(), Is.True);
            PlayerPrefs.DeleteKey("stats_stage_1_completed");
        }
    }
}
