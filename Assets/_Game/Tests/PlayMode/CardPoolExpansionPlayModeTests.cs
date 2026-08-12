using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class CardPoolExpansionPlayModeTests
    {
        private GameObject container;
        private HeroRosterManager rosterManager;
        private RunModifierManager modifierManager;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 2.0f;
            container = new GameObject("CardPoolExpansionTests_Container");
            rosterManager = container.AddComponent<HeroRosterManager>();
            modifierManager = container.AddComponent<RunModifierManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
            if (container != null)
            {
                Object.DestroyImmediate(container);
            }
        }

        [UnityTest]
        public IEnumerator AllRecruitableHeroes_HaveValidRecruitmentCardsInResources()
        {
            yield return null;

            // Archer is the starting defender; the remaining 5 heroes are recruitable via Card Draft.
            string[] expectedRecruitHeroes = new[]
            {
                "bombardier", "frost_mage", "electric_engineer", "fire_mage", "sniper"
            };

            CardDefinition[] allCards = Resources.LoadAll<CardDefinition>("Cards");
            Assert.IsNotNull(allCards, "Resources/Cards folder must contain CardDefinitions.");
            Assert.GreaterOrEqual(allCards.Length, 39, "Resources/Cards should contain at least 39 card assets.");

            var recruitCards = allCards.Where(c => c.cardCategory == CardCategory.RecruitHero).ToList();

            foreach (string heroId in expectedRecruitHeroes)
            {
                var recruitCard = recruitCards.FirstOrDefault(c => c.recruitHeroId == heroId);
                Assert.IsNotNull(recruitCard, $"Recruitment card for hero class '{heroId}' must exist in Resources/Cards.");
                Assert.AreEqual(CardCategory.RecruitHero, recruitCard.cardCategory);
            }
        }

        [UnityTest]
        public IEnumerator CardDraftSelector_OffersRecruitCards_ForFireMageAndSniperWhenSlotsOpen()
        {
            yield return null;

            CardDefinition addFireMage = Resources.Load<CardDefinition>("Cards/AddFireMage");
            CardDefinition addSniper = Resources.Load<CardDefinition>("Cards/AddSniper");

            Assert.IsNotNull(addFireMage, "AddFireMage card asset must exist.");
            Assert.IsNotNull(addSniper, "AddSniper card asset must exist.");

            // Draft state with 3 open slots and only Archer owned
            DraftSelectionState stateWithSlots = new DraftSelectionState(
                activeHeroes: new[] { "archer" },
                attackTypes: new[] { AttackType.SingleTarget },
                openHeroSlots: 3
            );

            Assert.IsTrue(CardDraftSelector.IsEligible(addFireMage, stateWithSlots), "AddFireMage must be eligible when open slots exist.");
            Assert.IsTrue(CardDraftSelector.IsEligible(addSniper, stateWithSlots), "AddSniper must be eligible when open slots exist.");

            // Draft state with 0 open slots
            DraftSelectionState stateSlotsFull = new DraftSelectionState(
                activeHeroes: new[] { "archer" },
                attackTypes: new[] { AttackType.SingleTarget },
                openHeroSlots: 0
            );

            Assert.IsFalse(CardDraftSelector.IsEligible(addFireMage, stateSlotsFull), "AddFireMage must be ineligible when slots are full.");
            Assert.IsFalse(CardDraftSelector.IsEligible(addSniper, stateSlotsFull), "AddSniper must be ineligible when slots are full.");
        }

        [UnityTest]
        public IEnumerator CardIconSpriteGenerator_GeneratesHighContrastSprites_ForAllSixHeroes()
        {
            yield return null;

            string[] heroIds = new[] { "archer", "bombardier", "frost_mage", "electric_engineer", "fire_mage", "sniper" };

            foreach (string heroId in heroIds)
            {
                Sprite sprite = CardIconSpriteGenerator.GetSpriteForCard($"Test {heroId}", "Add", heroId);
                Assert.IsNotNull(sprite, $"Generated icon sprite for hero '{heroId}' must not be null.");
                Assert.AreEqual(128, sprite.texture.width, "Generated sprite texture width should be 128.");
                Assert.AreEqual(128, sprite.texture.height, "Generated sprite texture height should be 128.");
            }
        }

        [UnityTest]
        public IEnumerator RecruitingAllSixHeroes_ProgressivelyUnlocksHeroSpecificUpgrades()
        {
            yield return null;

            CardDefinition hotCoals = Resources.Load<CardDefinition>("Cards/HotCoals");
            CardDefinition deadeye = Resources.Load<CardDefinition>("Cards/Deadeye");

            Assert.IsNotNull(hotCoals, "HotCoals card asset must exist.");
            Assert.IsNotNull(deadeye, "Deadeye card asset must exist.");

            // Before recruiting Fire Mage or Sniper
            DraftSelectionState initial = new DraftSelectionState(
                activeHeroes: new[] { "archer" },
                attackTypes: new[] { AttackType.SingleTarget },
                openHeroSlots: 3
            );

            Assert.IsFalse(CardDraftSelector.IsEligible(hotCoals, initial), "Fire Mage upgrade HotCoals must be ineligible before Fire Mage is recruited.");
            Assert.IsFalse(CardDraftSelector.IsEligible(deadeye, initial), "Sniper upgrade Deadeye must be ineligible before Sniper is recruited.");

            // After recruiting Fire Mage and Sniper
            DraftSelectionState activeAll = new DraftSelectionState(
                activeHeroes: new[] { "archer", "fire_mage", "sniper" },
                attackTypes: new[] { AttackType.SingleTarget, AttackType.Splash, AttackType.DoT },
                openHeroSlots: 2
            );

            Assert.IsTrue(CardDraftSelector.IsEligible(hotCoals, activeAll), "HotCoals must become eligible after Fire Mage is active.");
            Assert.IsTrue(CardDraftSelector.IsEligible(deadeye, activeAll), "Deadeye must become eligible after Sniper is active.");
        }

        [UnityTest]
        public IEnumerator UpgradeScaling_AllSixHeroes_AppliesModifiersToRunModifierManager()
        {
            yield return null;

            CardDefinition warTraining = Resources.Load<CardDefinition>("Cards/WarTraining");
            CardDefinition hotCoals = Resources.Load<CardDefinition>("Cards/HotCoals");
            CardDefinition deadeye = Resources.Load<CardDefinition>("Cards/Deadeye");

            Assert.IsNotNull(warTraining, "WarTraining card must exist.");
            Assert.IsNotNull(hotCoals, "HotCoals card must exist.");
            Assert.IsNotNull(deadeye, "Deadeye card must exist.");

            modifierManager.TryAddCard(warTraining);
            modifierManager.TryAddCard(hotCoals);
            modifierManager.TryAddCard(deadeye);

            Assert.AreEqual(1, modifierManager.GetCardStackCount("war_training"));
            Assert.AreEqual(1, modifierManager.GetCardStackCount("hot_coals"));
            Assert.AreEqual(1, modifierManager.GetCardStackCount("deadeye"));

            float sniperMultiplier = modifierManager.GetDamageMultiplier("sniper");
            Assert.Greater(sniperMultiplier, 1.0f, "Sniper damage multiplier must be > 1.0 after Deadeye modifier.");
        }
    }
}
