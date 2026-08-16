using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class DraftSynergyEvaluationTests
    {
        private GameObject rosterObj;
        private HeroRosterManager roster;

        [SetUp]
        public void SetUp()
        {
            rosterObj = new GameObject("TestRosterManager");
            roster = rosterObj.AddComponent<HeroRosterManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (rosterObj != null)
            {
                Object.DestroyImmediate(rosterObj);
            }
        }

        [Test]
        public void EvaluateSynergyTag_NullCard_ReturnsNull()
        {
            string tag = CardDraftManager.EvaluateSynergyTag(null, roster, null);
            Assert.IsNull(tag);
        }

        [Test]
        public void EvaluateSynergyTag_ThermalShockCombo_ReturnsThermalSynergy()
        {
            var frostCard = ScriptableObject.CreateInstance<CardDefinition>();
            frostCard.id = "card_frostbite";
            frostCard.displayName = "Frostbite Touch";
            frostCard.targetHeroId = "frost_mage";

            // Fire Mage recruited
            var fireHero = ScriptableObject.CreateInstance<HeroDefinition>();
            fireHero.id = "fire_mage";
            roster.RegisterHeroDefinition(fireHero);
            roster.AddOwnedHero("fire_mage");

            string tag = CardDraftManager.EvaluateSynergyTag(frostCard, roster, null);
            Assert.AreEqual("🔥 Thermal Synergy", tag);
        }

        [Test]
        public void EvaluateSynergyTag_OverloadCombo_ReturnsOverloadSynergy()
        {
            var shockCard = ScriptableObject.CreateInstance<CardDefinition>();
            shockCard.id = "card_static_field";
            shockCard.displayName = "Static Lightning Field";
            shockCard.targetHeroId = "electric_engineer";

            // Fire Mage recruited
            var fireHero = ScriptableObject.CreateInstance<HeroDefinition>();
            fireHero.id = "fire_mage";
            roster.RegisterHeroDefinition(fireHero);
            roster.AddOwnedHero("fire_mage");

            string tag = CardDraftManager.EvaluateSynergyTag(shockCard, roster, null);
            Assert.AreEqual("⚡ Overload Synergy", tag);
        }

        [Test]
        public void EvaluateSynergyTag_MatchingHeroUpgrade_ReturnsHeroUpgrade()
        {
            var archerUpgrade = ScriptableObject.CreateInstance<CardDefinition>();
            archerUpgrade.id = "card_sharpened_arrows";
            archerUpgrade.displayName = "Sharpened Arrows";
            archerUpgrade.targetHeroId = "archer";

            var archer = ScriptableObject.CreateInstance<HeroDefinition>();
            archer.id = "archer";
            roster.RegisterHeroDefinition(archer);
            roster.AddOwnedHero("archer");

            string tag = CardDraftManager.EvaluateSynergyTag(archerUpgrade, roster, null);
            Assert.AreEqual("⭐ Hero Upgrade", tag);
        }
    }
}
