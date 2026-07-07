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

        public static int BestWave { get; private set; }
        public static int TotalWins { get; private set; }
        public static int TotalLosses { get; private set; }
        public static int TotalRuns { get; private set; }

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

            PlayerPrefs.DeleteKey(KeyBestWave);
            PlayerPrefs.DeleteKey(KeyTotalWins);
            PlayerPrefs.DeleteKey(KeyTotalLosses);
            PlayerPrefs.DeleteKey(KeyTotalRuns);
            PlayerPrefs.Save();
        }
    }
}
