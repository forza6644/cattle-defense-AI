using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class SaveManagerMigrationTests
    {
        private struct SaveState
        {
            public int bestWave;
            public int totalWins;
            public int totalLosses;
            public int totalRuns;
            public int selectedStage;
            public int highestStageUnlocked;
            public int stage1Completed;
            public string selectedStartingDefender;
            public int metaGold;
            public int accountXp;
            public int coreMaterials;
        }

        private SaveState originalState;

        [SetUp]
        public void SetUp()
        {
            // Backup current PlayerPrefs state so we don't destroy actual user/dev data permanently
            originalState = new SaveState
            {
                bestWave = PlayerPrefs.GetInt("stats_best_wave", 0),
                totalWins = PlayerPrefs.GetInt("stats_total_wins", 0),
                totalLosses = PlayerPrefs.GetInt("stats_total_losses", 0),
                totalRuns = PlayerPrefs.GetInt("stats_total_runs", 0),
                selectedStage = PlayerPrefs.GetInt("lobby_selected_stage", 0),
                highestStageUnlocked = PlayerPrefs.GetInt("stats_highest_stage_unlocked", 1),
                stage1Completed = PlayerPrefs.GetInt("stats_stage_1_completed", 0),
                selectedStartingDefender = PlayerPrefs.GetString("lobby_selected_starting_defender", "archer"),
                metaGold = PlayerPrefs.GetInt("stats_meta_gold", 0),
                accountXp = PlayerPrefs.GetInt("stats_account_xp", 0),
                coreMaterials = PlayerPrefs.GetInt("stats_core_materials", 0)
            };
            PlayerPrefs.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original state
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("stats_best_wave", originalState.bestWave);
            PlayerPrefs.SetInt("stats_total_wins", originalState.totalWins);
            PlayerPrefs.SetInt("stats_total_losses", originalState.totalLosses);
            PlayerPrefs.SetInt("stats_total_runs", originalState.totalRuns);
            PlayerPrefs.SetInt("lobby_selected_stage", originalState.selectedStage);
            PlayerPrefs.SetInt("stats_highest_stage_unlocked", originalState.highestStageUnlocked);
            PlayerPrefs.SetInt("stats_stage_1_completed", originalState.stage1Completed);
            PlayerPrefs.SetString("lobby_selected_starting_defender", originalState.selectedStartingDefender);
            PlayerPrefs.SetInt("stats_meta_gold", originalState.metaGold);
            PlayerPrefs.SetInt("stats_account_xp", originalState.accountXp);
            PlayerPrefs.SetInt("stats_core_materials", originalState.coreMaterials);
            PlayerPrefs.Save();
        }

        [Test]
        public void SaveManager_EmptySave_LoadsDefaults()
        {
            PlayerPrefs.DeleteAll();
            SaveManager.LoadProgress();

            Assert.AreEqual(0, SaveManager.BestWave);
            Assert.AreEqual(0, SaveManager.TotalWins);
            Assert.AreEqual(0, SaveManager.TotalLosses);
            Assert.AreEqual(0, SaveManager.TotalRuns);
            Assert.AreEqual(0, SaveManager.SelectedStageIndex);
            Assert.AreEqual(1, SaveManager.HighestStageUnlocked);
            Assert.IsFalse(SaveManager.Stage1Completed);
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId);
            Assert.AreEqual(0, SaveManager.MetaGold);
            Assert.AreEqual(0, SaveManager.AccountXp);
            Assert.AreEqual(0, SaveManager.CoreMaterials);
        }

        [Test]
        public void SaveManager_ValidSave_LoadsCorrectly()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", 15);
            PlayerPrefs.SetInt("stats_total_wins", 5);
            PlayerPrefs.SetInt("stats_total_losses", 3);
            PlayerPrefs.SetInt("stats_total_runs", 8);
            PlayerPrefs.SetInt("lobby_selected_stage", 2);
            PlayerPrefs.SetInt("stats_highest_stage_unlocked", 3);
            PlayerPrefs.SetInt("stats_stage_1_completed", 1);
            PlayerPrefs.SetString("lobby_selected_starting_defender", "bombardier");
            PlayerPrefs.SetInt("stats_meta_gold", 500);
            PlayerPrefs.SetInt("stats_account_xp", 1200);
            PlayerPrefs.SetInt("stats_core_materials", 10);

            SaveManager.LoadProgress();

            Assert.AreEqual(15, SaveManager.BestWave);
            Assert.AreEqual(5, SaveManager.TotalWins);
            Assert.AreEqual(3, SaveManager.TotalLosses);
            Assert.AreEqual(8, SaveManager.TotalRuns);
            Assert.AreEqual(2, SaveManager.SelectedStageIndex);
            Assert.AreEqual(3, SaveManager.HighestStageUnlocked);
            Assert.IsTrue(SaveManager.Stage1Completed);
            Assert.AreEqual("bombardier", SaveManager.SelectedStartingDefenderId);
            Assert.AreEqual(500, SaveManager.MetaGold);
            Assert.AreEqual(1200, SaveManager.AccountXp);
            Assert.AreEqual(10, SaveManager.CoreMaterials);
        }

        [Test]
        public void SaveManager_Version0_MigratesToCurrent()
        {
            PlayerPrefs.SetInt("save_version", 0);
            PlayerPrefs.SetInt("stats_best_wave", 5);
            
            // Should upgrade version to 2 and populate missing keys
            SaveManager.LoadProgress();

            Assert.AreEqual(2, PlayerPrefs.GetInt("save_version"));
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId);
        }

        [Test]
        public void SaveManager_Version1_MigratesToCurrent()
        {
            PlayerPrefs.SetInt("save_version", 1);
            
            // Should upgrade version to 2 and make sure defender key is initialized
            SaveManager.LoadProgress();

            Assert.AreEqual(2, PlayerPrefs.GetInt("save_version"));
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId);
        }

        [Test]
        public void SaveManager_PartiallyMissingSave_HandlesGracefully()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_meta_gold", 250);
            // stats_account_xp is missing

            SaveManager.LoadProgress();

            Assert.AreEqual(250, SaveManager.MetaGold);
            Assert.AreEqual(0, SaveManager.AccountXp);
        }

        [Test]
        public void SaveManager_NegativeAndCorruptValues_Clamped()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", -10);
            PlayerPrefs.SetInt("stats_total_wins", -5);
            PlayerPrefs.SetInt("stats_total_losses", -1);
            PlayerPrefs.SetInt("stats_meta_gold", -100);
            PlayerPrefs.SetInt("stats_account_xp", -50);
            PlayerPrefs.SetInt("stats_core_materials", -5);
            PlayerPrefs.SetInt("lobby_selected_stage", -3);
            PlayerPrefs.SetInt("stats_highest_stage_unlocked", -1);

            SaveManager.LoadProgress();

            Assert.AreEqual(0, SaveManager.BestWave);
            Assert.AreEqual(0, SaveManager.TotalWins);
            Assert.AreEqual(0, SaveManager.TotalLosses);
            Assert.AreEqual(0, SaveManager.SelectedStageIndex);
            Assert.AreEqual(1, SaveManager.HighestStageUnlocked); // Minimum should be 1
            Assert.AreEqual(0, SaveManager.MetaGold);
            Assert.AreEqual(0, SaveManager.AccountXp);
            Assert.AreEqual(0, SaveManager.CoreMaterials);
        }

        [Test]
        public void SaveManager_UnknownStartingDefender_ResetsToArcher()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetString("lobby_selected_starting_defender", "invalid_hero_id");

            SaveManager.LoadProgress();

            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId);
        }

        [Test]
        public void SaveManager_ExcessivelyLargeValues_Clamped()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", 999999);
            PlayerPrefs.SetInt("stats_meta_gold", 99999999);
            PlayerPrefs.SetInt("stats_account_xp", 99999999);
            PlayerPrefs.SetInt("stats_core_materials", 99999999);

            SaveManager.LoadProgress();

            Assert.AreEqual(1000, SaveManager.BestWave); // Clamped to 1000
            Assert.AreEqual(9999999, SaveManager.MetaGold); // Clamped to 9999999
            Assert.AreEqual(9999999, SaveManager.AccountXp); // Clamped to 9999999
            Assert.AreEqual(999999, SaveManager.CoreMaterials); // Clamped to 999999
        }

        [Test]
        public void SaveManager_RewardClaimSafety()
        {
            SaveManager.BeginRunRewardSession();
            
            int gold, xp, mats;
            bool success1 = SaveManager.TryClaimRunRewards(10, out gold, out xp, out mats);
            Assert.IsTrue(success1);
            Assert.AreEqual(500, gold);
            Assert.AreEqual(20, xp);
            Assert.AreEqual(50, mats);

            // Attempting to claim again in the same session must fail
            bool success2 = SaveManager.TryClaimRunRewards(10, out gold, out xp, out mats);
            Assert.IsFalse(success2);
        }
    }
}
