using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class CampaignMapPlayModeTests
    {
        private GameObject testRoot;
        private CampaignProgressionManager campaignManager;

        [SetUp]
        public void SetUp()
        {
            CampaignProgressionManager.ResetForTesting();
            testRoot = new GameObject("CampaignTestRoot");
            campaignManager = testRoot.AddComponent<CampaignProgressionManager>();
            campaignManager.ResetCampaignProgress();
        }

        [TearDown]
        public void TearDown()
        {
            if (campaignManager != null)
            {
                campaignManager.ResetCampaignProgress();
            }

            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }

            CampaignProgressionManager.ResetForTesting();
        }

        [UnityTest]
        public IEnumerator CampaignProgressionManager_DefaultNodes_InitializesAll6Biomes()
        {
            Assert.IsNotNull(campaignManager.AllNodes);
            Assert.AreEqual(6, campaignManager.AllNodes.Count);

            var stage1 = campaignManager.GetNode(0);
            Assert.IsNotNull(stage1);
            Assert.AreEqual("Castle Road", stage1.stageName);
            Assert.AreEqual(0, stage1.requiredTotalStarsToUnlock);

            var stage6 = campaignManager.GetNode(5);
            Assert.IsNotNull(stage6);
            Assert.AreEqual("The Void Rift", stage6.stageName);
            Assert.AreEqual(10, stage6.requiredTotalStarsToUnlock);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignProgressionManager_RecordStageResult_Calculates3StarRatingAccurately()
        {
            // Victory only (low HP, low Heat) -> 1 Star
            int stars1 = campaignManager.RecordStageResult(0, true, 0.50f, 0, 1000);
            Assert.AreEqual(1, stars1);
            Assert.AreEqual(1, campaignManager.GetStarsForStage(0));

            // Victory with 80% HP, Heat 0 -> 2 Stars
            int stars2 = campaignManager.RecordStageResult(0, true, 0.80f, 0, 1500);
            Assert.AreEqual(2, stars2);
            Assert.AreEqual(2, campaignManager.GetStarsForStage(0));

            // Victory with 90% HP, Heat 3 -> 3 Stars
            int stars3 = campaignManager.RecordStageResult(0, true, 0.90f, 3, 3000);
            Assert.AreEqual(3, stars3);
            Assert.AreEqual(3, campaignManager.GetStarsForStage(0));
            Assert.AreEqual(3, campaignManager.GetHighestHeatCleared(0));
            Assert.AreEqual(3000, campaignManager.GetBestScore(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignProgressionManager_StarRequirements_UnlocksSubsequentStages()
        {
            Assert.IsTrue(campaignManager.IsStageUnlocked(0)); // Stage 1 (0 req)
            Assert.IsFalse(campaignManager.IsStageUnlocked(1)); // Stage 2 (2 req)

            // Earn 2 stars on Stage 1
            campaignManager.RecordStageResult(0, true, 0.75f, 0, 1200);
            Assert.AreEqual(2, campaignManager.GetTotalStarsEarned());
            Assert.IsTrue(campaignManager.IsStageUnlocked(1)); // Stage 2 unlocked!

            // Stage 3 requires 4 stars
            Assert.IsFalse(campaignManager.IsStageUnlocked(2));
            campaignManager.RecordStageResult(1, true, 0.75f, 0, 1500);
            Assert.AreEqual(4, campaignManager.GetTotalStarsEarned());
            Assert.IsTrue(campaignManager.IsStageUnlocked(2)); // Stage 3 unlocked!
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignProgressionManager_ResetCampaignProgress_ClearsStarsAndHeatRecords()
        {
            campaignManager.RecordStageResult(0, true, 0.95f, 4, 5000);
            Assert.AreEqual(3, campaignManager.GetStarsForStage(0));
            Assert.AreEqual(4, campaignManager.GetHighestHeatCleared(0));

            campaignManager.ResetCampaignProgress();
            Assert.AreEqual(0, campaignManager.GetStarsForStage(0));
            Assert.AreEqual(0, campaignManager.GetHighestHeatCleared(0));
            Assert.AreEqual(0, campaignManager.GetTotalStarsEarned());
            yield return null;
        }
    }
}
