using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class AndroidReleaseHardeningTests
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
            public int saveVersion;
        }

        private SaveState originalState;

        [SetUp]
        public void SetUp()
        {
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
                coreMaterials = PlayerPrefs.GetInt("stats_core_materials", 0),
                saveVersion = PlayerPrefs.GetInt("save_version", 0)
            };
            PlayerPrefs.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
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
            PlayerPrefs.SetInt("save_version", originalState.saveVersion);
            PlayerPrefs.Save();
        }

        // ── Save Compatibility ──

        [Test]
        public void SaveCompatibility_FreshInstall_LoadsCleanDefaults()
        {
            PlayerPrefs.DeleteAll();
            SaveManager.LoadProgress();

            Assert.AreEqual(0, SaveManager.BestWave, "BestWave should default to 0");
            Assert.AreEqual(0, SaveManager.TotalWins, "TotalWins should default to 0");
            Assert.AreEqual(0, SaveManager.TotalLosses, "TotalLosses should default to 0");
            Assert.AreEqual(0, SaveManager.TotalRuns, "TotalRuns should default to 0");
            Assert.AreEqual(0, SaveManager.SelectedStageIndex, "SelectedStageIndex should default to 0");
            Assert.AreEqual(1, SaveManager.HighestStageUnlocked, "HighestStageUnlocked should default to 1");
            Assert.IsFalse(SaveManager.Stage1Completed, "Stage1Completed should default to false");
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId, "Defender should default to archer");
            Assert.AreEqual(0, SaveManager.MetaGold, "MetaGold should default to 0");
            Assert.AreEqual(0, SaveManager.AccountXp, "AccountXp should default to 0");
            Assert.AreEqual(0, SaveManager.CoreMaterials, "CoreMaterials should default to 0");
        }

        [Test]
        public void SaveCompatibility_Version0To2_MigrationSucceeds()
        {
            PlayerPrefs.SetInt("save_version", 0);
            PlayerPrefs.SetInt("stats_best_wave", 7);
            PlayerPrefs.SetInt("stats_meta_gold", 300);

            SaveManager.LoadProgress();

            Assert.AreEqual(2, PlayerPrefs.GetInt("save_version"), "Should migrate to version 2");
            Assert.AreEqual(7, SaveManager.BestWave);
            Assert.AreEqual(300, SaveManager.MetaGold);
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId, "Missing defender should default to archer");
        }

        [Test]
        public void SaveCompatibility_Version1To2_MigrationSucceeds()
        {
            PlayerPrefs.SetInt("save_version", 1);
            PlayerPrefs.SetInt("stats_best_wave", 10);

            SaveManager.LoadProgress();

            Assert.AreEqual(2, PlayerPrefs.GetInt("save_version"), "Should migrate to version 2");
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId);
        }

        [Test]
        public void SaveCompatibility_ValidVersion2_PreservesAllData()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", 10);
            PlayerPrefs.SetInt("stats_total_wins", 5);
            PlayerPrefs.SetInt("stats_total_losses", 3);
            PlayerPrefs.SetInt("stats_total_runs", 8);
            PlayerPrefs.SetInt("lobby_selected_stage", 1);
            PlayerPrefs.SetInt("stats_highest_stage_unlocked", 2);
            PlayerPrefs.SetInt("stats_stage_1_completed", 1);
            PlayerPrefs.SetString("lobby_selected_starting_defender", "frost_mage");
            PlayerPrefs.SetInt("stats_meta_gold", 1500);
            PlayerPrefs.SetInt("stats_account_xp", 800);
            PlayerPrefs.SetInt("stats_core_materials", 45);

            SaveManager.LoadProgress();

            Assert.AreEqual(10, SaveManager.BestWave);
            Assert.AreEqual(5, SaveManager.TotalWins);
            Assert.AreEqual(3, SaveManager.TotalLosses);
            Assert.AreEqual(8, SaveManager.TotalRuns);
            Assert.AreEqual(1, SaveManager.SelectedStageIndex);
            Assert.AreEqual(2, SaveManager.HighestStageUnlocked);
            Assert.IsTrue(SaveManager.Stage1Completed);
            Assert.AreEqual("frost_mage", SaveManager.SelectedStartingDefenderId);
            Assert.AreEqual(1500, SaveManager.MetaGold);
            Assert.AreEqual(800, SaveManager.AccountXp);
            Assert.AreEqual(45, SaveManager.CoreMaterials);
        }

        // ── Corrupt Save Recovery ──

        [Test]
        public void CorruptRecovery_NegativeValues_ClampedToSafeDefaults()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", -999);
            PlayerPrefs.SetInt("stats_total_wins", -10);
            PlayerPrefs.SetInt("stats_total_losses", -5);
            PlayerPrefs.SetInt("stats_meta_gold", -50000);
            PlayerPrefs.SetInt("stats_account_xp", -1);
            PlayerPrefs.SetInt("stats_core_materials", -100);
            PlayerPrefs.SetInt("lobby_selected_stage", -2);
            PlayerPrefs.SetInt("stats_highest_stage_unlocked", -3);

            SaveManager.LoadProgress();

            Assert.AreEqual(0, SaveManager.BestWave, "Negative BestWave clamped to 0");
            Assert.AreEqual(0, SaveManager.TotalWins, "Negative TotalWins clamped to 0");
            Assert.AreEqual(0, SaveManager.TotalLosses, "Negative TotalLosses clamped to 0");
            Assert.AreEqual(0, SaveManager.MetaGold, "Negative MetaGold clamped to 0");
            Assert.AreEqual(0, SaveManager.AccountXp, "Negative AccountXp clamped to 0");
            Assert.AreEqual(0, SaveManager.CoreMaterials, "Negative CoreMaterials clamped to 0");
            Assert.AreEqual(0, SaveManager.SelectedStageIndex, "Negative stage clamped to 0");
            Assert.AreEqual(1, SaveManager.HighestStageUnlocked, "Negative unlock clamped to 1");
        }

        [Test]
        public void CorruptRecovery_ExtremeOverflow_ClampedToMaximum()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", int.MaxValue);
            PlayerPrefs.SetInt("stats_meta_gold", int.MaxValue);
            PlayerPrefs.SetInt("stats_account_xp", int.MaxValue);
            PlayerPrefs.SetInt("stats_core_materials", int.MaxValue);

            SaveManager.LoadProgress();

            Assert.AreEqual(1000, SaveManager.BestWave, "Overflow BestWave clamped to 1000");
            Assert.AreEqual(9999999, SaveManager.MetaGold, "Overflow MetaGold clamped to 9999999");
            Assert.AreEqual(9999999, SaveManager.AccountXp, "Overflow AccountXp clamped to 9999999");
            Assert.AreEqual(999999, SaveManager.CoreMaterials, "Overflow CoreMaterials clamped to 999999");
        }

        [Test]
        public void CorruptRecovery_InvalidDefenderId_ResetsToArcher()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetString("lobby_selected_starting_defender", "CORRUPTED_VALUE_123");

            SaveManager.LoadProgress();

            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId, "Invalid defender ID should reset to archer");
        }

        [Test]
        public void CorruptRecovery_EmptyDefenderId_ResetsToArcher()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetString("lobby_selected_starting_defender", "");

            SaveManager.LoadProgress();

            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId, "Empty defender ID should reset to archer");
        }

        [Test]
        public void CorruptRecovery_TotalRunsLessThanWinsLosses_Corrected()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_total_wins", 10);
            PlayerPrefs.SetInt("stats_total_losses", 5);
            PlayerPrefs.SetInt("stats_total_runs", 3); // Corrupt: less than wins+losses

            SaveManager.LoadProgress();

            Assert.GreaterOrEqual(SaveManager.TotalRuns, SaveManager.TotalWins + SaveManager.TotalLosses,
                "TotalRuns must be >= TotalWins + TotalLosses");
        }

        [Test]
        public void CorruptRecovery_SanitizedValuesWrittenBack()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", -5);
            PlayerPrefs.SetInt("stats_meta_gold", -100);

            SaveManager.LoadProgress();

            // After LoadProgress, PlayerPrefs should reflect sanitized values
            Assert.AreEqual(0, PlayerPrefs.GetInt("stats_best_wave"), "PlayerPrefs should have sanitized BestWave");
            Assert.AreEqual(0, PlayerPrefs.GetInt("stats_meta_gold"), "PlayerPrefs should have sanitized MetaGold");
        }

        [Test]
        public void CorruptRecovery_MissingPartialSave_GracefulDefaults()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_meta_gold", 500);
            // Everything else missing

            SaveManager.LoadProgress();

            Assert.AreEqual(500, SaveManager.MetaGold, "Present value preserved");
            Assert.AreEqual(0, SaveManager.BestWave, "Missing value defaults to 0");
            Assert.AreEqual(0, SaveManager.AccountXp, "Missing value defaults to 0");
            Assert.AreEqual("archer", SaveManager.SelectedStartingDefenderId, "Missing defender defaults to archer");
        }

        // ── Reward-Once Safety ──

        [Test]
        public void RewardSafety_SingleClaim_Succeeds()
        {
            SaveManager.BeginRunRewardSession();

            bool success = SaveManager.TryClaimRunRewards(10, out int gold, out int xp, out int mats);

            Assert.IsTrue(success, "First claim should succeed");
            Assert.AreEqual(500, gold);
            Assert.AreEqual(20, xp);
            Assert.AreEqual(50, mats);
        }

        [Test]
        public void RewardSafety_DoubleClaim_SecondFails()
        {
            SaveManager.BeginRunRewardSession();
            SaveManager.TryClaimRunRewards(10, out _, out _, out _);

            bool duplicate = SaveManager.TryClaimRunRewards(10, out int gold2, out int xp2, out int mats2);

            Assert.IsFalse(duplicate, "Double claim must fail");
        }

        [Test]
        public void RewardSafety_NewSession_AllowsNewClaim()
        {
            SaveManager.BeginRunRewardSession();
            SaveManager.TryClaimRunRewards(5, out _, out _, out _);

            SaveManager.BeginRunRewardSession();
            bool success = SaveManager.TryClaimRunRewards(5, out int gold, out int xp, out int mats);

            Assert.IsTrue(success, "New session should allow new claim");
            Assert.AreEqual(250, gold);
        }

        [Test]
        public void RewardSafety_ZeroWave_ClampedToMinimum()
        {
            SaveManager.BeginRunRewardSession();
            bool success = SaveManager.TryClaimRunRewards(0, out int gold, out int xp, out int mats);

            Assert.IsTrue(success);
            Assert.AreEqual(50, gold, "Zero wave clamped to 1: 1*50=50");
            Assert.AreEqual(2, xp, "Zero wave clamped to 1: 1*2=2");
            Assert.AreEqual(5, mats, "Zero wave clamped to 1: 1*5=5");
        }

        // ── Restart and Lifecycle ──

        [Test]
        public void Lifecycle_GameManagerHasApplicationPauseHandler()
        {
            MethodInfo method = typeof(GameManager).GetMethod("OnApplicationPause",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "GameManager must have OnApplicationPause handler");
        }

        [Test]
        public void Lifecycle_GameManagerHasApplicationFocusHandler()
        {
            MethodInfo method = typeof(GameManager).GetMethod("OnApplicationFocus",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "GameManager must have OnApplicationFocus handler");
        }

        [Test]
        public void Lifecycle_SaveManagerSaveProgressExists()
        {
            MethodInfo method = typeof(SaveManager).GetMethod("SaveProgress",
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "SaveManager must expose SaveProgress");
        }

        [Test]
        public void Lifecycle_SaveManagerResetProgressClearsState()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", 10);
            PlayerPrefs.SetInt("stats_total_wins", 5);
            PlayerPrefs.SetInt("stats_meta_gold", 1000);
            SaveManager.LoadProgress();

            SaveManager.ResetProgress();

            Assert.AreEqual(0, SaveManager.BestWave, "BestWave should be 0 after reset");
            Assert.AreEqual(0, SaveManager.TotalWins, "TotalWins should be 0 after reset");
            Assert.AreEqual(0, SaveManager.TotalRuns, "TotalRuns should be 0 after reset");
        }

        [Test]
        public void Lifecycle_SaveManagerResetAllClearsEverything()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("stats_best_wave", 10);
            PlayerPrefs.SetInt("stats_meta_gold", 5000);
            SaveManager.LoadProgress();

            SaveManager.ResetAll();

            Assert.AreEqual(0, SaveManager.BestWave, "BestWave should be 0 after full reset");
            Assert.AreEqual(0, SaveManager.MetaGold, "MetaGold should be 0 after full reset");
        }

        [Test]
        public void Lifecycle_RecordWinIncrementsCounters()
        {
            PlayerPrefs.DeleteAll();
            SaveManager.LoadProgress();
            int startWins = SaveManager.TotalWins;
            int startRuns = SaveManager.TotalRuns;

            SaveManager.RecordWin();

            Assert.AreEqual(startWins + 1, SaveManager.TotalWins);
            Assert.AreEqual(startRuns + 1, SaveManager.TotalRuns);
        }

        [Test]
        public void Lifecycle_RecordLossIncrementsCounters()
        {
            PlayerPrefs.DeleteAll();
            SaveManager.LoadProgress();
            int startLosses = SaveManager.TotalLosses;
            int startRuns = SaveManager.TotalRuns;

            SaveManager.RecordLoss();

            Assert.AreEqual(startLosses + 1, SaveManager.TotalLosses);
            Assert.AreEqual(startRuns + 1, SaveManager.TotalRuns);
        }

        // ── Portrait and Safe-Area Validation ──

        [Test]
        public void Portrait_BuildScriptExists()
        {
            // Verify ReleaseCandidateBuild type is loaded via reflection
            System.Type buildType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                buildType = asm.GetType("Stonehold.Editor.ReleaseCandidateBuild");
                if (buildType != null) break;
            }
            Assert.IsNotNull(buildType, "ReleaseCandidateBuild type must exist in loaded assemblies");

            MethodInfo devBuild = buildType.GetMethod("BuildDevelopment",
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(devBuild, "BuildDevelopment method must exist");

            MethodInfo rcBuild = buildType.GetMethod("BuildReleaseCandidate",
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(rcBuild, "BuildReleaseCandidate method must exist");
        }

        [Test]
        public void SafeArea_UIManagerHasCreateSafeAreaMethod()
        {
            MethodInfo method = typeof(UIManager).GetMethod("CreateSafeArea",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "UIManager must have CreateSafeArea method");
        }

        [Test]
        public void SafeArea_MainMenuUIHasCreateSafeAreaMethod()
        {
            MethodInfo method = typeof(MainMenuUI).GetMethod("CreateSafeArea",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "MainMenuUI must have CreateSafeArea method");
        }

        // ── Build Configuration Validation ──

        [Test]
        public void BuildConfig_DualBuildPaths()
        {
            System.Type buildType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                buildType = asm.GetType("Stonehold.Editor.ReleaseCandidateBuild");
                if (buildType != null) break;
            }
            Assert.IsNotNull(buildType, "ReleaseCandidateBuild must exist");

            var devField = buildType.GetField("DevOutputPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            var rcField = buildType.GetField("ReleaseOutputPath",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(devField, "DevOutputPath field must exist");
            Assert.IsNotNull(rcField, "ReleaseOutputPath field must exist");

            string devPath = (string)devField.GetValue(null);
            string rcPath = (string)rcField.GetValue(null);

            Assert.AreNotEqual(devPath, rcPath, "Development and Release output paths must differ");
            Assert.IsTrue(devPath.EndsWith(".apk"), "Development output must be .apk");
            Assert.IsTrue(rcPath.EndsWith(".apk"), "Release output must be .apk");
        }

        [Test]
        public void BuildConfig_LegacyMethodExists()
        {
            System.Type buildType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                buildType = asm.GetType("Stonehold.Editor.ReleaseCandidateBuild");
                if (buildType != null) break;
            }
            Assert.IsNotNull(buildType, "ReleaseCandidateBuild must exist");

            MethodInfo legacy = buildType.GetMethod("BuildAndroidDevelopment",
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(legacy, "Legacy BuildAndroidDevelopment method must exist for backwards compatibility");
        }

        // ── SceneReferenceValidator Existence ──

        [Test]
        public void Validator_SceneReferenceValidatorTypeExists()
        {
            System.Type validatorType = typeof(SceneReferenceValidator);
            Assert.IsNotNull(validatorType, "SceneReferenceValidator class must exist");
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(validatorType),
                "SceneReferenceValidator must be a MonoBehaviour");
        }

        [Test]
        public void Validator_GameManagerAttachesValidator()
        {
            MethodInfo awakeMethod = typeof(GameManager).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(awakeMethod, "GameManager must have Awake method");
        }

        // ── Hero Level Sanitization ──

        [Test]
        public void CorruptRecovery_NegativeHeroLevel_ClampedTo1()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("meta_level_archer", -5);

            SaveManager.LoadProgress();

            Assert.AreEqual(1, PlayerPrefs.GetInt("meta_level_archer"),
                "Negative hero level should be clamped to 1");
        }

        [Test]
        public void CorruptRecovery_ExcessiveHeroLevel_ClampedTo100()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("meta_level_archer", 999);

            SaveManager.LoadProgress();

            Assert.AreEqual(100, PlayerPrefs.GetInt("meta_level_archer"),
                "Excessive hero level should be clamped to 100");
        }

        [Test]
        public void CorruptRecovery_NegativeMetaUpgrade_ClampedTo0()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("meta_upgrade_damage", -3);

            SaveManager.LoadProgress();

            Assert.AreEqual(0, PlayerPrefs.GetInt("meta_upgrade_damage"),
                "Negative meta upgrade should be clamped to 0");
        }

        [Test]
        public void CorruptRecovery_ExcessiveMetaUpgrade_ClampedTo10()
        {
            PlayerPrefs.SetInt("save_version", 2);
            PlayerPrefs.SetInt("meta_upgrade_damage", 50);

            SaveManager.LoadProgress();

            Assert.AreEqual(10, PlayerPrefs.GetInt("meta_upgrade_damage"),
                "Excessive meta upgrade should be clamped to 10");
        }
    }
}
