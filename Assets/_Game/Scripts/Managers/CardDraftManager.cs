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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug trigger: Press C to start a card draft
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                if (!isDraftActive && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                {
                    Debug.Log("[CardDraftManager] Manual draft triggered via C key.");
                    StartCoroutine(StartDraftCoroutine());
                }
            }
#endif
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

            List<CardDefinition> validCards = GetValidCardPool();
            if (validCards.Count == 0)
            {
                Debug.LogWarning("[CardDraftManager] Cannot start draft: no valid cards for the current roster.");
                yield break;
            }

            isDraftActive = true;
            isSelectionMade = false;

            // Pick 3 random cards based on weights
            List<CardDefinition> choices = PickRandomCards(validCards, 3);

            // Map CardDefinition to RunProgressionManager.CardChoice
            RunProgressionManager.CardChoice[] choiceStructs = new RunProgressionManager.CardChoice[3];
            for (int i = 0; i < 3; i++)
            {
                if (i < choices.Count)
                {
                    var card = choices[i];
                    choiceStructs[i] = CreateChoice(card);
                }
                else
                {
                    choiceStructs[i] = CreateFallbackChoice();
                }
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
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.LevelUp)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        private RunProgressionManager.CardChoice CreateChoice(CardDefinition card)
        {
            CardDefinition selectedCard = card;
            string cardType = selectedCard.cardCategory == CardCategory.RecruitHero ? "Add" : "Card";
            return new RunProgressionManager.CardChoice(
                selectedCard.displayName,
                selectedCard.description,
                () =>
                {
                    ApplyCard(selectedCard);
                    isSelectionMade = true;
                },
                cardType,
                selectedCard.rarity.ToString()
            );
        }

        private RunProgressionManager.CardChoice CreateFallbackChoice()
        {
            return new RunProgressionManager.CardChoice(
                "Hold Formation",
                "No new draft option is available right now.",
                () =>
                {
                    isSelectionMade = true;
                },
                "Card",
                CardRarity.Common.ToString()
            );
        }

        private void ApplyCard(CardDefinition card)
        {
            if (card == null)
            {
                return;
            }

            if (card.cardCategory == CardCategory.RecruitHero)
            {
                if (HeroRosterManager.Instance != null)
                {
                    HeroRosterManager.Instance.RecruitHero(card.recruitHeroId);
                }
                return;
            }

            if (RunModifierManager.Instance != null)
            {
                RunModifierManager.Instance.AddCard(card);
            }
        }

        private List<CardDefinition> GetValidCardPool()
        {
            List<CardDefinition> valid = new List<CardDefinition>();
            foreach (CardDefinition card in cardPool)
            {
                if (IsCardValid(card))
                {
                    valid.Add(card);
                }
            }

            return valid;
        }

        private bool IsCardValid(CardDefinition card)
        {
            if (card == null)
            {
                return false;
            }

            HeroRosterManager roster = HeroRosterManager.Instance;
            if (roster == null)
            {
                return card.cardCategory != CardCategory.RecruitHero;
            }

            if (!string.IsNullOrEmpty(card.requiredOwnedHeroId) && !roster.IsHeroOwned(card.requiredOwnedHeroId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(card.blockedIfOwnedHeroId) && roster.IsHeroOwned(card.blockedIfOwnedHeroId))
            {
                return false;
            }

            if (card.cardCategory == CardCategory.RecruitHero)
            {
                return roster.CanRecruit(card.recruitHeroId);
            }

            if (card.targetType == CardTargetType.HeroById && !string.IsNullOrEmpty(card.targetHeroId))
            {
                return roster.CanUpgrade(card.targetHeroId);
            }

            if (card.targetType == CardTargetType.AttackType)
            {
                return roster.HasOwnedHeroWithAttackType(card.targetAttackType);
            }

            return true;
        }

        private List<CardDefinition> PickRandomCards(List<CardDefinition> sourcePool, int count)
        {
            List<CardDefinition> pool = new List<CardDefinition>(sourcePool);
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

                CardDefinition selectedCard = pool[selectedIndex];
                selected.Add(selectedCard);
                if (selectedCard.cardCategory == CardCategory.RecruitHero && !string.IsNullOrEmpty(selectedCard.recruitHeroId))
                {
                    pool.RemoveAll(card => card != null
                        && card.cardCategory == CardCategory.RecruitHero
                        && card.recruitHeroId == selectedCard.recruitHeroId);
                    continue;
                }

                pool.RemoveAt(selectedIndex);
            }

            return selected;
        }
    }
}
