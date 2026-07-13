using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Handles lightweight persistence for player progress stats using PlayerPrefs.
    /// </summary>
    public static class SaveManager
    {
        private const int CurrentSaveVersion = 2;
        private const string KeySaveVersion = "save_version";
        private const string KeyBestWave = "stats_best_wave";
        private const string KeyTotalWins = "stats_total_wins";
        private const string KeyTotalLosses = "stats_total_losses";
        private const string KeyTotalRuns = "stats_total_runs";
        private const string KeySelectedStage = "lobby_selected_stage";
        private const string KeyHighestStageUnlocked = "stats_highest_stage_unlocked";
        private const string KeyStage1Completed = "stats_stage_1_completed";
        private const string KeySelectedStartingDefender = "lobby_selected_starting_defender";
        private const string KeyMetaGold = "stats_meta_gold";
        private const string KeyAccountXp = "stats_account_xp";
        private const string KeyCoreMaterials = "stats_core_materials";

        private static readonly string[] CurrentHeroIds =
        {
            "archer",
            "bombardier",
            "frost_mage",
            "fire_mage",
            "electric_engineer",
            "sniper"
        };

        private static readonly string[] CurrentMetaUpgradeIds =
        {
            "castle_hp",
            "castle_regen",
            "damage",
            "fire_rate",
            "range"
        };

        private static bool runRewardsClaimed;

        public static int BestWave { get; private set; }
        public static int TotalWins { get; private set; }
        public static int TotalLosses { get; private set; }
        public static int TotalRuns { get; private set; }
        public static int SelectedStageIndex { get; private set; }
        public static int HighestStageUnlocked { get; private set; }
        public static bool Stage1Completed { get; private set; }
        public static string SelectedStartingDefenderId { get; private set; }
        public static int MetaGold { get; private set; }
        public static int Coins => MetaGold;
        public static int AccountXp { get; private set; }
        public static int CoreMaterials { get; private set; }

        static SaveManager()
        {
            LoadProgress();
        }

        public static void LoadProgress()
        {
            EnsureSaveVersion();

            BestWave = PlayerPrefs.GetInt(KeyBestWave, 0);
            TotalWins = PlayerPrefs.GetInt(KeyTotalWins, 0);
            TotalLosses = PlayerPrefs.GetInt(KeyTotalLosses, 0);
            TotalRuns = PlayerPrefs.GetInt(KeyTotalRuns, 0);
            SelectedStageIndex = PlayerPrefs.GetInt(KeySelectedStage, 0);
            HighestStageUnlocked = PlayerPrefs.GetInt(KeyHighestStageUnlocked, 1);
            Stage1Completed = PlayerPrefs.GetInt(KeyStage1Completed, 0) == 1;
            SelectedStartingDefenderId = PlayerPrefs.GetString(KeySelectedStartingDefender, "archer");
            MetaGold = PlayerPrefs.GetInt(KeyMetaGold, 0);
            AccountXp = PlayerPrefs.GetInt(KeyAccountXp, 0);
            CoreMaterials = PlayerPrefs.GetInt(KeyCoreMaterials, 0);
        }

        public static void BeginRunRewardSession()
        {
            runRewardsClaimed = false;
        }

        public static bool TryClaimRunRewards(int waveReached, out int gold, out int xp, out int materials)
        {
            int safeWave = Mathf.Max(1, waveReached);
            gold = safeWave * 50;
            xp = safeWave * 2;
            materials = safeWave * 5;

            if (runRewardsClaimed)
            {
                return false;
            }

            runRewardsClaimed = true;
            AddRewards(gold, xp, materials);
            return true;
        }

        private static void EnsureSaveVersion()
        {
            int savedVersion = PlayerPrefs.GetInt(KeySaveVersion, 0);
            if (savedVersion >= CurrentSaveVersion)
            {
                return;
            }

            PlayerPrefs.SetInt(KeySaveVersion, CurrentSaveVersion);
            PlayerPrefs.Save();
        }

        public static void SetSelectedStage(int index)
        {
            SelectedStageIndex = index;
            PlayerPrefs.SetInt(KeySelectedStage, SelectedStageIndex);
            PlayerPrefs.Save();
        }

        public static void SetSelectedStartingDefender(string defenderId)
        {
            SelectedStartingDefenderId = defenderId;
            PlayerPrefs.SetString(KeySelectedStartingDefender, SelectedStartingDefenderId);
            PlayerPrefs.Save();
        }

        public static void UnlockStage(int stageNumber)
        {
            if (stageNumber > HighestStageUnlocked)
            {
                HighestStageUnlocked = stageNumber;
                PlayerPrefs.SetInt(KeyHighestStageUnlocked, HighestStageUnlocked);
                PlayerPrefs.Save();
            }
        }

        public static void CompleteStage1()
        {
            CompleteStage(0);
        }

        public static void CompleteStage(int stageIndex)
        {
            int safeStageIndex = Mathf.Max(0, stageIndex);
            if (safeStageIndex == 0)
            {
                Stage1Completed = true;
                PlayerPrefs.SetInt(KeyStage1Completed, 1);
            }

            // Stage indexes are zero-based; unlocked stage numbers are one-based.
            UnlockStage(safeStageIndex + 2);
            PlayerPrefs.Save();
        }

        public static void UpdateBestWave(int wave)
        {
            if (wave > BestWave)
            {
                BestWave = wave;
                PlayerPrefs.SetInt(KeyBestWave, BestWave);
                PlayerPrefs.Save();
            }
        }

        public static void RecordWin()
        {
            TotalWins++;
            TotalRuns++;
            PlayerPrefs.SetInt(KeyTotalWins, TotalWins);
            PlayerPrefs.SetInt(KeyTotalRuns, TotalRuns);
            PlayerPrefs.Save();
        }

        public static void RecordLoss()
        {
            TotalLosses++;
            TotalRuns++;
            PlayerPrefs.SetInt(KeyTotalLosses, TotalLosses);
            PlayerPrefs.SetInt(KeyTotalRuns, TotalRuns);
            PlayerPrefs.Save();
        }

        public static void AddMetaGold(int amount)
        {
            MetaGold += amount;
            PlayerPrefs.SetInt(KeyMetaGold, MetaGold);
            PlayerPrefs.Save();
        }

        public static int GetMetaLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 1;
            return PlayerPrefs.GetInt("meta_level_" + id, 1);
        }

        public static void UpgradeMetaLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            int current = GetMetaLevel(id);
            PlayerPrefs.SetInt("meta_level_" + id, current + 1);
            PlayerPrefs.Save();
        }

        public static void AddRewards(int gold, int xp, int materials)
        {
            AddMetaGold(gold);
            AccountXp += xp;
            CoreMaterials += materials;
            PlayerPrefs.SetInt(KeyAccountXp, AccountXp);
            PlayerPrefs.SetInt(KeyCoreMaterials, CoreMaterials);
            PlayerPrefs.Save();
        }

        public static int GetUpgradeLevel(string upgradeId)
        {
            return PlayerPrefs.GetInt("meta_upgrade_" + upgradeId, 0);
        }

        public static void SetUpgradeLevel(string upgradeId, int level)
        {
            PlayerPrefs.SetInt("meta_upgrade_" + upgradeId, level);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            BestWave = 0;
            TotalWins = 0;
            TotalLosses = 0;
            TotalRuns = 0;
            SelectedStageIndex = 0;
            HighestStageUnlocked = 1;
            Stage1Completed = false;
            AccountXp = 0;
            CoreMaterials = 0;

            PlayerPrefs.DeleteKey(KeyBestWave);
            PlayerPrefs.DeleteKey(KeyTotalWins);
            PlayerPrefs.DeleteKey(KeyTotalLosses);
            PlayerPrefs.DeleteKey(KeyTotalRuns);
            PlayerPrefs.DeleteKey(KeySelectedStage);
            PlayerPrefs.DeleteKey(KeyHighestStageUnlocked);
            PlayerPrefs.DeleteKey(KeyStage1Completed);
            PlayerPrefs.DeleteKey(KeyAccountXp);
            PlayerPrefs.DeleteKey(KeyCoreMaterials);

            for (int i = 0; i < CurrentMetaUpgradeIds.Length; i++)
            {
                PlayerPrefs.DeleteKey("meta_upgrade_" + CurrentMetaUpgradeIds[i]);
            }

            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            ResetProgress();
            MetaGold = 0;
            PlayerPrefs.DeleteKey(KeyMetaGold);

            for (int i = 0; i < CurrentHeroIds.Length; i++)
            {
                PlayerPrefs.DeleteKey("meta_level_" + CurrentHeroIds[i]);
            }

            PlayerPrefs.Save();
        }
    }
}
