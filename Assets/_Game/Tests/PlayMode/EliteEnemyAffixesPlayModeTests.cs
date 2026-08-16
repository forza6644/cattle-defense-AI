using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class EliteEnemyAffixesPlayModeTests
    {
        private GameObject enemyRegistryGO;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            // Clear any stale GameManager singleton
            if (GameManager.Instance != null)
            {
                var instanceProp = typeof(GameManager).GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp != null && instanceProp.CanWrite)
                {
                    instanceProp.SetValue(null, null);
                }
                else
                {
                    var backingField = typeof(GameManager).GetField("<Instance>k__BackingField",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (backingField != null) backingField.SetValue(null, null);
                }
            }

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

        private Enemy SpawnEnemyWithAffix(EnemyAffixType affix, float health = 200f, float shield = 0f, Vector3 pos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "elite_grunt";
            data.enemyName = "elite_grunt";
            data.health = health;
            data.moveSpeed = 1f;
            data.shieldCapacity = shield;
            data.affix = affix;
            data.explosionRadius = 3.5f;
            data.explosionDamage = 40f;
            data.goldReward = 10;
            data.classification = affix != EnemyAffixType.None ? EnemyClassification.Elite : EnemyClassification.Normal;
            createdObjects.Add(data);

            GameObject enemyGo = new GameObject("EliteEnemy");
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
        public IEnumerator EliteEnemy_ShieldedAffix_AbsorbsDamageUntilBroken()
        {
            Enemy enemy = SpawnEnemyWithAffix(EnemyAffixType.Shielded, health: 200f, shield: 60f, pos: new Vector3(0f, 0f, 5f));

            Assert.AreEqual(60f, enemy.CurrentShield, "Enemy must start with full shield capacity.");
            Assert.AreEqual(200f, enemy.CurrentHealth, "Enemy health must be untouched initially.");

            // Hit 1: 40 damage absorbed by shield
            enemy.TakeDamage(40f, ignoreArmor: true);
            Assert.AreEqual(20f, enemy.CurrentShield, "Shield must absorb damage first.");
            Assert.AreEqual(200f, enemy.CurrentHealth, "Health must not be reduced while shield is active.");

            // Hit 2: 40 damage breaks remaining 20 shield and deals 20 to health
            enemy.TakeDamage(40f, ignoreArmor: true);
            Assert.AreEqual(0f, enemy.CurrentShield, "Shield must be depleted.");
            Assert.AreEqual(180f, enemy.CurrentHealth, "Excess damage must penetrate to health.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EliteEnemy_VolatileExplosion_DamagesSurroundingEnemiesOnDeath()
        {
            Enemy volatileEnemy = SpawnEnemyWithAffix(EnemyAffixType.VolatileExplosive, health: 50f, pos: new Vector3(0f, 0f, 5f));
            Enemy adjacentEnemy = SpawnEnemyWithAffix(EnemyAffixType.None, health: 200f, pos: new Vector3(2f, 0f, 5f)); // within 3.5m
            Enemy distantEnemy = SpawnEnemyWithAffix(EnemyAffixType.None, health: 200f, pos: new Vector3(20f, 0f, 5f)); // outside 3.5m

            float adjacentInitialHp = adjacentEnemy.CurrentHealth;
            float distantInitialHp = distantEnemy.CurrentHealth;

            // Kill volatile enemy
            volatileEnemy.Kill();

            Assert.That(adjacentEnemy.CurrentHealth, Is.LessThan(adjacentInitialHp), "Adjacent enemy must take explosion damage on volatile enemy death.");
            Assert.AreEqual(distantInitialHp, distantEnemy.CurrentHealth, "Distant enemy beyond blast radius must not be damaged.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EliteEnemy_BerserkerAffix_EnragesAtLowHealth()
        {
            Enemy enemy = SpawnEnemyWithAffix(EnemyAffixType.BerserkerRage, health: 100f, pos: new Vector3(0f, 0f, 5f));

            Assert.IsFalse(enemy.IsEnraged, "Enemy must not be enraged at full health.");

            // Reduce health to 35% (below 40% threshold)
            enemy.TakeDamage(65f, ignoreArmor: true);

            Assert.IsTrue(enemy.IsEnraged, "Enemy must become enraged when HP drops below 40%.");
            Assert.That(enemy.SlowMultiplier, Is.GreaterThanOrEqualTo(1.35f), "Enraged enemy must receive a speed boost.");

            yield return null;
        }
    }
}
