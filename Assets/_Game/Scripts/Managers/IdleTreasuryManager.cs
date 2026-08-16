using System;
using UnityEngine;

namespace Stonehold
{
    [Serializable]
    public struct DailyRewardInfo
    {
        public int dayNumber;
        public string title;
        public int gold;
        public int materials;
        public string iconBadge;
    }

    /// <summary>
    /// Manages AFK idle resource accumulation (Gold + Core Materials over time, capped at 8 hours)
    /// and 7-day daily login streak rewards with PlayerPrefs persistence.
    /// </summary>
    public class IdleTreasuryManager : MonoBehaviour
    {
        public static IdleTreasuryManager Instance { get; private set; }

        private const string KeyLastTreasuryTimestamp = "idle_treasury_last_timestamp";
        private const string KeyDailyLastClaimDate = "daily_reward_last_claim_date";
        private const string KeyDailyStreakCount = "daily_reward_streak_count";

        public const float GoldPerHour = 50f;
        public const float MaterialsPerHour = 5f;
        public const float MaxCapacityHours = 8f;
        public const float MaxCapacitySeconds = MaxCapacityHours * 3600f;

        private static readonly DailyRewardInfo[] StreakRewards =
        {
            new DailyRewardInfo { dayNumber = 1, title = "Day 1 Tribute", gold = 150, materials = 0, iconBadge = "🪙" },
            new DailyRewardInfo { dayNumber = 2, title = "Day 2 Supplies", gold = 200, materials = 10, iconBadge = "📦" },
            new DailyRewardInfo { dayNumber = 3, title = "Day 3 Cache", gold = 300, materials = 15, iconBadge = "💎" },
            new DailyRewardInfo { dayNumber = 4, title = "Day 4 Armory", gold = 400, materials = 25, iconBadge = "⚔️" },
            new DailyRewardInfo { dayNumber = 5, title = "Day 5 Vault", gold = 600, materials = 35, iconBadge = "🛡️" },
            new DailyRewardInfo { dayNumber = 6, title = "Day 6 Reliquary", gold = 800, materials = 50, iconBadge = "👑" },
            new DailyRewardInfo { dayNumber = 7, title = "Day 7 Grand Cache", gold = 1200, materials = 100, iconBadge = "🌌" }
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialTimestamps();
        }

        private static void EnsureInitialTimestamps()
        {
            if (!PlayerPrefs.HasKey(KeyLastTreasuryTimestamp))
            {
                PlayerPrefs.SetString(KeyLastTreasuryTimestamp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                PlayerPrefs.Save();
            }
        }

        public long GetLastTreasuryTimestamp()
        {
            string str = PlayerPrefs.GetString(KeyLastTreasuryTimestamp, "");
            if (long.TryParse(str, out long val)) return val;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetString(KeyLastTreasuryTimestamp, now.ToString());
            return now;
        }

        public float GetElapsedSeconds()
        {
            long last = GetLastTreasuryTimestamp();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < last) return 0f;
            return Mathf.Min((float)(now - last), MaxCapacitySeconds);
        }

        public int AccumulatedGold
        {
            get
            {
                float elapsed = GetElapsedSeconds();
                return Mathf.FloorToInt((elapsed / 3600f) * GoldPerHour);
            }
        }

        public int AccumulatedMaterials
        {
            get
            {
                float elapsed = GetElapsedSeconds();
                return Mathf.FloorToInt((elapsed / 3600f) * MaterialsPerHour);
            }
        }

        public float FillPercentage01 => Mathf.Clamp01(GetElapsedSeconds() / MaxCapacitySeconds);

        public bool ClaimTreasury(out int goldEarned, out int materialsEarned)
        {
            goldEarned = AccumulatedGold;
            materialsEarned = AccumulatedMaterials;

            if (goldEarned <= 0 && materialsEarned <= 0)
            {
                return false;
            }

            SaveManager.AddMetaGold(goldEarned);
            SaveManager.AddCoreMaterials(materialsEarned);

            PlayerPrefs.SetString(KeyLastTreasuryTimestamp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();

            HapticFeedbackManager.TriggerMedium();
            return true;
        }

        // ------------------------------------------------------------- Daily Login Streak

        public int CurrentStreakIndex => Mathf.Clamp(PlayerPrefs.GetInt(KeyDailyStreakCount, 0), 0, StreakRewards.Length - 1);

        public bool IsDailyRewardAvailable()
        {
            string lastClaim = PlayerPrefs.GetString(KeyDailyLastClaimDate, "");
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return lastClaim != today;
        }

        public DailyRewardInfo GetTodayReward()
        {
            return StreakRewards[CurrentStreakIndex];
        }

        public DailyRewardInfo GetRewardForDay(int dayIndex0)
        {
            int clamped = Mathf.Clamp(dayIndex0, 0, StreakRewards.Length - 1);
            return StreakRewards[clamped];
        }

        public bool ClaimDailyReward(out DailyRewardInfo claimedReward)
        {
            claimedReward = GetTodayReward();
            if (!IsDailyRewardAvailable())
            {
                return false;
            }

            SaveManager.AddMetaGold(claimedReward.gold);
            SaveManager.AddCoreMaterials(claimedReward.materials);

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            string lastClaim = PlayerPrefs.GetString(KeyDailyLastClaimDate, "");

            int nextStreak = (lastClaim == yesterday) ? (CurrentStreakIndex + 1) % StreakRewards.Length : 0;
            if (lastClaim == "") nextStreak = 1 % StreakRewards.Length;

            PlayerPrefs.SetString(KeyDailyLastClaimDate, today);
            PlayerPrefs.SetInt(KeyDailyStreakCount, nextStreak);
            PlayerPrefs.Save();

            HapticFeedbackManager.TriggerHeavy();
            return true;
        }

        public static void ResetForTesting()
        {
            Instance = null;
            PlayerPrefs.DeleteKey(KeyLastTreasuryTimestamp);
            PlayerPrefs.DeleteKey(KeyDailyLastClaimDate);
            PlayerPrefs.DeleteKey(KeyDailyStreakCount);
        }
    }
}
