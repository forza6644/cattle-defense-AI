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
            public string cardType;
            public string rarity;

            public CardChoice(string title, string description, Action action, string cardType = "Boost", string rarity = "Common")
            {
                this.title = title;
                this.description = description;
                this.action = action;
                this.cardType = cardType;
                this.rarity = rarity;
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
            switch (currentLevel)
            {
                case 1: return 25;
                case 2: return 60;
                case 3: return 120;
                case 4: return 250;
                case 5: return 400;
                case 6: return 600;
                case 7: return 900;
                default: return 900 + (currentLevel - 7) * 400;
            }
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
            var chosenList = new System.Collections.Generic.List<CardChoice>();
            var rng = new System.Random();

            // Determine active/placed defender count
            var slots = UnityEngine.Object.FindObjectsByType<TowerSlot>();
            int emptySlotsCount = 0;
            foreach (var s in slots)
            {
                if (!s.IsOccupied) emptySlotsCount++;
            }
            int placedCount = slots.Length - emptySlotsCount;

            // Compile candidate lists
            var addCandidates = GetAddCandidates();
            var upgradeCandidates = GetUpgradeCandidates();
            var boostCandidates = GetBoostCandidates();

            for (int i = 0; i < 3; i++)
            {
                bool hasAdd = addCandidates.Count > 0;
                bool hasUpgrade = upgradeCandidates.Count > 0;
                bool hasBoost = boostCandidates.Count > 0;

                // Force at least one Add Defender card if player has <= 2 placed defenders and empty slots exist
                bool forceAdd = (i == 0) && (placedCount <= 2) && hasAdd;

                if (forceAdd)
                {
                    int idx = rng.Next(addCandidates.Count);
                    chosenList.Add(addCandidates[idx]);
                    addCandidates.RemoveAt(idx);
                    continue;
                }

                // Determine weights
                float addWeight = 0f;
                float upgradeWeight = 0f;
                float boostWeight = 0f;

                if (hasAdd)
                {
                    addWeight = 0.7f;
                    upgradeWeight = hasUpgrade ? 0.15f : 0f;
                    boostWeight = hasBoost ? 0.15f : 0f;
                }
                else
                {
                    upgradeWeight = hasUpgrade ? 0.5f : 0f;
                    boostWeight = hasBoost ? 0.5f : 0f;
                }

                float totalWeight = addWeight + upgradeWeight + boostWeight;
                if (totalWeight <= 0f)
                {
                    // Fallback choice if all pools are empty
                    chosenList.Add(new CardChoice(
                        "Minor Blessing",
                        "Gives 15 gold instantly to the treasury.",
                        () => {
                            if (EconomyManager.Instance != null)
                            {
                                EconomyManager.Instance.AddGold(15);
                            }
                        }
                    ));
                    continue;
                }

                double randVal = rng.NextDouble() * totalWeight;
                if (hasAdd && randVal < addWeight)
                {
                    int idx = rng.Next(addCandidates.Count);
                    chosenList.Add(addCandidates[idx]);
                    addCandidates.RemoveAt(idx);
                }
                else if (hasUpgrade && randVal < (addWeight + upgradeWeight))
                {
                    int idx = rng.Next(upgradeCandidates.Count);
                    chosenList.Add(upgradeCandidates[idx]);
                    upgradeCandidates.RemoveAt(idx);
                }
                else if (hasBoost)
                {
                    int idx = rng.Next(boostCandidates.Count);
                    chosenList.Add(boostCandidates[idx]);
                    boostCandidates.RemoveAt(idx);
                }
                else
                {
                    // Fallback
                    chosenList.Add(new CardChoice(
                        "Minor Blessing",
                        "Gives 15 gold instantly to the treasury.",
                        () => {
                            if (EconomyManager.Instance != null)
                            {
                                EconomyManager.Instance.AddGold(15);
                            }
                        }
                    ));
                }
            }

            return chosenList.ToArray();
        }

        private System.Collections.Generic.List<CardChoice> GetAddCandidates()
        {
            var candidates = new System.Collections.Generic.List<CardChoice>();

            // Find all empty slots
            var slots = UnityEngine.Object.FindObjectsByType<TowerSlot>();
            int emptySlotsCount = 0;
            foreach (var s in slots)
            {
                if (!s.IsOccupied) emptySlotsCount++;
            }

            if (emptySlotsCount == 0)
            {
                return candidates; // empty
            }

            // Find placed defender IDs (only those on wall slots)
            var activeTowers = UnityEngine.Object.FindObjectsByType<Tower>();
            var placedIds = new System.Collections.Generic.HashSet<string>();
            foreach (var t in activeTowers)
            {
                if (t.Slot != null && t.Data != null && !string.IsNullOrEmpty(t.Data.defenderId))
                {
                    placedIds.Add(t.Data.defenderId);
                }
            }

            // Find available towers from TowerManager
            var tm = UnityEngine.Object.FindAnyObjectByType<TowerManager>();
            if (tm != null && tm.AvailableTowers != null)
            {
                foreach (var towerData in tm.AvailableTowers)
                {
                    if (towerData != null && !string.IsNullOrEmpty(towerData.defenderId))
                    {
                        if (!placedIds.Contains(towerData.defenderId))
                        {
                            var dataRef = towerData; // local copy for closure
                            string displayName = string.IsNullOrEmpty(dataRef.displayNameOverride) ? dataRef.towerName : dataRef.displayNameOverride;
                            candidates.Add(new CardChoice(
                                $"Add {displayName}",
                                $"Places a new {displayName} onto the wall for free.",
                                () => {
                                    // Find first available slot
                                    var sortedSlots = UnityEngine.Object.FindObjectsByType<TowerSlot>();
                                    System.Array.Sort(sortedSlots, (a, b) => string.Compare(a.name, b.name));
                                    foreach (var s in sortedSlots)
                                     {
                                        if (!s.IsOccupied)
                                        {
                                            var towerManager = UnityEngine.Object.FindAnyObjectByType<TowerManager>();
                                            if (towerManager != null)
                                            {
                                                towerManager.PlaceTowerFree(s, dataRef);
                                            }
                                            break;
                                        }
                                    }
                                },
                                "Add",
                                dataRef.rarity.ToString()
                            ));
                        }
                    }
                }
            }

            return candidates;
        }

        private System.Collections.Generic.List<CardChoice> GetUpgradeCandidates()
        {
            var candidates = new System.Collections.Generic.List<CardChoice>();

            var activeTowers = UnityEngine.Object.FindObjectsByType<Tower>();
            foreach (var t in activeTowers)
            {
                if (t != null && t.Slot != null && t.Data != null && !t.IsMaxLevel)
                {
                    var towerRef = t; // local copy for closure
                    string displayName = string.IsNullOrEmpty(towerRef.Data.displayNameOverride) ? towerRef.Data.towerName : towerRef.Data.displayNameOverride;
                    candidates.Add(new CardChoice(
                        $"Upgrade {displayName}",
                        $"Upgrades the placed {displayName} to Level {towerRef.Level + 1} for free.",
                        () => {
                            if (towerRef != null)
                            {
                                towerRef.UpgradeFree();
                            }
                        },
                        "Upgrade",
                        towerRef.Data.rarity.ToString()
                    ));
                }
            }

            return candidates;
        }

        private System.Collections.Generic.List<CardChoice> GetBoostCandidates()
        {
            var candidates = new System.Collections.Generic.List<CardChoice>();

            candidates.Add(new CardChoice(
                "Sharp Training",
                "Boosts all defenders' damage by 10% for the rest of this run.",
                () => { runDamageMultiplier += 0.10f; },
                "Boost",
                "Rare"
            ));

            candidates.Add(new CardChoice(
                "Rapid Reload",
                "Boosts all defenders' fire rate by 10% for the rest of this run.",
                () => { runFireRateMultiplier += 0.10f; },
                "Boost",
                "Rare"
            ));

            candidates.Add(new CardChoice(
                "Extended Sights",
                "Boosts all defenders' range by 10% for the rest of this run.",
                () => { runRangeMultiplier += 0.10f; },
                "Boost",
                "Rare"
            ));

            candidates.Add(new CardChoice(
                "Scholar's Path",
                "Boosts all run XP gained by 10% for the rest of this run.",
                () => { runXpMultiplier += 0.10f; },
                "Boost",
                "Epic"
            ));

            var castle = FindFirstObjectByType<Castle>();
            if (castle != null && castle.CurrentHealth < castle.MaxHealth)
            {
                candidates.Add(new CardChoice(
                    "Fortify Walls",
                    "Repairs the Castle Wall by 20% of its max health.",
                    () => {
                        castle.Repair(Mathf.RoundToInt(castle.MaxHealth * 0.20f));
                    },
                    "Boost",
                    "Common"
                ));
            }

            return candidates;
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
