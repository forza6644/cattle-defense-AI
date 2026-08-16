using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class IdleTreasuryManagerTests
    {
        private GameObject managerGo;
        private IdleTreasuryManager treasury;

        [SetUp]
        public void SetUp()
        {
            IdleTreasuryManager.ResetForTesting();
            SaveManager.ResetProgress();

            managerGo = new GameObject("TestIdleTreasuryManager");
            treasury = managerGo.AddComponent<IdleTreasuryManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (managerGo != null)
            {
                Object.DestroyImmediate(managerGo);
            }
            IdleTreasuryManager.ResetForTesting();
        }

        [Test]
        public void InitialTreasury_HasValidTimestamp()
        {
            long timestamp = treasury.GetLastTreasuryTimestamp();
            Assert.Greater(timestamp, 0);
        }

        [Test]
        public void CapacityLimits_DoNotExceed8Hours()
        {
            float maxSeconds = IdleTreasuryManager.MaxCapacitySeconds;
            Assert.AreEqual(8f * 3600f, maxSeconds);
        }

        [Test]
        public void DailyStreak_ReturnsValidDailyRewards()
        {
            for (int i = 0; i < 7; i++)
            {
                DailyRewardInfo reward = treasury.GetRewardForDay(i);
                Assert.AreEqual(i + 1, reward.dayNumber);
                Assert.Greater(reward.gold, 0, $"Day {i + 1} should have positive gold reward.");
            }
        }

        [Test]
        public void ClaimDailyReward_AdvancesStreakAndGrantsGold()
        {
            int initialGold = SaveManager.MetaGold;
            bool claimed = treasury.ClaimDailyReward(out DailyRewardInfo reward);

            Assert.IsTrue(claimed, "First daily claim should succeed.");
            Assert.AreEqual(initialGold + reward.gold, SaveManager.MetaGold, "Gold should be awarded to SaveManager.");

            bool claimAgain = treasury.ClaimDailyReward(out _);
            Assert.IsFalse(claimAgain, "Should not allow double claiming on the same day.");
        }
    }
}
