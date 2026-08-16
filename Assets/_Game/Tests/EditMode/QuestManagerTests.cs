using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class QuestManagerTests
    {
        private GameObject go;
        private QuestManager manager;

        [SetUp]
        public void SetUp()
        {
            QuestManager.ResetForTesting();
            SaveManager.ResetProgress();

            go = new GameObject("TestQuestManager");
            manager = go.AddComponent<QuestManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
            QuestManager.ResetForTesting();
        }

        [Test]
        public void Quests_GenerateDailyAndWeeklyQuests()
        {
            Assert.IsNotNull(manager.ActiveQuests);
            Assert.GreaterOrEqual(manager.ActiveQuests.Count, 3, "Should generate daily and weekly quests on startup.");
        }

        [Test]
        public void IncrementObjective_UpdatesProgressAndCompletes()
        {
            var quest = manager.ActiveQuests[0];
            int initialAmount = quest.currentAmount;
            int target = quest.targetAmount;

            manager.IncrementObjective(quest.objectiveType, target);

            Assert.IsTrue(quest.IsCompleted, "Quest should be completed after reaching target.");
            Assert.AreEqual(target, quest.currentAmount);
        }

        [Test]
        public void ClaimReward_AwardsGoldAndMaterials()
        {
            var quest = manager.ActiveQuests[0];
            manager.IncrementObjective(quest.objectiveType, quest.targetAmount);

            int initialGold = SaveManager.MetaGold;
            bool claimed = manager.ClaimQuestReward(quest.questId, out int goldEarned, out int matsEarned);

            Assert.IsTrue(claimed, "Should successfully claim reward.");
            Assert.Greater(goldEarned, 0);
            Assert.AreEqual(initialGold + goldEarned, SaveManager.MetaGold);
            Assert.IsTrue(quest.isClaimed);

            bool claimAgain = manager.ClaimQuestReward(quest.questId, out _, out _);
            Assert.IsFalse(claimAgain, "Cannot claim reward twice.");
        }
    }
}
