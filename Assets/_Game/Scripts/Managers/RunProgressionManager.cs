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
        [SerializeField] private float runRangeMultiplier = 1.0f;
        [SerializeField] private float runXpMultiplier = 1.0f;

        public int CurrentXp => currentXp;
        public int CurrentLevel => currentLevel;
        public float RunDamageMultiplier => runDamageMultiplier;
        public float RunFireRateMultiplier => runFireRateMultiplier;
        public float RunRangeMultiplier => runRangeMultiplier;
        public float RunXpMultiplier => runXpMultiplier;

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

            int finalAmount = Mathf.RoundToInt(amount * runXpMultiplier);
            currentXp += finalAmount;
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
            var pool = new System.Collections.Generic.List<CardChoice>();

            // Always eligible: Damage Boost
            pool.Add(new CardChoice(
                "Sharp Training",
                "Boosts all defenders' damage by 10% for the rest of this run.",
                () => { runDamageMultiplier += 0.10f; }
            ));

            // Always eligible: Attack Speed Boost
            pool.Add(new CardChoice(
                "Rapid Reload",
                "Boosts all defenders' fire rate by 10% for the rest of this run.",
                () => { runFireRateMultiplier += 0.10f; }
            ));

            // Always eligible: Range Boost
            pool.Add(new CardChoice(
                "Extended Sights",
                "Boosts all defenders' range by 10% for the rest of this run.",
                () => { runRangeMultiplier += 0.10f; }
            ));

            // Always eligible: XP Gain Boost
            pool.Add(new CardChoice(
                "Scholar's Path",
                "Boosts all run XP gained by 10% for the rest of this run.",
                () => { runXpMultiplier += 0.10f; }
            ));

            // Conditional eligible: Repair Wall (only when castle is damaged)
            var castle = FindFirstObjectByType<Castle>();
            if (castle != null && castle.CurrentHealth < castle.MaxHealth)
            {
                pool.Add(new CardChoice(
                    "Fortify Walls",
                    "Repairs the Castle Wall by 20% of its max health.",
                    () => {
                        castle.Repair(Mathf.RoundToInt(castle.MaxHealth * 0.20f));
                    }
                ));
            }

            // We must pick exactly 3 unique cards randomly
            var chosen = new CardChoice[3];
            var rng = new System.Random();

            for (int i = 0; i < 3; i++)
            {
                if (pool.Count == 0)
                {
                    // Fallback choice if pool is depleted
                    chosen[i] = new CardChoice(
                        "Minor Blessing",
                        "Gives 15 gold instantly to the treasury.",
                        () => {
                            if (EconomyManager.Instance != null)
                            {
                                EconomyManager.Instance.AddGold(15);
                            }
                        }
                    );
                    continue;
                }

                int idx = rng.Next(pool.Count);
                chosen[i] = pool[idx];
                pool.RemoveAt(idx); // prevent duplicate selections!
            }

            return chosen;
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
