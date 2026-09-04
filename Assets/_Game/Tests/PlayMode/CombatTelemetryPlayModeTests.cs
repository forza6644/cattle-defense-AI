using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class CombatTelemetryPlayModeTests
    {
        private GameObject testRoot;
        private CombatTelemetryManager telemetryManager;

        [SetUp]
        public void SetUp()
        {
            var existing = Object.FindObjectsByType<CombatTelemetryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null) Object.DestroyImmediate(existing[i].gameObject);
            }
            testRoot = new GameObject("TelemetryTestRoot");
            telemetryManager = testRoot.AddComponent<CombatTelemetryManager>();
            var prop = typeof(CombatTelemetryManager).GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            prop?.SetValue(null, telemetryManager);
            telemetryManager.ResetTelemetry();
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
            var existing = Object.FindObjectsByType<CombatTelemetryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null) Object.DestroyImmediate(existing[i].gameObject);
            }
            var prop = typeof(CombatTelemetryManager).GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            prop?.SetValue(null, null);
        }

        [UnityTest]
        public IEnumerator CombatTelemetryManager_RecordDamageAndCrits_AggregatesCorrectly()
        {
            CombatTelemetryManager.RecordDamage("frost_mage", 500f, false);
            CombatTelemetryManager.RecordDamage("frost_mage", 500f, true);
            CombatTelemetryManager.RecordDamage("fire_mage", 1000f, true);

            Assert.AreEqual(2000f, telemetryManager.GetTotalDamage());
            Assert.AreEqual(1000f, telemetryManager.GetHeroDamage("frost_mage"));
            Assert.AreEqual(1000f, telemetryManager.GetHeroDamage("fire_mage"));
            Assert.AreEqual(50f, telemetryManager.GetHeroPercentage("frost_mage"), 0.01f);
            Assert.AreEqual(50f, telemetryManager.GetHeroPercentage("fire_mage"), 0.01f);
            Assert.AreEqual(1, telemetryManager.CritsByHero["frost_mage"]);
            Assert.AreEqual(1, telemetryManager.CritsByHero["fire_mage"]);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatTelemetryManager_GetMvpReport_IdentifiesHighestDamageHero()
        {
            CombatTelemetryManager.RecordDamage("archer", 200f);
            CombatTelemetryManager.RecordDamage("sniper", 1500f, true);
            CombatTelemetryManager.RecordDamage("bombardier", 800f);

            var mvp = telemetryManager.GetMvpReport();
            Assert.AreEqual("sniper", mvp.heroId);
            Assert.AreEqual(1500f, mvp.totalDamage);
            Assert.Greater(mvp.damagePercentage, 50f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatTelemetryManager_ReactionsAndCastleDamage_TracksAccurately()
        {
            CombatTelemetryManager.RecordReaction(ElementalReactionType.ThermalShock, 250f);
            CombatTelemetryManager.RecordReaction(ElementalReactionType.ThermalShock, 250f);
            CombatTelemetryManager.RecordReaction(ElementalReactionType.Overload, 400f);

            Assert.AreEqual(2, telemetryManager.ReactionCounts[ElementalReactionType.ThermalShock]);
            Assert.AreEqual(500f, telemetryManager.ReactionDamage[ElementalReactionType.ThermalShock]);
            Assert.AreEqual(1, telemetryManager.ReactionCounts[ElementalReactionType.Overload]);
            Assert.AreEqual(400f, telemetryManager.ReactionDamage[ElementalReactionType.Overload]);

            CombatTelemetryManager.RecordCastleDamage(30f, 50f);
            Assert.AreEqual(30f, telemetryManager.TotalCastleDamageTaken);
            Assert.AreEqual(50f, telemetryManager.TotalShieldAbsorbed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatTelemetryManager_ResetTelemetry_ClearsState()
        {
            CombatTelemetryManager.RecordDamage("frost_mage", 500f);
            CombatTelemetryManager.RecordKill();

            Assert.Greater(telemetryManager.GetTotalDamage(), 0f);

            telemetryManager.ResetTelemetry();
            Assert.AreEqual(0f, telemetryManager.GetTotalDamage());
            Assert.AreEqual(0, telemetryManager.TotalEnemiesKilled);
            yield return null;
        }
    }
}
