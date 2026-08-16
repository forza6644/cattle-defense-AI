using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class FloatingCombatTextPlayModeTests
    {
        private GameObject managerGO;
        private FloatingCombatTextManager manager;
        private GameObject enemyRegistryGO;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            if (FloatingCombatTextManager.Instance != null)
            {
                Object.DestroyImmediate(FloatingCombatTextManager.Instance.gameObject);
            }

            managerGO = new GameObject("FloatingCombatTextManager", typeof(FloatingCombatTextManager));
            manager = managerGO.GetComponent<FloatingCombatTextManager>();
            createdObjects.Add(managerGO);

            // Setup Enemy Registry
            enemyRegistryGO = new GameObject("Enemy Registry", typeof(EnemyManager));
            createdObjects.Add(enemyRegistryGO);

            // Setup Castle
            castleConfig = ScriptableObject.CreateInstance<GameConfig>();
            castleConfig.castleMaxHealth = 100;
            createdObjects.Add(castleConfig);

            castleGO = new GameObject("Castle");
            castleGO.SetActive(false);
            castle = castleGO.AddComponent<Castle>();
            var field = castle.GetType().GetField("config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(castle, castleConfig);
            castleGO.SetActive(true);
            createdObjects.Add(castleGO);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        private Enemy SpawnEnemy(float health = 200f, Vector3 pos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "fct_grunt";
            data.enemyName = "fct_grunt";
            data.health = health;
            data.moveSpeed = 1f;
            data.goldReward = 5;
            createdObjects.Add(data);

            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.AddComponent<CapsuleCollider>();
            Enemy enemy = enemyGo.AddComponent<Enemy>();
            var dataField = enemy.GetType().GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dataField != null) dataField.SetValue(enemy, data);
            createdObjects.Add(enemyGo);

            enemy.PrepareForSpawn(data, pos, Quaternion.identity);
            enemy.ActivateFromPool(new[] { pos, new Vector3(0f, 0f, -100f) }, castle);

            return enemy;
        }

        [UnityTest]
        public IEnumerator FloatingCombatText_SpawnsOnEnemyDamage()
        {
            Enemy enemy = SpawnEnemy(200f, new Vector3(0f, 0f, 4f));
            int initialActive = manager.ActiveCount;

            enemy.TakeDamage(45f, false, false, "archer");

            Assert.AreEqual(initialActive + 1, manager.ActiveCount, "Floating damage number must be activated from pool upon dealing damage.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator FloatingCombatText_TriggersOnElementalReactions()
        {
            int initialActive = manager.ActiveCount;

            StatusEffectController.TriggerReactionEvent(ElementalReactionType.ThermalShock, new Vector3(1f, 0f, 3f), "fire_mage");
            StatusEffectController.TriggerReactionEvent(ElementalReactionType.Overload, new Vector3(2f, 0f, 3f), "electric_engineer");
            StatusEffectController.TriggerReactionEvent(ElementalReactionType.Shatter, new Vector3(3f, 0f, 3f), "archer");

            Assert.AreEqual(initialActive + 3, manager.ActiveCount, "Elemental reaction popups must activate from pool for each combo event.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator FloatingCombatText_RecyclesToPoolAfterLifetime()
        {
            Enemy enemy = SpawnEnemy(200f, new Vector3(0f, 0f, 4f));
            enemy.TakeDamage(50f, false, false, "archer");

            Assert.AreEqual(1, manager.ActiveCount, "One text item must be active initially.");

            // Wait for item duration (0.75s) to elapse
            yield return new WaitForSeconds(1.0f);

            Assert.AreEqual(0, manager.ActiveCount, "Text item must automatically deactivate and return to pool without memory allocation.");
            Assert.That(manager.TotalPoolCount, Is.GreaterThanOrEqualTo(40), "Pool capacity must remain pre-allocated.");
        }
    }
}
