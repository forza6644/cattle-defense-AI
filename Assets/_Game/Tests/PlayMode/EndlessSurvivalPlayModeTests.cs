using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class EndlessSurvivalPlayModeTests
    {
        private GameObject testRoot;
        private EndlessSurvivalManager endlessManager;
        private Castle castle;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("EndlessTestRoot");
            endlessManager = testRoot.AddComponent<EndlessSurvivalManager>();

            var castleGo = new GameObject("Castle");
            castleGo.transform.SetParent(testRoot.transform);
            castle = castleGo.AddComponent<Castle>();

            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.castleMaxHealth = 100;
            createdObjects.Add(config);

            var configField = typeof(Castle).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(castle, config);
        }

        [TearDown]
        public void TearDown()
        {
            if (endlessManager != null)
            {
                endlessManager.StopEndlessMode();
            }

            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }

            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator EndlessSurvivalManager_StartAndAdvance_ScalesWaveNumbersAndTrophies()
        {
            Assert.IsFalse(endlessManager.IsEndlessActive);

            endlessManager.StartEndlessMode();
            Assert.IsTrue(endlessManager.IsEndlessActive);
            Assert.AreEqual(1, endlessManager.AbyssalWaveNumber);

            int initialTrophies = endlessManager.AbyssalTrophies;
            endlessManager.AdvanceAbyssalWave();

            Assert.AreEqual(2, endlessManager.AbyssalWaveNumber);
            Assert.Greater(endlessManager.AbyssalTrophies, initialTrophies);
            Assert.GreaterOrEqual(endlessManager.HighestAbyssalWave, 1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessSurvivalManager_ScalingMath_HealthDamageAndCountScaleProperly()
        {
            float hpW1 = endlessManager.GetAbyssalHealthMultiplier(1);
            float hpW5 = endlessManager.GetAbyssalHealthMultiplier(5);
            float hpW10 = endlessManager.GetAbyssalHealthMultiplier(10);

            Assert.Greater(hpW5, hpW1);
            Assert.Greater(hpW10, hpW5);

            float dmgW1 = endlessManager.GetAbyssalDamageMultiplier(1);
            float dmgW10 = endlessManager.GetAbyssalDamageMultiplier(10);
            Assert.Greater(dmgW10, dmgW1);

            int countW1 = endlessManager.GetAbyssalEnemyCount(1, 15);
            int countW10 = endlessManager.GetAbyssalEnemyCount(10, 15);
            Assert.Greater(countW10, countW1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessSurvivalManager_OverchargeDraft_GeneratesDistinctOptions()
        {
            var draft = endlessManager.RollOverchargeDraft(3);
            Assert.IsNotNull(draft);
            Assert.AreEqual(3, draft.Count);

            var ids = new HashSet<string>();
            foreach (var opt in draft)
            {
                Assert.IsNotNull(opt);
                Assert.IsTrue(ids.Add(opt.id), $"Duplicate overcharge option {opt.id} generated");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessSurvivalManager_ClaimOvercharge_AppliesFortressAndSingularity()
        {
            Assert.AreEqual(0, endlessManager.ActiveOvercharges.Count);

            var singularity = new AbyssalOvercharge
            {
                id = "abyssal_singularity",
                name = "Singularity Barrier",
                description = "+200 Castle Kinetic Shield",
                rarityBadge = "ABYSSAL"
            };

            endlessManager.ClaimOvercharge(singularity);
            Assert.AreEqual(1, endlessManager.ActiveOvercharges.Count);
            Assert.AreEqual(200f, castle.CurrentShield);

            castle.TakeDamage(50);
            Assert.AreEqual(150f, castle.CurrentShield);
            yield return null;
        }
    }
}
