using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum QuestType
    {
        Daily,
        Weekly
    }

    public enum QuestObjectiveType
    {
        DefeatEnemies,
        TriggerReactions,
        ClearWaves,
        SurviveWithoutDamage,
        CollectGold,
        CastAbilities
    }

    [Serializable]
    public class QuestData
    {
        public string questId;
        public string title;
        public string description;
        public QuestType questType;
        public QuestObjectiveType objectiveType;
        public int targetAmount;
        public int currentAmount;
        public int goldReward;
        public int materialsReward;
        public bool isClaimed;

        public bool IsCompleted => currentAmount >= targetAmount;
        public float Progress01 => targetAmount > 0 ? Mathf.Clamp01((float)currentAmount / targetAmount) : 0f;
    }

    /// <summary>
    /// Manages daily (24h) and weekly (7d) repeatable quests, event-driven progression,
    /// reward claiming, and PlayerPrefs persistence.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        private const string KeyDailyTimestamp = "quest_daily_reset_timestamp";
        private const string KeyWeeklyTimestamp = "quest_weekly_reset_timestamp";
        private const string KeyQuestsJson = "quest_active_data_json";

        public event Action OnQuestsUpdated;

        [SerializeField] private List<QuestData> activeQuests = new List<QuestData>();

        public IReadOnlyList<QuestData> ActiveQuests
        {
            get
            {
                EnsureQuestsLoaded();
                return activeQuests;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
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
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadQuests();
            CheckAndRefreshQuests();

            Enemy.AnyKilled += OnEnemyKilled;
            StatusEffectController.OnElementalReaction += OnElementalReaction;
        }

        private void EnsureQuestsLoaded()
        {
            if (activeQuests == null || activeQuests.Count == 0)
            {
                LoadQuests();
                CheckAndRefreshQuests();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Enemy.AnyKilled -= OnEnemyKilled;
                StatusEffectController.OnElementalReaction -= OnElementalReaction;
                Instance = null;
            }
        }

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
            IncrementObjective(QuestObjectiveType.DefeatEnemies, 1);
            if (gold > 0)
            {
                IncrementObjective(QuestObjectiveType.CollectGold, gold);
            }
        }

        private void OnElementalReaction(ElementalReactionType reaction, Vector3 worldPos, string heroId)
        {
            IncrementObjective(QuestObjectiveType.TriggerReactions, 1);
        }

        public void ReportWaveCleared()
        {
            IncrementObjective(QuestObjectiveType.ClearWaves, 1);
        }

        public void ReportGoldCollected(int amount)
        {
            if (amount > 0)
            {
                IncrementObjective(QuestObjectiveType.CollectGold, amount);
            }
        }

        public void ReportAbilityCast()
        {
            IncrementObjective(QuestObjectiveType.CastAbilities, 1);
        }

        public void ReportFlawlessRun()
        {
            IncrementObjective(QuestObjectiveType.SurviveWithoutDamage, 1);
        }

        public void IncrementObjective(QuestObjectiveType objectiveType, int amount)
        {
            EnsureQuestsLoaded();
            bool changed = false;
            foreach (var q in activeQuests)
            {
                if (q.objectiveType == objectiveType && !q.IsCompleted)
                {
                    q.currentAmount = Mathf.Min(q.currentAmount + amount, q.targetAmount);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        public bool ClaimQuestReward(string questId, out int goldReward, out int materialsReward)
        {
            EnsureQuestsLoaded();
            goldReward = 0;
            materialsReward = 0;

            var quest = activeQuests.Find(q => q.questId == questId);
            if (quest == null || !quest.IsCompleted || quest.isClaimed)
            {
                return false;
            }

            quest.isClaimed = true;
            goldReward = quest.goldReward;
            materialsReward = quest.materialsReward;

            SaveManager.AddMetaGold(goldReward);
            SaveManager.AddCoreMaterials(materialsReward);
            SaveQuests();

            HapticFeedbackManager.TriggerMedium();
            OnQuestsUpdated?.Invoke();
            return true;
        }

        public bool HasUnclaimedQuests()
        {
            EnsureQuestsLoaded();
            return activeQuests.Exists(q => q.IsCompleted && !q.isClaimed);
        }

        public void CheckAndRefreshQuests()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long lastDaily = GetStoredTimestamp(KeyDailyTimestamp);
            long lastWeekly = GetStoredTimestamp(KeyWeeklyTimestamp);

            bool needsSave = false;

            // Daily reset: 86,400s (24h)
            if (now - lastDaily >= 86400 || activeQuests.FindAll(q => q.questType == QuestType.Daily).Count == 0)
            {
                activeQuests.RemoveAll(q => q.questType == QuestType.Daily);
                activeQuests.AddRange(GenerateDailyQuests());
                PlayerPrefs.SetString(KeyDailyTimestamp, now.ToString());
                needsSave = true;
            }

            // Weekly reset: 604,800s (7 days)
            if (now - lastWeekly >= 604800 || activeQuests.FindAll(q => q.questType == QuestType.Weekly).Count == 0)
            {
                activeQuests.RemoveAll(q => q.questType == QuestType.Weekly);
                activeQuests.AddRange(GenerateWeeklyQuests());
                PlayerPrefs.SetString(KeyWeeklyTimestamp, now.ToString());
                needsSave = true;
            }

            if (needsSave)
            {
                SaveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        private List<QuestData> GenerateDailyQuests()
        {
            return new List<QuestData>
            {
                new QuestData
                {
                    questId = "daily_kill_" + DateTime.UtcNow.ToString("yyyyMMdd"),
                    title = "Vanguard Cleanser",
                    description = "Defeat 40 hostile invaders in battle.",
                    questType = QuestType.Daily,
                    objectiveType = QuestObjectiveType.DefeatEnemies,
                    targetAmount = 40,
                    goldReward = 180,
                    materialsReward = 10
                },
                new QuestData
                {
                    questId = "daily_react_" + DateTime.UtcNow.ToString("yyyyMMdd"),
                    title = "Elemental Conductor",
                    description = "Trigger 12 Elemental Reactions.",
                    questType = QuestType.Daily,
                    objectiveType = QuestObjectiveType.TriggerReactions,
                    targetAmount = 12,
                    goldReward = 220,
                    materialsReward = 15
                },
                new QuestData
                {
                    questId = "daily_wave_" + DateTime.UtcNow.ToString("yyyyMMdd"),
                    title = "Line Defender",
                    description = "Successfully clear 8 combat waves.",
                    questType = QuestType.Daily,
                    objectiveType = QuestObjectiveType.ClearWaves,
                    targetAmount = 8,
                    goldReward = 200,
                    materialsReward = 12
                }
            };
        }

        private List<QuestData> GenerateWeeklyQuests()
        {
            return new List<QuestData>
            {
                new QuestData
                {
                    questId = "weekly_kill_" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 604800),
                    title = "Grand Exterminator",
                    description = "Defeat 250 hostile monsters across all campaigns.",
                    questType = QuestType.Weekly,
                    objectiveType = QuestObjectiveType.DefeatEnemies,
                    targetAmount = 250,
                    goldReward = 900,
                    materialsReward = 60
                },
                new QuestData
                {
                    questId = "weekly_react_" + (DateTimeOffset.UtcNow.ToString()),
                    title = "Master of Elements",
                    description = "Trigger 60 Elemental Reactions.",
                    questType = QuestType.Weekly,
                    objectiveType = QuestObjectiveType.TriggerReactions,
                    targetAmount = 60,
                    goldReward = 1100,
                    materialsReward = 80
                },
                new QuestData
                {
                    questId = "weekly_gold_" + (DateTimeOffset.UtcNow.ToString()),
                    title = "War Chest Accumulator",
                    description = "Amass 1,500 Gold in combat victories.",
                    questType = QuestType.Weekly,
                    objectiveType = QuestObjectiveType.CollectGold,
                    targetAmount = 1500,
                    goldReward = 850,
                    materialsReward = 50
                }
            };
        }

        private long GetStoredTimestamp(string key)
        {
            string s = PlayerPrefs.GetString(key, "0");
            return long.TryParse(s, out long val) ? val : 0;
        }

        [Serializable]
        private class QuestListWrapper
        {
            public List<QuestData> list;
        }

        private void SaveQuests()
        {
            var wrapper = new QuestListWrapper { list = activeQuests };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(KeyQuestsJson, json);
            PlayerPrefs.Save();
        }

        private void LoadQuests()
        {
            string json = PlayerPrefs.GetString(KeyQuestsJson, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<QuestListWrapper>(json);
                    if (wrapper != null && wrapper.list != null)
                    {
                        activeQuests = wrapper.list;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[QuestManager] Failed to deserialize quests: {e.Message}");
                }
            }
        }

        public static void ResetForTesting()
        {
            Instance = null;
            PlayerPrefs.DeleteKey(KeyDailyTimestamp);
            PlayerPrefs.DeleteKey(KeyWeeklyTimestamp);
            PlayerPrefs.DeleteKey(KeyQuestsJson);
        }
    }
}
