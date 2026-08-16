using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class CastleAbilitiesPlayModeTests
    {
        private GameObject testRoot;
        private CastleAbilityManager abilityManager;
        private Castle testCastle;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("CastleAbilityTestRoot");
            abilityManager = testRoot.AddComponent<CastleAbilityManager>();
            testCastle = testRoot.AddComponent<Castle>();

            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.castleMaxHealth = 500;
            var configField = typeof(Castle).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(testCastle, config);
            var awakeMethod = typeof(Castle).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(testCastle, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        [UnityTest]
        public IEnumerator CastleAbilityManager_DefaultAbilities_LoadsAndInitializesEnergy()
        {
            Assert.IsNotNull(abilityManager.AllAbilities);
            Assert.AreEqual(3, abilityManager.AllAbilities.Count);

            var mortar = abilityManager.GetAbility("ability_mortar");
            Assert.IsNotNull(mortar);
            Assert.AreEqual("Arcane Mortar Strike", mortar.displayName);
            Assert.AreEqual(30f, mortar.energyCost);
            Assert.AreEqual(100f, abilityManager.maxEnergy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CastleAbilityManager_CastAbility_ConsumesEnergyAndAppliesCooldown()
        {
            abilityManager.currentEnergy = 50f;
            Assert.IsTrue(abilityManager.IsReady("ability_mortar"));

            bool cast = abilityManager.CastAbility("ability_mortar", Vector3.zero);
            Assert.IsTrue(cast);
            Assert.AreEqual(20f, abilityManager.currentEnergy); // 50 - 30
            Assert.Greater(abilityManager.GetCooldownRemaining("ability_mortar"), 15f);

            // Cannot cast while on cooldown
            Assert.IsFalse(abilityManager.IsReady("ability_mortar"));
            bool castAgain = abilityManager.CastAbility("ability_mortar", Vector3.zero);
            Assert.IsFalse(castAgain);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CastleAbilityManager_FortressAegis_GrantsShieldToCastle()
        {
            abilityManager.currentEnergy = 60f;
            float initialShield = testCastle.CurrentShield;

            bool cast = abilityManager.CastAbility("ability_barrier", Vector3.zero);
            Assert.IsTrue(cast);
            Assert.AreEqual(initialShield + 300f, testCastle.CurrentShield);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CastleAbilityManager_EnergyRegen_AddsEnergyOverTime()
        {
            abilityManager.currentEnergy = 10f;
            abilityManager.AddEnergy(25f);
            Assert.AreEqual(35f, abilityManager.currentEnergy);

            abilityManager.AddEnergy(200f);
            Assert.AreEqual(100f, abilityManager.currentEnergy); // Clamped at max
            yield return null;
        }
    }
}
