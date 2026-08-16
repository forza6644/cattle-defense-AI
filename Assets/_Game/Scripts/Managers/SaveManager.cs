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
        private const string KeySelectedStarterCrystal = "lobby_selected_starter_crystal";
        private const string KeyMetaGold = "stats_meta_gold";
        private const string KeyAccountXp = "stats_account_xp";
        private const string KeyCoreMaterials = "stats_core_materials";
        private const string KeyEndlessAbyssHighestWave = "endless_abyss_highest_wave";
        private const string KeyEndlessAbyssHighScore = "endless_abyss_high_score";

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
            "range",
            "gold_bonus"
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
        public static string SelectedStarterCrystalId { get; private set; }
        public static int MetaGold { get; private set; }
        public static int Coins => MetaGold;
        public static int AccountXp { get; private set; }
        public static int CoreMaterials { get; private set; }
        public static int EndlessAbyssHighestWave { get; private set; }
        public static int EndlessAbyssHighScore { get; private set; }

        static SaveManager()
        {
            LoadProgress();
        }

        public static void SaveProgress()
        {
            PlayerPrefs.Save();
        }

        public static void Save() => SaveProgress();
        public static void Load() => LoadProgress();

        public static void LoadProgress()
        {
            EnsureSaveVersion();

            // Load and sanitize values
            BestWave = Mathf.Clamp(PlayerPrefs.GetInt(KeyBestWave, 0), 0, 1000);
            TotalWins = Mathf.Max(0, PlayerPrefs.GetInt(KeyTotalWins, 0));
            TotalLosses = Mathf.Max(0, PlayerPrefs.GetInt(KeyTotalLosses, 0));
            TotalRuns = Mathf.Max(TotalWins + TotalLosses, PlayerPrefs.GetInt(KeyTotalRuns, 0));
            SelectedStageIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeySelectedStage, 0), 0, 100);
            HighestStageUnlocked = Mathf.Clamp(PlayerPrefs.GetInt(KeyHighestStageUnlocked, 1), 1, 100);
            Stage1Completed = PlayerPrefs.GetInt(KeyStage1Completed, 0) == 1;

            string startingDefender = PlayerPrefs.GetString(KeySelectedStartingDefender, "archer");
            if (System.Array.IndexOf(CurrentHeroIds, startingDefender) < 0)
            {
                startingDefender = "archer";
            }
            SelectedStartingDefenderId = startingDefender;

            SelectedStarterCrystalId = PlayerPrefs.GetString(KeySelectedStarterCrystal, "crystal_lightning");

            MetaGold = Mathf.Clamp(PlayerPrefs.GetInt(KeyMetaGold, 0), 0, 9999999);
            AccountXp = Mathf.Clamp(PlayerPrefs.GetInt(KeyAccountXp, 0), 0, 9999999);
            CoreMaterials = Mathf.Clamp(PlayerPrefs.GetInt(KeyCoreMaterials, 0), 0, 999999);
            EndlessAbyssHighestWave = Mathf.Max(0, PlayerPrefs.GetInt(KeyEndlessAbyssHighestWave, 0));
            EndlessAbyssHighScore = Mathf.Max(0, PlayerPrefs.GetInt(KeyEndlessAbyssHighScore, 0));

            // Clean invalid/corrupt values in PlayerPrefs by writing back sanitized values
            PlayerPrefs.SetInt(KeyBestWave, BestWave);
            PlayerPrefs.SetInt(KeyTotalWins, TotalWins);
            PlayerPrefs.SetInt(KeyTotalLosses, TotalLosses);
            PlayerPrefs.SetInt(KeyTotalRuns, TotalRuns);
            PlayerPrefs.SetInt(KeySelectedStage, SelectedStageIndex);
            PlayerPrefs.SetInt(KeyHighestStageUnlocked, HighestStageUnlocked);
            PlayerPrefs.SetInt(KeyEndlessAbyssHighestWave, EndlessAbyssHighestWave);
            PlayerPrefs.SetInt(KeyEndlessAbyssHighScore, EndlessAbyssHighScore);
            PlayerPrefs.SetInt(KeyStage1Completed, Stage1Completed ? 1 : 0);
            PlayerPrefs.SetString(KeySelectedStartingDefender, SelectedStartingDefenderId);
            PlayerPrefs.SetString(KeySelectedStarterCrystal, SelectedStarterCrystalId);
            PlayerPrefs.SetInt(KeyMetaGold, MetaGold);
            PlayerPrefs.SetInt(KeyAccountXp, AccountXp);
            PlayerPrefs.SetInt(KeyCoreMaterials, CoreMaterials);

            // Sanitize hero levels
            foreach (string heroId in CurrentHeroIds)
            {
                string key = "meta_level_" + heroId;
                int lvl = Mathf.Clamp(PlayerPrefs.GetInt(key, 1), 1, 100);
                PlayerPrefs.SetInt(key, lvl);
            }

            // Sanitize meta upgrades
            foreach (string upgradeId in CurrentMetaUpgradeIds)
            {
                string key = "meta_upgrade_" + upgradeId;
                int lvl = Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, 10);
                PlayerPrefs.SetInt(key, lvl);
            }

            PlayerPrefs.Save();
        }

        public static void BeginRunRewardSession()
        {
            runRewardsClaimed = false;
        }

        public static bool TryClaimRunRewards(int waveReached, out int gold, out int xp, out int materials)
        {
            int safeWave = Mathf.Max(1, waveReached);
            float scoreMult = AscensionManager.Instance != null ? AscensionManager.Instance.GetScoreMultiplier() : 1.0f;
            gold = Mathf.RoundToInt(safeWave * 50 * scoreMult);
            xp = Mathf.RoundToInt(safeWave * 2 * scoreMult);
            materials = Mathf.RoundToInt(safeWave * 5 * scoreMult);

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

            if (savedVersion < 1)
            {
                foreach (string upgradeId in CurrentMetaUpgradeIds)
                {
                    string key = "meta_upgrade_" + upgradeId;
                    if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetInt(key, 0);
                }
            }
            if (savedVersion < 2)
            {
                if (!PlayerPrefs.HasKey(KeySelectedStartingDefender))
                {
                    PlayerPrefs.SetString(KeySelectedStartingDefender, "archer");
                }
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

        public static void SetSelectedStarterCrystal(string crystalId)
        {
            if (string.IsNullOrEmpty(crystalId)) crystalId = "crystal_lightning";
            SelectedStarterCrystalId = crystalId;
            PlayerPrefs.SetString(KeySelectedStarterCrystal, SelectedStarterCrystalId);
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

        public static void UnlockStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return;
            if (stageId.Contains("2") || stageId.Contains("highlands"))
            {
                UnlockStage(2);
            }
            else if (stageId.Contains("3") || stageId.Contains("frozen") || stageId.Contains("peak"))
            {
                UnlockStage(3);
            }
            else if (stageId.Contains("1") || stageId.Contains("castle") || stageId.Contains("road"))
            {
                UnlockStage(1);
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

        public static void RecordEndlessAbyssWave(int wave, int score)
        {
            bool changed = false;
            if (wave > EndlessAbyssHighestWave)
            {
                EndlessAbyssHighestWave = wave;
                PlayerPrefs.SetInt(KeyEndlessAbyssHighestWave, EndlessAbyssHighestWave);
                changed = true;
            }
            if (score > EndlessAbyssHighScore)
            {
                EndlessAbyssHighScore = score;
                PlayerPrefs.SetInt(KeyEndlessAbyssHighScore, EndlessAbyssHighScore);
                changed = true;
            }
            if (changed)
            {
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

        public static void AddCoreMaterials(int amount)
        {
            CoreMaterials += amount;
            PlayerPrefs.SetInt(KeyCoreMaterials, CoreMaterials);
            PlayerPrefs.Save();
        }

        public static void AddAccountXp(int amount)
        {
            AccountXp += amount;
            PlayerPrefs.SetInt(KeyAccountXp, AccountXp);
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

        public static bool TryPurchaseUpgrade(string upgradeId, int cost)
        {
            if (string.IsNullOrEmpty(upgradeId) || cost <= 0 || MetaGold < cost)
            {
                return false;
            }

            MetaGold -= cost;
            PlayerPrefs.SetInt(KeyMetaGold, MetaGold);
            int currentLevel = GetUpgradeLevel(upgradeId);
            SetUpgradeLevel(upgradeId, currentLevel + 1);
            SaveProgress();
            return true;
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
            EndlessAbyssHighestWave = 0;
            EndlessAbyssHighScore = 0;

            PlayerPrefs.DeleteKey(KeyBestWave);
            PlayerPrefs.DeleteKey(KeyTotalWins);
            PlayerPrefs.DeleteKey(KeyTotalLosses);
            PlayerPrefs.DeleteKey(KeyTotalRuns);
            PlayerPrefs.DeleteKey(KeySelectedStage);
            PlayerPrefs.DeleteKey(KeyHighestStageUnlocked);
            PlayerPrefs.DeleteKey(KeyStage1Completed);
            PlayerPrefs.DeleteKey(KeyAccountXp);
            PlayerPrefs.DeleteKey(KeyCoreMaterials);
            PlayerPrefs.DeleteKey(KeyEndlessAbyssHighestWave);
            PlayerPrefs.DeleteKey(KeyEndlessAbyssHighScore);

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
