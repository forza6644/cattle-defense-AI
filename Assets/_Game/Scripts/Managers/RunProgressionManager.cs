using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tracks run-wide XP, levels, and applies active boost card multipliers.
    /// Listens to enemy deaths to award XP.
    /// </summary>
    public class RunProgressionManager : MonoBehaviour
    {
        public static RunProgressionManager Instance { get; private set; }

        public struct CardChoice
        {
            public string title;
            public string description;
            public Action action;

            public CardChoice(string title, string description, Action action)
            {
                this.title = title;
                this.description = description;
                this.action = action;
            }
        }

        [Header("State")]
        [SerializeField] private int currentXp = 0;
        [SerializeField] private int currentLevel = 1;

        [Header("Active Multipliers")]
        [SerializeField] private float runDamageMultiplier = 1.0f;
        [SerializeField] private float runFireRateMultiplier = 1.0f;

        public int CurrentXp => currentXp;
        public int CurrentLevel => currentLevel;
        public float RunDamageMultiplier => runDamageMultiplier;
        public float RunFireRateMultiplier => runFireRateMultiplier;

        /// <summary>Fired when XP or level changes: (currentXp, xpNeeded, currentLevel).</summary>
        public event Action<int, int, int> XpChanged;

        /// <summary>Fired when a level-up occurs to show the draft UI: (choices).</summary>
        public event Action<CardChoice[]> ShowLevelUpDraft;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            Enemy.AnyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            Enemy.AnyKilled -= OnEnemyKilled;
        }

        private void Start()
        {
            // Initial UI update
            XpChanged?.Invoke(currentXp, GetXpNeededForNextLevel(), currentLevel);
        }

        private void OnEnemyKilled(Enemy enemy, int goldAwarded)
        {
            if (enemy == null || enemy.Data == null)
            {
                return;
            }

            int xp = enemy.Data.xpValue;
            if (xp <= 0)
            {
                // Fallback: use gold reward or 1
                xp = enemy.Data.goldReward > 0 ? enemy.Data.goldReward : 1;
            }

            AddXp(xp);
        }

        public int GetXpNeededForNextLevel()
        {
            // Level 1 -> 2: 100 XP
            // Level 2 -> 3: 150 XP
            // Level 3 -> 4: 200 XP
            // etc.
            return 100 + (currentLevel - 1) * 50;
        }

        public void AddXp(int amount)
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            currentXp += amount;
            int needed = GetXpNeededForNextLevel();
            bool levelUpTriggered = false;

            while (currentXp >= needed)
            {
                currentXp -= needed;
                currentLevel++;
                levelUpTriggered = true;
                needed = GetXpNeededForNextLevel();
            }

            XpChanged?.Invoke(currentXp, needed, currentLevel);

            if (levelUpTriggered)
            {
                TriggerLevelUpDraft();
            }
        }

        private void TriggerLevelUpDraft()
        {
            // Pause the game state safely
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelUp);
            }

            // Generate card choices
            CardChoice[] choices = GenerateChoices();

            // Trigger event for UI
            ShowLevelUpDraft?.Invoke(choices);
        }

        private CardChoice[] GenerateChoices()
        {
            CardChoice[] choices = new CardChoice[3];

            // Card 1: Damage Boost
            choices[0] = new CardChoice(
                "Sharp Training",
                "Boosts all defenders' damage by 10% for the rest of this run.",
                () => { runDamageMultiplier += 0.10f; }
            );

            // Card 2: Attack Speed Boost
            choices[1] = new CardChoice(
                "Rapid Reload",
                "Boosts all defenders' fire rate by 10% for the rest of this run.",
                () => { runFireRateMultiplier += 0.10f; }
            );

            // Card 3: Repair Wall
            choices[2] = new CardChoice(
                "Fortify Walls",
                "Repairs the Castle Wall by 20% of its max health.",
                () => {
                    var castle = FindFirstObjectByType<Castle>();
                    if (castle != null)
                    {
                        castle.Repair(Mathf.RoundToInt(castle.MaxHealth * 0.20f));
                    }
                }
            );

            return choices;
        }

        public void ApplyChoice(CardChoice choice)
        {
            choice.action?.Invoke();
            Debug.Log($"Applied blessing card: {choice.title}");

            // Resume gameplay
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }
    }
}
