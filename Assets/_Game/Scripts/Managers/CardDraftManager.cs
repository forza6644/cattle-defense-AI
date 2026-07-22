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

        [SerializeField] private CardPoolDefinition poolOverride;

        private readonly List<CardPoolEntry> cardPool = new List<CardPoolEntry>();
        private readonly HashSet<string> displayedCardIds = new HashSet<string>(System.StringComparer.Ordinal);
        private System.Random draftRandom = new System.Random();
        private bool isDraftActive = false;
        private bool isSelectionMade = false;

        public bool IsDraftActive => isDraftActive;
        public CardPoolDefinition PoolOverride => poolOverride;

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
            cardPool.Clear();
            if (poolOverride != null)
            {
                if (GameplayDataValidation.HasErrors(GameplayDataValidation.ValidateCardPool(poolOverride)))
                {
                    Debug.LogError($"[CardDraftManager] Pool override '{poolOverride.name}' is invalid. Draft disabled until corrected.", poolOverride);
                    return;
                }

                cardPool.AddRange(poolOverride.cards);
                Debug.Log($"[CardDraftManager] Loaded override pool '{poolOverride.stableId}' with {cardPool.Count} cards.");
                return;
            }

            CardDefinition[] loadedCards = Resources.LoadAll<CardDefinition>("Cards");
            if (loadedCards != null && loadedCards.Length > 0)
            {
                for (int i = 0; i < loadedCards.Length; i++)
                {
                    cardPool.Add(new CardPoolEntry
                    {
                        card = loadedCards[i],
                        rarity = loadedCards[i].rarity,
                        weight = loadedCards[i].weight
                    });
                }
                Debug.Log($"[CardDraftManager] Successfully loaded {cardPool.Count} cards from Resources/Cards.");
            }
            else
            {
                Debug.LogWarning("[CardDraftManager] No CardDefinitions found in Resources/Cards.");
            }
        }

        public void ResetForRun()
        {
            StopAllCoroutines();
            isDraftActive = false;
            isSelectionMade = false;
            displayedCardIds.Clear();
            draftRandom = new System.Random();
            LoadCardPool();
        }

        public void SetPoolOverrideForQualification(CardPoolDefinition definition)
        {
            if (isDraftActive)
            {
                Debug.LogWarning("[CardDraftManager] Cannot change card pool during an active draft.");
                return;
            }
            poolOverride = definition;
            LoadCardPool();
        }


        /// <summary>
        /// Suspends wave progression, shows the draft UI, and blocks until a card is selected.
        /// </summary>
        public IEnumerator StartDraftCoroutine(string title = "CARD DRAFT", string subtitle = "Choose a card to upgrade your run")
        {
            if (isDraftActive)
            {
                yield break;
            }

            List<DraftCardChoice> choices = GenerateDraftChoices();
            if (choices.Count == 0)
            {
                Debug.LogWarning("[CardDraftManager] Cannot start draft: no valid cards for the current roster.");
                yield break;
            }

            isDraftActive = true;
            isSelectionMade = false;
            RememberDisplayedChoices(choices);

            // Map CardDefinition to RunProgressionManager.CardChoice
            RunProgressionManager.CardChoice[] choiceStructs = new RunProgressionManager.CardChoice[3];
            for (int i = 0; i < 3; i++)
            {
                if (i < choices.Count)
                {
                    choiceStructs[i] = CreateChoice(choices[i]);
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
                UIManager.Instance.SetLevelUpPanelTitle(title, subtitle);
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

        private RunProgressionManager.CardChoice CreateChoice(DraftCardChoice choice)
        {
            CardDefinition selectedCard = choice.Card;
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
                choice.Rarity.ToString()
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

            if (card.cardCategory == CardCategory.Trap)
            {
                if (TrapRuntimeManager.Instance != null
                    && TrapRuntimeManager.Instance.TryDeploy(card.trapDefinition, out _))
                {
                    RunModifierManager.Instance?.TryAddCard(card);
                }
                return;
            }

            if (card.cardCategory == CardCategory.BattlefieldDefense)
            {
                if (BattlefieldDefenseManager.Instance != null
                    && BattlefieldDefenseManager.Instance.TryDeploy(card.battlefieldDefenseDefinition, out _))
                {
                    RunModifierManager.Instance?.TryAddCard(card);
                }
                return;
            }

            if (RunModifierManager.Instance != null)
            {
                RunModifierManager.Instance.TryAddCard(card);
            }
        }

        public List<DraftCardChoice> GenerateDraftChoices(int? deterministicSeed = null)
        {
            DraftSelectionState state = BuildSelectionState();
            RecruitOptionPolicy policy = poolOverride != null
                ? poolOverride.recruitOptionPolicy
                : RecruitOptionPolicy.Weighted;
            int? seed = deterministicSeed ?? draftRandom.Next();
            return CardDraftSelector.Generate(cardPool, state, 3, policy, seed);
        }

        public const int RerollCost = 20;

        public bool CanReroll()
        {
            if (!isDraftActive || isSelectionMade) return false;
            EconomyManager economy = EconomyManager.Instance;
            return economy != null && economy.Gold >= RerollCost;
        }

        public bool TryReroll()
        {
            if (!CanReroll()) return false;

            if (!TryGenerateDifferentReroll(out List<DraftCardChoice> choices))
            {
                const string message = "No alternative card set is available.";
                UIManager.Instance?.ShowDraftFeedback(message);
                Debug.LogWarning("[CardDraftManager] Reroll failed: " + message);
                return false;
            }

            if (EconomyManager.Instance != null && EconomyManager.Instance.Gold >= RerollCost)
            {
                if (!EconomyManager.Instance.TrySpend(RerollCost))
                {
                    return false;
                }
            }

            RunProgressionManager.CardChoice[] choiceStructs = new RunProgressionManager.CardChoice[3];
            for (int i = 0; i < 3; i++)
            {
                choiceStructs[i] = i < choices.Count ? CreateChoice(choices[i]) : CreateFallbackChoice();
            }

            RememberDisplayedChoices(choices);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDraftFeedback("Choose a card to upgrade your run");
                UIManager.Instance.OnShowLevelUpDraft(choiceStructs);
            }
            Debug.Log("[CardDraftManager] Reroll executed successfully.");
            return true;
        }

        private bool TryGenerateDifferentReroll(out List<DraftCardChoice> choices)
        {
            const int randomAttempts = 12;
            for (int attempt = 0; attempt < randomAttempts; attempt++)
            {
                choices = GenerateDraftChoices();
                if (choices.Count > 0 && IsDifferentFromDisplayed(choices))
                {
                    return true;
                }
            }

            DraftSelectionState state = BuildSelectionState();
            var eligible = new List<DraftCardChoice>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < cardPool.Count; i++)
            {
                CardPoolEntry entry = cardPool[i];
                CardDefinition card = entry?.card;
                if (card == null
                    || !CardDraftSelector.IsEligible(card, state)
                    || entry.weight <= 0f
                    || float.IsNaN(entry.weight)
                    || float.IsInfinity(entry.weight)
                    || !seen.Add(card.id))
                {
                    continue;
                }

                eligible.Add(new DraftCardChoice(card, entry.rarity, entry.weight));
            }

            DraftCardChoice alternative = default;
            bool foundAlternative = false;
            for (int i = 0; i < eligible.Count; i++)
            {
                if (!displayedCardIds.Contains(eligible[i].Card.id))
                {
                    alternative = eligible[i];
                    foundAlternative = true;
                    break;
                }
            }

            choices = GenerateDraftChoices();
            if (!foundAlternative || choices.Count == 0)
            {
                return false;
            }

            int replaceIndex = choices.Count - 1;
            int recruitCount = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Card.cardCategory == CardCategory.RecruitHero)
                {
                    recruitCount++;
                }
            }
            if (recruitCount == 1
                && choices[replaceIndex].Card.cardCategory == CardCategory.RecruitHero
                && alternative.Card.cardCategory != CardCategory.RecruitHero)
            {
                for (int i = choices.Count - 1; i >= 0; i--)
                {
                    if (choices[i].Card.cardCategory != CardCategory.RecruitHero)
                    {
                        replaceIndex = i;
                        break;
                    }
                }
            }

            choices[replaceIndex] = alternative;
            return IsDifferentFromDisplayed(choices);
        }

        private bool IsDifferentFromDisplayed(List<DraftCardChoice> choices)
        {
            if (displayedCardIds.Count != choices.Count)
            {
                return true;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Card == null || !displayedCardIds.Contains(choices[i].Card.id))
                {
                    return true;
                }
            }
            return false;
        }

        private void RememberDisplayedChoices(List<DraftCardChoice> choices)
        {
            displayedCardIds.Clear();
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Card != null)
                {
                    displayedCardIds.Add(choices[i].Card.id);
                }
            }
        }

        private DraftSelectionState BuildSelectionState()
        {
            HeroRosterManager roster = HeroRosterManager.Instance;
            var heroIds = new List<string>();
            var attackTypes = new List<AttackType>();
            int openSlots = 0;
            if (roster != null)
            {
                heroIds.AddRange(roster.OwnedHeroIds);
                roster.CopyOwnedAttackTypesTo(attackTypes);
                openSlots = roster.CachedEmptySlotCount;
            }

            var stacks = new Dictionary<string, int>(System.StringComparer.Ordinal);
            RunModifierManager modifiers = RunModifierManager.Instance;
            if (modifiers != null)
            {
                for (int i = 0; i < cardPool.Count; i++)
                {
                    CardDefinition card = cardPool[i]?.card;
                    if (card != null && card.cardCategory == CardCategory.HeroUpgrade)
                    {
                        stacks[card.id] = modifiers.GetCardStackCount(card.id);
                    }
                }
            }
            bool trapAvailable = BattlefieldAnchorManager.Instance != null
                && BattlefieldAnchorManager.Instance.HasAvailableAnchor(BattlefieldAnchorType.Trap);
            bool defenseAvailable = BattlefieldAnchorManager.Instance != null
                && BattlefieldAnchorManager.Instance.HasAvailableAnchor(BattlefieldAnchorType.Defense);
            return new DraftSelectionState(heroIds, attackTypes, openSlots, stacks, trapAvailable, defenseAvailable);
        }
    }
}
