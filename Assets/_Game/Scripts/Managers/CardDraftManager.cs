using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Stonehold
{
    /// <summary>
    /// Handles loading cards, picking 3 random choices, pausing the game, and showing the draft UI panel.
    /// Supports a manual debug trigger (C key) and integrations between waves.
    /// </summary>
    public class CardDraftManager : MonoBehaviour
    {
        public static CardDraftManager Instance { get; private set; }

        private List<CardDefinition> cardPool = new List<CardDefinition>();
        private bool isDraftActive = false;
        private bool isSelectionMade = false;

        public bool IsDraftActive => isDraftActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCardPool();
        }

        private void Update()
        {
            // Debug trigger: Press C to start a card draft
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                if (!isDraftActive && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                {
                    Debug.Log("[CardDraftManager] Manual draft triggered via C key.");
                    StartCoroutine(StartDraftCoroutine());
                }
            }
        }

        private void LoadCardPool()
        {
            CardDefinition[] loadedCards = Resources.LoadAll<CardDefinition>("Cards");
            cardPool.Clear();
            if (loadedCards != null && loadedCards.Length > 0)
            {
                cardPool.AddRange(loadedCards);
                Debug.Log($"[CardDraftManager] Successfully loaded {cardPool.Count} cards from Resources/Cards.");
            }
            else
            {
                Debug.LogWarning("[CardDraftManager] No CardDefinitions found in Resources/Cards.");
            }
        }

        /// <summary>
        /// Suspends wave progression, shows the draft UI, and blocks until a card is selected.
        /// </summary>
        public IEnumerator StartDraftCoroutine()
        {
            if (isDraftActive)
            {
                yield break;
            }

            // Reload card pool to make sure any dynamically created/edited cards are captured
            LoadCardPool();

            if (cardPool.Count < 3)
            {
                Debug.LogError("[CardDraftManager] Cannot start draft: Card pool has fewer than 3 cards.");
                yield break;
            }

            isDraftActive = true;
            isSelectionMade = false;

            // Pick 3 random cards based on weights
            List<CardDefinition> choices = PickRandomCards(3);

            // Map CardDefinition to RunProgressionManager.CardChoice
            RunProgressionManager.CardChoice[] choiceStructs = new RunProgressionManager.CardChoice[3];
            for (int i = 0; i < 3; i++)
            {
                var card = choices[i];
                choiceStructs[i] = new RunProgressionManager.CardChoice(
                    card.displayName,
                    card.description,
                    () =>
                    {
                        if (RunModifierManager.Instance != null)
                        {
                            RunModifierManager.Instance.AddCard(card);
                        }
                        isSelectionMade = true;
                    },
                    "Card", // Sets the badge label type
                    card.rarity.ToString()
                );
            }

            // Pause combat state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelUp);
            }

            // Update LevelUp Panel title & subtitle in UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetLevelUpPanelTitle("CARD DRAFT", "Choose a card to upgrade your run");
                UIManager.Instance.OnShowLevelUpDraft(choiceStructs);
            }

            // Block until a card is selected
            while (!isSelectionMade)
            {
                yield return null;
            }

            isDraftActive = false;
        }

        private List<CardDefinition> PickRandomCards(int count)
        {
            List<CardDefinition> pool = new List<CardDefinition>(cardPool);
            List<CardDefinition> selected = new List<CardDefinition>();
            System.Random rng = new System.Random();

            for (int c = 0; c < count; c++)
            {
                if (pool.Count == 0) break;

                // Calculate cumulative weight
                float totalWeight = 0f;
                foreach (var card in pool)
                {
                    totalWeight += card.weight;
                }

                if (totalWeight <= 0f)
                {
                    // Fallback to direct random index
                    int idx = rng.Next(pool.Count);
                    selected.Add(pool[idx]);
                    pool.RemoveAt(idx);
                    continue;
                }

                double randVal = rng.NextDouble() * totalWeight;
                float runningSum = 0f;
                int selectedIndex = 0;

                for (int i = 0; i < pool.Count; i++)
                {
                    runningSum += pool[i].weight;
                    if (randVal <= runningSum)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                selected.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }

            return selected;
        }
    }
}
