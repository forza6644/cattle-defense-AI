using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class BestiaryPlayModeTests
    {
        private GameObject testRoot;
        private BestiaryManager bestiaryManager;

        [SetUp]
        public void SetUp()
        {
            BestiaryManager.ResetForTesting();
            testRoot = new GameObject("BestiaryTestRoot");
            bestiaryManager = testRoot.AddComponent<BestiaryManager>();
            bestiaryManager.ResetEncounters();
        }

        [TearDown]
        public void TearDown()
        {
            if (bestiaryManager != null)
            {
                bestiaryManager.ResetEncounters();
            }

            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }

            BestiaryManager.ResetForTesting();
        }

        [UnityTest]
        public IEnumerator BestiaryManager_DefaultEntries_LoadsAllCategoriesAndStats()
        {
            Assert.IsNotNull(bestiaryManager.AllEntries);
            Assert.GreaterOrEqual(bestiaryManager.AllEntries.Count, 5);

            var goblin = bestiaryManager.GetEntry("goblin_grunt");
            Assert.IsNotNull(goblin);
            Assert.AreEqual("Goblin Grunt", goblin.displayName);
            Assert.AreEqual(EnemyCategory.Swarm, goblin.category);
            Assert.IsTrue(goblin.weaknesses.Contains(StatusEffectType.Burn));

            var warlord = bestiaryManager.GetEntry("warlord_boss");
            Assert.IsNotNull(warlord);
            Assert.IsTrue(warlord.isBoss);
            Assert.AreEqual(5, warlord.threatLevel);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BestiaryManager_RegisterEncounterAndKill_PersistsCounts()
        {
            Assert.IsFalse(bestiaryManager.IsEncountered("orc_warrior"));
            Assert.AreEqual(0, bestiaryManager.GetKillCount("orc_warrior"));

            bestiaryManager.RegisterEncounter("orc_warrior");
            Assert.IsTrue(bestiaryManager.IsEncountered("orc_warrior"));

            bestiaryManager.RegisterKill("orc_warrior");
            bestiaryManager.RegisterKill("orc_warrior");
            Assert.AreEqual(2, bestiaryManager.GetKillCount("orc_warrior"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BestiaryManager_WeaknessQuery_ReturnsCorrectElementalSynergies()
        {
            var siege = bestiaryManager.GetEntry("armored_siege_ram");
            Assert.IsNotNull(siege);
            Assert.IsTrue(siege.weaknesses.Contains(StatusEffectType.Shock));
            Assert.IsTrue(siege.weaknesses.Contains(StatusEffectType.Slow));
            Assert.AreEqual("Electric Engineer / Frost Mage", siege.recommendedHeroCounter);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BestiaryManager_ResetEncounters_ClearsPlayerPrefsData()
        {
            bestiaryManager.RegisterEncounter("skeleton_archer");
            bestiaryManager.RegisterKill("skeleton_archer");
            Assert.IsTrue(bestiaryManager.IsEncountered("skeleton_archer"));

            bestiaryManager.ResetEncounters();
            Assert.IsFalse(bestiaryManager.IsEncountered("skeleton_archer"));
            Assert.AreEqual(0, bestiaryManager.GetKillCount("skeleton_archer"));
            yield return null;
        }
    }
}
