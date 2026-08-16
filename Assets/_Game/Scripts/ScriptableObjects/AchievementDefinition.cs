using UnityEngine;

namespace Stonehold
{
    public enum AchievementCategory
    {
        Combat,
        Synergies,
        Progression,
        Mastery,
        Endless
    }

    [CreateAssetMenu(fileName = "NewAchievement", menuName = "Stonehold/Achievement Definition")]
    public class AchievementDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 4)]
        public string description;
        public AchievementCategory category = AchievementCategory.Combat;
        public float targetValue = 1f;
        public int rewardGold = 100;
        public int rewardMaterials = 25;
        public string iconBadge = "🏆";
        public Color themeColor = new Color(1f, 0.85f, 0.25f);
    }
}
