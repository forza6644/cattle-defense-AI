using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Handles lightweight persistence for player progress stats using PlayerPrefs.
    /// </summary>
    public static class SaveManager
    {
        private const string KeyBestWave = "stats_best_wave";
        private const string KeyTotalWins = "stats_total_wins";
        private const string KeyTotalLosses = "stats_total_losses";
        private const string KeyTotalRuns = "stats_total_runs";
        private const string KeySelectedStage = "lobby_selected_stage";
        private const string KeyHighestStageUnlocked = "stats_highest_stage_unlocked";
        private const string KeyStage1Completed = "stats_stage_1_completed";

        public static int BestWave { get; private set; }
        public static int TotalWins { get; private set; }
        public static int TotalLosses { get; private set; }
        public static int TotalRuns { get; private set; }
        public static int SelectedStageIndex { get; private set; }
        public static int HighestStageUnlocked { get; private set; }
        public static bool Stage1Completed { get; private set; }

        static SaveManager()
        {
            LoadProgress();
        }

        public static void LoadProgress()
        {
            BestWave = PlayerPrefs.GetInt(KeyBestWave, 0);
            TotalWins = PlayerPrefs.GetInt(KeyTotalWins, 0);
            TotalLosses = PlayerPrefs.GetInt(KeyTotalLosses, 0);
            TotalRuns = PlayerPrefs.GetInt(KeyTotalRuns, 0);
            SelectedStageIndex = PlayerPrefs.GetInt(KeySelectedStage, 0);
            HighestStageUnlocked = PlayerPrefs.GetInt(KeyHighestStageUnlocked, 1);
            Stage1Completed = PlayerPrefs.GetInt(KeyStage1Completed, 0) == 1;
        }

        public static void SetSelectedStage(int index)
        {
            SelectedStageIndex = index;
            PlayerPrefs.SetInt(KeySelectedStage, SelectedStageIndex);
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
            Stage1Completed = true;
            PlayerPrefs.SetInt(KeyStage1Completed, 1);
            UnlockStage(2);
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

        public static void ResetProgress()
        {
            BestWave = 0;
            TotalWins = 0;
            TotalLosses = 0;
            TotalRuns = 0;
            SelectedStageIndex = 0;
            HighestStageUnlocked = 1;
            Stage1Completed = false;

            PlayerPrefs.DeleteKey(KeyBestWave);
            PlayerPrefs.DeleteKey(KeyTotalWins);
            PlayerPrefs.DeleteKey(KeyTotalLosses);
            PlayerPrefs.DeleteKey(KeyTotalRuns);
            PlayerPrefs.DeleteKey(KeySelectedStage);
            PlayerPrefs.DeleteKey(KeyHighestStageUnlocked);
            PlayerPrefs.DeleteKey(KeyStage1Completed);
            PlayerPrefs.Save();
        }
    }
}
