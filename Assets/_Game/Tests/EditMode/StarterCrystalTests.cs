using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    public class StarterCrystalTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("lobby_selected_starter_crystal");
            PlayerPrefs.Save();
            SaveManager.LoadProgress();
        }

        [Test]
        public void SaveManager_DefaultsToLightningCrystal_WhenNoSelectionExists()
        {
            PlayerPrefs.DeleteKey("lobby_selected_starter_crystal");
            PlayerPrefs.Save();
            SaveManager.LoadProgress();

            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo("crystal_lightning"));
        }

        [Test]
        public void SaveManager_SelectedStarterCrystalId_PersistsCorrectly()
        {
            SaveManager.SetSelectedStarterCrystal("crystal_fire");
            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo("crystal_fire"));

            SaveManager.LoadProgress();
            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo("crystal_fire"));
        }

        [Test]
        public void DraftSelectionState_WithZeroOwnedHeroes_HasSixOpenSlotsAndNoHeroes()
        {
            var state = new DraftSelectionState(new string[0], new AttackType[0], 6);

            Assert.That(state.OpenHeroSlots, Is.EqualTo(6));
            Assert.That(state.ActiveHeroIds, Is.Empty);
            Assert.That(state.HasHero("archer"), Is.False);
        }

        [Test]
        public void CardDraftSelector_WithZeroHeroes_FiltersHeroUpgradesAndAllowsRecruits()
        {
            var state = new DraftSelectionState(new string[0], new AttackType[0], 6);

            CardDefinition recruitCard = ScriptableObject.CreateInstance<CardDefinition>();
            recruitCard.id = "recruit_archer";
            recruitCard.cardCategory = CardCategory.RecruitHero;
            recruitCard.recruitHeroId = "archer";

            CardDefinition upgradeCard = ScriptableObject.CreateInstance<CardDefinition>();
            upgradeCard.id = "archer_twin_volley";
            upgradeCard.cardCategory = CardCategory.HeroUpgrade;
            upgradeCard.targetType = CardTargetType.HeroById;
            upgradeCard.targetHeroId = "archer";

            Assert.That(CardDraftSelector.IsEligible(recruitCard, state), Is.True, "Recruit cards must be eligible with 0 owned heroes.");
            Assert.That(CardDraftSelector.IsEligible(upgradeCard, state), Is.False, "Hero upgrades must be filtered out until hero is recruited.");

            Object.DestroyImmediate(recruitCard);
            Object.DestroyImmediate(upgradeCard);
        }

        [Test]
        public void AllFiveResourceDefinitionAssets_ResolveCorrectly()
        {
            string[] crystalIds = { "crystal_fire", "crystal_ice", "crystal_lightning", "crystal_stone", "crystal_shadow" };
            foreach (string id in crystalIds)
            {
                StarterCrystalDefinition def = Resources.Load<StarterCrystalDefinition>("Crystals/" + id);
                Assert.That(def, Is.Not.Null, $"StarterCrystalDefinition for '{id}' must exist in Resources/Crystals/");
                Assert.That(def.crystalId, Is.EqualTo(id));
                Assert.That(def.crystalMaterial, Is.Not.Null, $"Material for '{id}' must not be null.");
            }
        }

        [Test]
        public void RuntimeResolution_ResolvesLightningCrystal_ByDefault()
        {
            PlayerPrefs.DeleteKey("lobby_selected_starter_crystal");
            PlayerPrefs.Save();
            SaveManager.LoadProgress();

            string selectedId = SaveManager.SelectedStarterCrystalId;
            Assert.That(selectedId, Is.EqualTo("crystal_lightning"));

            StarterCrystalDefinition def = Resources.Load<StarterCrystalDefinition>("Crystals/" + selectedId);
            Assert.That(def, Is.Not.Null, "Default selected crystal definition must resolve.");
            Assert.That(def.element, Is.EqualTo(CrystalElement.Lightning));
        }

        [TestCase("crystal_fire")]
        [TestCase("crystal_ice")]
        [TestCase("crystal_lightning")]
        [TestCase("crystal_stone")]
        [TestCase("crystal_shadow")]
        public void Lobby_SelectingCrystal_WritesCorrectIdToSaveManager(string crystalId)
        {
            SaveManager.SetSelectedStarterCrystal(crystalId);
            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo(crystalId));

            SaveManager.LoadProgress();
            Assert.That(SaveManager.SelectedStarterCrystalId, Is.EqualTo(crystalId));
        }
    }
}

