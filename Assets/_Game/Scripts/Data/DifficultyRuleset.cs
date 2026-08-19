using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Maps player-facing difficulty to the existing Heat mutator framework.
    /// Hard uses a curated subset — not every mutator — so the mode stays fair.
    /// </summary>
    public static class DifficultyRuleset
    {
        public const string PrefsSelectedMode = "lobby_selected_difficulty";

        public static readonly string[] HardMutatorIds =
        {
            "fast_enemies",
            "armored_horde",
            "empowered_elites",
            "hyper_waves"
        };

        public const float HardGoldRewardMultiplier = 1.50f;
        public const float HardEnemySpeedBonus = 0.20f;
        public const float HardEliteHealthBonus = 0.50f;
        public const float HardWaveCountdownReduction = 0.50f;

        public static DifficultyMode ClampSelectable(DifficultyMode mode)
        {
            if (mode == DifficultyMode.Nightmare)
            {
                return DifficultyMode.Hard;
            }

            return mode == DifficultyMode.Hard ? DifficultyMode.Hard : DifficultyMode.Normal;
        }

        public static DifficultyMode GetSelectedMode()
        {
            int stored = PlayerPrefs.GetInt(PrefsSelectedMode, (int)DifficultyMode.Normal);
            return ClampSelectable((DifficultyMode)stored);
        }

        public static void SetSelectedMode(DifficultyMode mode)
        {
            DifficultyMode selectable = ClampSelectable(mode);
            if (selectable == DifficultyMode.Hard && !IsHardUnlocked())
            {
                selectable = DifficultyMode.Normal;
            }

            PlayerPrefs.SetInt(PrefsSelectedMode, (int)selectable);
            PlayerPrefs.Save();
        }

        public static bool IsHardUnlocked()
        {
            if (SaveManager.Stage1Completed || PlayerPrefs.GetInt("stats_stage_1_completed", 0) == 1)
            {
                return true;
            }

            if (CampaignProgressionManager.Instance != null
                && CampaignProgressionManager.Instance.GetStarsForStage(0) > 0)
            {
                return true;
            }

            return PlayerPrefs.GetInt("campaign_stars_stage_0", 0) > 0;
        }

        public static bool ContainsHardMutator(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            for (int i = 0; i < HardMutatorIds.Length; i++)
            {
                if (HardMutatorIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        public static string FormatHardSummary()
        {
            return "Enemies +20% Speed\nElites / Bosses +50% HP\nWave prep 50% faster\nArmor plating active\n\nRewards +50% Gold\n+1★ First Clear (Hard)";
        }
    }
}
