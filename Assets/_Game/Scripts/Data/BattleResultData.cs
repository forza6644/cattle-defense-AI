using System.Collections.Generic;

namespace Stonehold
{
    public enum BattleResultType
    {
        Victory,
        Defeat
    }

    [System.Serializable]
    public class RewardEntry
    {
        public string rewardName;
        public int amount;
        public string iconName;

        public RewardEntry(string name, int amt, string icon = "")
        {
            rewardName = name;
            amount = amt;
            iconName = icon;
        }
    }

    [System.Serializable]
    public class DamageReportEntry
    {
        public string heroId;
        public string displayName;
        public float damageDealt;
        public float percentage;

        public DamageReportEntry(string id, string name, float damage, float pct)
        {
            heroId = id;
            displayName = name;
            damageDealt = damage;
            percentage = pct;
        }
    }

    [System.Serializable]
    public class BattleResultData
    {
        public BattleResultType resultType;
        public int waveReached;
        public int battleNumber;
        public List<RewardEntry> rewards = new List<RewardEntry>();
        public List<DamageReportEntry> damageReport = new List<DamageReportEntry>();
        public float totalDamage;
    }

    public static class RewardCalculator
    {
        public static List<RewardEntry> CalculateRewards(int waveReached)
        {
            var list = new List<RewardEntry>();
            list.Add(new RewardEntry("Gold Coins", waveReached * 50));
            list.Add(new RewardEntry("Account XP", waveReached * 2));
            list.Add(new RewardEntry("Core Materials", waveReached * 5));
            return list;
        }
    }
}
