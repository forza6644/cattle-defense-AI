using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class SaveManagerProgressionPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveManager.ResetAll();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SaveManager.LoadProgress();
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.ResetAll();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SaveManager.LoadProgress();
        }

        [UnityTest]
        public IEnumerator CompleteStage1_TriggersStage2Unlock_AndSetsStage1Completed()
        {
            Assert.That(SaveManager.HighestStageUnlocked, Is.EqualTo(1), "Initial unlocked stage must be 1.");
            Assert.That(SaveManager.Stage1Completed, Is.False, "Initial Stage1Completed must be false.");

            SaveManager.CompleteStage1();

            yield return null;

            Assert.That(SaveManager.Stage1Completed, Is.True, "Stage 1 must be marked as completed after CompleteStage1().");
            Assert.That(SaveManager.HighestStageUnlocked, Is.GreaterThanOrEqualTo(2), "Completing Stage 1 must unlock Stage 2.");
        }

        [UnityTest]
        public IEnumerator UnlockStage_ByStringId_UnlocksStage2Highlands()
        {
            Assert.That(SaveManager.HighestStageUnlocked, Is.EqualTo(1));

            SaveManager.UnlockStage("stage_2_highlands");

            yield return null;

            Assert.That(SaveManager.HighestStageUnlocked, Is.GreaterThanOrEqualTo(2), "Unlocking 'stage_2_highlands' by string ID must unlock Stage 2.");
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_PersistsPlayerCurrencyAndRewards_Accurately()
        {
            SaveManager.AddMetaGold(1500);
            SaveManager.AddRewards(500, 250, 100);
            SaveManager.UpdateBestWave(10);
            SaveManager.RecordWin();

            SaveManager.SaveProgress();

            yield return null;

            // Clear in-memory values and reload from PlayerPrefs
            SaveManager.LoadProgress();

            Assert.That(SaveManager.MetaGold, Is.EqualTo(2000), "MetaGold must persist across save/load.");
            Assert.That(SaveManager.AccountXp, Is.EqualTo(250), "AccountXP must persist across save/load.");
            Assert.That(SaveManager.CoreMaterials, Is.EqualTo(100), "CoreMaterials must persist across save/load.");
            Assert.That(SaveManager.BestWave, Is.EqualTo(10), "BestWave must persist across save/load.");
            Assert.That(SaveManager.TotalWins, Is.EqualTo(1), "TotalWins must persist across save/load.");
        }

        [UnityTest]
        public IEnumerator ResetProgress_ResetsToStage1Only_WithCleanDefaults()
        {
            SaveManager.CompleteStage(0);
            SaveManager.AddMetaGold(5000);
            SaveManager.SaveProgress();

            yield return null;

            SaveManager.ResetAll();
            SaveManager.LoadProgress();

            Assert.That(SaveManager.HighestStageUnlocked, Is.EqualTo(1), "Resetting progress must revert HighestStageUnlocked to Stage 1.");
            Assert.That(SaveManager.Stage1Completed, Is.False, "Resetting progress must set Stage1Completed to false.");
            Assert.That(SaveManager.MetaGold, Is.EqualTo(0), "Resetting progress must reset MetaGold to 0.");
            Assert.That(SaveManager.SelectedStartingDefenderId, Is.EqualTo("archer"), "Default starting defender must be archer.");
            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo("crystal_lightning"), "Default starter crystal must be crystal_lightning.");
        }

        [UnityTest]
        public IEnumerator TryClaimRunRewards_EnforcesSingleClaimPerSession()
        {
            SaveManager.BeginRunRewardSession();

            bool claimedFirst = SaveManager.TryClaimRunRewards(10, out int gold1, out int xp1, out int mat1);
            Assert.That(claimedFirst, Is.True, "First reward claim in session must succeed.");
            Assert.That(gold1, Is.EqualTo(500), "Gold reward for wave 10 must be 500.");

            bool claimedSecond = SaveManager.TryClaimRunRewards(10, out int gold2, out int xp2, out int mat2);
            Assert.That(claimedSecond, Is.False, "Second reward claim in same session must fail.");

            yield return null;
        }
    }
}
