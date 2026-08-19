using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class AscensionHeatPlayModeTests
    {
        private GameObject testRoot;
        private AscensionManager ascensionManager;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(DifficultyRuleset.PrefsSelectedMode);
            AscensionManager.ResetForTesting();
            testRoot = new GameObject("AscensionTestRoot");
            ascensionManager = testRoot.AddComponent<AscensionManager>();
            ascensionManager.ClearAllMutators();
        }

        [TearDown]
        public void TearDown()
        {
            if (ascensionManager != null)
            {
                ascensionManager.ClearAllMutators();
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
            AscensionManager.ResetForTesting();
        }

        private AscensionMutatorDefinition CreateMutator(string id, string name, int heat, AscensionMutatorType type, float val, float bonus = 0.1f)
        {
            var def = ScriptableObject.CreateInstance<AscensionMutatorDefinition>();
            def.id = id;
            def.displayName = name;
            def.heatPoints = heat;
            def.mutatorType = type;
            def.effectValue = val;
            def.scoreMultiplierBonus = bonus;
            createdObjects.Add(def);
            return def;
        }

        [UnityTest]
        public IEnumerator AscensionManager_HeatAndScoreMultiplier_CalculatesCorrectly()
        {
            var m1 = CreateMutator("m1", "Mutator 1", 1, AscensionMutatorType.EnemySpeedMultiplier, 0.2f, 0.10f);
            var m2 = CreateMutator("m2", "Mutator 2", 2, AscensionMutatorType.EnemyRegenPercent, 0.03f, 0.20f);
            ascensionManager.RegisterMutators(new[] { m1, m2 });

            Assert.AreEqual(0, ascensionManager.GetCurrentHeatLevel());
            Assert.AreEqual(1.0f, ascensionManager.GetScoreMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("m1", true);
            Assert.AreEqual(1, ascensionManager.GetCurrentHeatLevel());
            Assert.AreEqual(1.10f, ascensionManager.GetScoreMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("m2", true);
            Assert.AreEqual(3, ascensionManager.GetCurrentHeatLevel());
            Assert.AreEqual(1.30f, ascensionManager.GetScoreMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("m1", false);
            Assert.AreEqual(2, ascensionManager.GetCurrentHeatLevel());
            Assert.AreEqual(1.20f, ascensionManager.GetScoreMultiplier(), 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_FastEnemies_IncreasesEnemySpeed()
        {
            var fast = CreateMutator("fast_enemies", "Fast Enemies", 1, AscensionMutatorType.EnemySpeedMultiplier, 0.25f);
            ascensionManager.RegisterMutators(new[] { fast });

            Assert.AreEqual(1.0f, ascensionManager.GetEnemySpeedMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("fast_enemies", true);
            Assert.AreEqual(1.25f, ascensionManager.GetEnemySpeedMultiplier(), 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_ArmoredHorde_IncreasesEnemyArmor()
        {
            var armorMutator = CreateMutator("armored_horde", "Armored Horde", 1, AscensionMutatorType.EnemyArmorBonus, 15f);
            ascensionManager.RegisterMutators(new[] { armorMutator });

            Assert.AreEqual(0f, ascensionManager.GetEnemyArmorBonus(), 0.001f);

            ascensionManager.SetMutatorActive("armored_horde", true);
            Assert.AreEqual(15f, ascensionManager.GetEnemyArmorBonus(), 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_RegeneratingMonsters_TicksEnemyRegeneration()
        {
            var regen = CreateMutator("regenerating_monsters", "Regenerating Monsters", 2, AscensionMutatorType.EnemyRegenPercent, 0.05f);
            ascensionManager.RegisterMutators(new[] { regen });

            Assert.AreEqual(0f, ascensionManager.GetEnemyRegenPercent(), 0.001f);

            ascensionManager.SetMutatorActive("regenerating_monsters", true);
            Assert.AreEqual(0.05f, ascensionManager.GetEnemyRegenPercent(), 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_CostlyEconomy_ReducesGoldGain()
        {
            var costly = CreateMutator("costly_economy", "Costly Economy", 1, AscensionMutatorType.GoldRewardMultiplier, 0.20f);
            ascensionManager.RegisterMutators(new[] { costly });

            Assert.AreEqual(1.0f, ascensionManager.GetGoldMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("costly_economy", true);
            Assert.AreEqual(0.80f, ascensionManager.GetGoldMultiplier(), 0.001f);

            var econGo = new GameObject("TestEconomy");
            econGo.transform.SetParent(testRoot.transform);
            var econ = econGo.AddComponent<EconomyManager>();

            // Add 100 gold -> with 0.8x multiplier = 80 gold added
            int before = econ.Gold;
            econ.AddGold(100);
            Assert.AreEqual(before + 80, econ.Gold);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_BrittleCastle_ReducesCastleMaxHealth()
        {
            var brittle = CreateMutator("brittle_castle", "Brittle Castle", 2, AscensionMutatorType.CastleMaxHealthPenalty, 0.30f);
            ascensionManager.RegisterMutators(new[] { brittle });

            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.castleMaxHealth = 100;
            createdObjects.Add(config);

            var castleGo = new GameObject("TestCastle");
            castleGo.transform.SetParent(testRoot.transform);
            var castle = castleGo.AddComponent<Castle>();

            // Inject config via reflection/field
            var configField = typeof(Castle).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(castle, config);

            Assert.AreEqual(100, castle.MaxHealth);

            ascensionManager.SetMutatorActive("brittle_castle", true);
            Assert.AreEqual(70, castle.MaxHealth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AscensionManager_EmpoweredElites_IncreasesEliteHealthMultiplier()
        {
            var elites = CreateMutator("empowered_elites", "Empowered Elites", 2, AscensionMutatorType.EliteExtraAffixAndHealth, 0.50f);
            ascensionManager.RegisterMutators(new[] { elites });

            Assert.AreEqual(1.0f, ascensionManager.GetEliteHealthMultiplier(), 0.001f);

            ascensionManager.SetMutatorActive("empowered_elites", true);
            Assert.AreEqual(1.50f, ascensionManager.GetEliteHealthMultiplier(), 0.001f);
            yield return null;
        }
    }
}
