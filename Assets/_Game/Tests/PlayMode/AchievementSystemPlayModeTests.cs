using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class AchievementSystemPlayModeTests
    {
        private GameObject testRoot;
        private AchievementManager achievementManager;

        [SetUp]
        public void SetUp()
        {
            AchievementManager.ResetForTesting();
            testRoot = new GameObject("AchievementTestRoot");
            achievementManager = testRoot.AddComponent<AchievementManager>();
            achievementManager.ResetAllAchievements();
        }

        [TearDown]
        public void TearDown()
        {
            if (achievementManager != null)
            {
                achievementManager.ResetAllAchievements();
            }

            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }

            AchievementManager.ResetForTesting();
        }

        [UnityTest]
        public IEnumerator AchievementManager_DefaultAchievements_LoadsAndMapsCategories()
        {
            Assert.IsNotNull(achievementManager.AllAchievements);
            Assert.GreaterOrEqual(achievementManager.AllAchievements.Count, 10);

            var firstBlood = achievementManager.GetAchievement("achv_first_blood");
            Assert.IsNotNull(firstBlood);
            Assert.AreEqual("First Blood", firstBlood.title);
            Assert.AreEqual(AchievementCategory.Combat, firstBlood.category);
            Assert.AreEqual(1f, firstBlood.targetValue);

            var maestro = achievementManager.GetAchievement("achv_synergy_50");
            Assert.IsNotNull(maestro);
            Assert.AreEqual(AchievementCategory.Synergies, maestro.category);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AchievementManager_AddProgress_UnlocksAndFiresEvent()
        {
            bool eventFired = false;
            AchievementDefinition unlockedAchv = null;
            AchievementManager.OnAchievementUnlocked += achv =>
            {
                eventFired = true;
                unlockedAchv = achv;
            };

            Assert.IsFalse(achievementManager.IsUnlocked("achv_first_blood"));
            achievementManager.AddProgress("achv_first_blood", 1f);

            Assert.IsTrue(achievementManager.IsUnlocked("achv_first_blood"));
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(unlockedAchv);
            Assert.AreEqual("achv_first_blood", unlockedAchv.id);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AchievementManager_ClaimReward_GrantsCurrenciesAndPersists()
        {
            achievementManager.AddProgress("achv_first_blood", 1f);
            Assert.IsTrue(achievementManager.IsUnlocked("achv_first_blood"));
            Assert.IsFalse(achievementManager.IsClaimed("achv_first_blood"));

            bool claimed = achievementManager.ClaimReward("achv_first_blood", out int gold, out int mats);
            Assert.IsTrue(claimed);
            Assert.Greater(gold, 0);
            Assert.IsTrue(achievementManager.IsClaimed("achv_first_blood"));

            // Second claim attempt should fail
            bool claimedAgain = achievementManager.ClaimReward("achv_first_blood", out _, out _);
            Assert.IsFalse(claimedAgain);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AchievementManager_Reset_ClearsProgressAndUnlockedState()
        {
            achievementManager.AddProgress("achv_slayer_100", 50f);
            Assert.AreEqual(50f, achievementManager.GetProgress("achv_slayer_100"));

            achievementManager.ResetAllAchievements();
            Assert.AreEqual(0f, achievementManager.GetProgress("achv_slayer_100"));
            Assert.IsFalse(achievementManager.IsUnlocked("achv_slayer_100"));
            yield return null;
        }
    }
}
