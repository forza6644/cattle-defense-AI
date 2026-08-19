using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central manager for the in-game Achievements, Milestones & Trophy Quests system.
    /// Tracks combat milestones, elemental synergies, progression, and claims permanent rewards.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public static event Action<AchievementDefinition> OnAchievementUnlocked;
        public static event Action OnAchievementsUpdated;

        private readonly List<AchievementDefinition> allAchievements = new List<AchievementDefinition>();
        private readonly Dictionary<string, AchievementDefinition> achievementMap = new Dictionary<string, AchievementDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<AchievementDefinition> AllAchievements
        {
            get
            {
                EnsureLoaded();
                return allAchievements;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnAchievementUnlocked = null;
            OnAchievementsUpdated = null;
        }

        public static void ResetForTesting()
        {
            Instance = null;
            OnAchievementUnlocked = null;
            OnAchievementsUpdated = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }
            Instance = this;
            LoadAchievements();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureLoaded()
        {
            if (allAchievements == null || allAchievements.Count == 0)
            {
                LoadAchievements();
            }
        }

        public void LoadAchievements()
        {
            allAchievements.Clear();
            achievementMap.Clear();

            var loaded = Resources.LoadAll<AchievementDefinition>("Achievements");
            if (loaded != null && loaded.Length > 0)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    AddAchievement(loaded[i]);
                }
            }

            if (allAchievements.Count == 0)
            {
                CreateDefaultAchievements();
            }
        }

        public void AddAchievement(AchievementDefinition achv)
        {
            if (achv == null || string.IsNullOrEmpty(achv.id)) return;
            if (!achievementMap.ContainsKey(achv.id))
            {
                allAchievements.Add(achv);
                achievementMap[achv.id] = achv;
            }
        }

        public AchievementDefinition GetAchievement(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return null;
            return achievementMap.TryGetValue(id, out var achv) ? achv : null;
        }

        public float GetProgress(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return 0f;
            return PlayerPrefs.GetFloat("achv_prog_" + id, 0f);
        }

        public bool IsUnlocked(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return false;
            return PlayerPrefs.GetInt("achv_unlocked_" + id, 0) == 1;
        }

        public bool IsClaimed(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return false;
            return PlayerPrefs.GetInt("achv_claimed_" + id, 0) == 1;
        }

        public void AddProgress(string id, float amount = 1f)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id) || amount <= 0f) return;
            var achv = GetAchievement(id);
            if (achv == null || IsUnlocked(id)) return;

            float current = GetProgress(id) + amount;
            PlayerPrefs.SetFloat("achv_prog_" + id, current);

            if (current >= achv.targetValue)
            {
                Unlock(achv);
            }
            else
            {
                PlayerPrefs.Save();
                OnAchievementsUpdated?.Invoke();
            }
        }

        public void SetProgress(string id, float value)
        {
            if (string.IsNullOrEmpty(id)) return;
            var achv = GetAchievement(id);
            if (achv == null || IsUnlocked(id)) return;

            PlayerPrefs.SetFloat("achv_prog_" + id, value);
            if (value >= achv.targetValue)
            {
                Unlock(achv);
            }
            else
            {
                PlayerPrefs.Save();
                OnAchievementsUpdated?.Invoke();
            }
        }

        private void Unlock(AchievementDefinition achv)
        {
            if (achv == null) return;
            PlayerPrefs.SetInt("achv_unlocked_" + achv.id, 1);
            PlayerPrefs.SetFloat("achv_prog_" + achv.id, achv.targetValue);
            PlayerPrefs.Save();

            Debug.Log($"[AchievementManager] 🏆 UNLOCKED: {achv.title} ({achv.id})!");
            OnAchievementUnlocked?.Invoke(achv);
            OnAchievementsUpdated?.Invoke();
            UIManager.Instance?.ShowAchievementToast(achv);
        }

        public bool ClaimReward(string id, out int goldReward, out int matReward)
        {
            goldReward = 0;
            matReward = 0;

            if (string.IsNullOrEmpty(id) || !IsUnlocked(id) || IsClaimed(id))
            {
                return false;
            }

            var achv = GetAchievement(id);
            if (achv == null) return false;

            goldReward = achv.rewardGold;
            matReward = achv.rewardMaterials;

            PlayerPrefs.SetInt("achv_claimed_" + id, 1);
            PlayerPrefs.Save();

            if (EconomyManager.Instance != null && goldReward > 0)
            {
                EconomyManager.Instance.AddGold(goldReward);
            }

            if (goldReward > 0 || matReward > 0)
            {
                SaveManager.AddRewards(goldReward, 0, matReward);
            }

            Debug.Log($"[AchievementManager] Claimed reward for {achv.title}: +{goldReward} Gold, +{matReward} Materials.");
            OnAchievementsUpdated?.Invoke();
            return true;
        }

        public int GetUnlockedCount()
        {
            int count = 0;
            for (int i = 0; i < allAchievements.Count; i++)
            {
                if (IsUnlocked(allAchievements[i].id)) count++;
            }
            return count;
        }

        public int GetTotalCount()
        {
            return allAchievements.Count;
        }

        public float GetCompletionPercentage()
        {
            if (allAchievements.Count == 0) return 0f;
            return (float)GetUnlockedCount() / allAchievements.Count * 100f;
        }

        public void ResetAllAchievements()
        {
            EnsureLoaded();
            for (int i = 0; i < allAchievements.Count; i++)
            {
                string id = allAchievements[i].id;
                PlayerPrefs.DeleteKey("achv_prog_" + id);
                PlayerPrefs.DeleteKey("achv_unlocked_" + id);
                PlayerPrefs.DeleteKey("achv_claimed_" + id);
            }
            PlayerPrefs.Save();
            OnAchievementsUpdated?.Invoke();
        }

        private void CreateDefaultAchievements()
        {
            // Combat Achievements
            AddDefault("achv_first_blood", "First Blood", "Defeat your first enemy invader.", AchievementCategory.Combat, 1, 100, 10, "⚔️");
            AddDefault("achv_slayer_100", "Centurion Slayer", "Defeat 100 total enemy invaders.", AchievementCategory.Combat, 100, 300, 25, "💀");
            AddDefault("achv_slayer_500", "Dread Vanquisher", "Defeat 500 total enemy invaders.", AchievementCategory.Combat, 500, 800, 60, "☠️");
            AddDefault("achv_boss_slayer", "Warlord's Downfall", "Slay the powerful Stage 1 Warlord Boss.", AchievementCategory.Combat, 1, 500, 40, "👑");
            AddDefault("achv_crit_100", "Deadly Precision", "Land 100 Critical Strikes on enemies.", AchievementCategory.Combat, 100, 250, 20, "🎯");

            // Synergies Achievements
            AddDefault("achv_thermal_shock", "Pyromancy & Frost", "Trigger 20 Thermal Shock (Burn + Frost) reactions.", AchievementCategory.Synergies, 20, 300, 30, "🔥");
            AddDefault("achv_overload", "High Voltage", "Trigger 20 Overload (Burn + Shock) reactions.", AchievementCategory.Synergies, 20, 300, 30, "⚡");
            AddDefault("achv_corrosive", "Toxic Catalyst", "Trigger 20 Corrosive Blast (Burn + Poison) reactions.", AchievementCategory.Synergies, 20, 300, 30, "☠️");
            AddDefault("achv_shatter", "Sub-Zero Shatter", "Trigger 20 Shatter vulnerability bursts.", AchievementCategory.Synergies, 20, 300, 30, "❄️");
            AddDefault("achv_synergy_50", "Elemental Maestro", "Trigger 50 total Elemental Synergies.", AchievementCategory.Synergies, 50, 600, 50, "✨");

            // Progression Achievements
            AddDefault("achv_flawless_keep", "Iron Fortress", "Complete a battle without taking any Castle damage.", AchievementCategory.Progression, 1, 400, 35, "🏰");
            AddDefault("achv_million_damage", "Cataclysmic Force", "Deal over 50,000 total damage across all defenders.", AchievementCategory.Progression, 50000, 500, 40, "💥");
            AddDefault("achv_relic_collector", "Relic Hoarder", "Collect 3 Legendary Relics in a single run.", AchievementCategory.Progression, 3, 400, 30, "💍");
            AddDefault("achv_codex_discoverer", "Grand Archivist", "Discover and record 5 distinct enemies in the Codex.", AchievementCategory.Progression, 5, 300, 25, "📚");

            // Mastery (IDs kept so existing progress is not reset)
            AddDefault("achv_heat_1", "Hardened Commander", "Clear any stage on Hard.", AchievementCategory.Mastery, 1, 350, 30, "⚔");
            AddDefault("achv_heat_3", "Infernal Trial", "Clear any stage on Hard with a strong keep.", AchievementCategory.Mastery, 3, 700, 60, "🌋");
            AddDefault("achv_heat_5", "Abyssal Crucible", "Clear any stage on Hard.", AchievementCategory.Mastery, 5, 1200, 100, "👹");

            // Endless Mode
            AddDefault("achv_abyss_5", "Into the Void", "Survive until Abyssal Wave 5 in Endless Survival.", AchievementCategory.Endless, 5, 500, 40, "🌌");
            AddDefault("achv_abyss_10", "Abyssal Conqueror", "Survive until Abyssal Wave 10 in Endless Survival.", AchievementCategory.Endless, 10, 1000, 80, "🪐");
        }

        private void AddDefault(string id, string title, string desc, AchievementCategory cat, float target, int gold, int mats, string badge)
        {
            var achv = ScriptableObject.CreateInstance<AchievementDefinition>();
            achv.id = id;
            achv.title = title;
            achv.description = desc;
            achv.category = cat;
            achv.targetValue = target;
            achv.rewardGold = gold;
            achv.rewardMaterials = mats;
            achv.iconBadge = badge;
            AddAchievement(achv);
        }
    }
}
