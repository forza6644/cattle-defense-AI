using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class ElementalSynergyPlayModeTests
    {
        private GameObject enemyRegistryGO;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
        private GameObject runModifierGO;
        private readonly List<Object> createdObjects = new List<Object>();

        private ElementalReactionType lastReactionType = ElementalReactionType.None;
        private Vector3 lastReactionPos;
        private string lastReactionHeroId;
        private int reactionCount = 0;

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

            // Setup RunModifierManager
            if (RunModifierManager.Instance == null)
            {
                runModifierGO = new GameObject("RunModifierManager", typeof(RunModifierManager));
                createdObjects.Add(runModifierGO);
            }
            RunModifierManager.Instance.ClearModifiers();

            // Hook Elemental Reaction Events
            lastReactionType = ElementalReactionType.None;
            reactionCount = 0;
            StatusEffectController.OnElementalReaction += HandleElementalReaction;
        }

        [TearDown]
        public void TearDown()
        {
            StatusEffectController.OnElementalReaction -= HandleElementalReaction;
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        private void HandleElementalReaction(ElementalReactionType type, Vector3 pos, string heroId)
        {
            lastReactionType = type;
            lastReactionPos = pos;
            lastReactionHeroId = heroId;
            reactionCount++;
        }

        private Enemy SpawnEnemy(float health = 300f, Vector3 pos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "test_grunt";
            data.enemyName = "test_grunt";
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
        public IEnumerator ThermalShock_FirePlusFrost_TriggersReactionAndDealsBurstDamage()
        {
            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            float initialHealth = enemy.CurrentHealth;

            // Apply Frost (Slow) first
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.4f, 4.0f, "frost_mage"));
            Assert.AreEqual(ElementalReactionType.None, lastReactionType, "Slow alone must not trigger reaction.");

            // Apply Burn (Fire) second
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 15f, 4.0f, "fire_mage"));

            Assert.AreEqual(ElementalReactionType.ThermalShock, lastReactionType, "Fire applied to slowed target must trigger ThermalShock.");
            Assert.AreEqual("fire_mage", lastReactionHeroId);
            Assert.That(enemy.CurrentHealth, Is.LessThan(initialHealth - 20f), "Thermal Shock must deal immediate burst damage.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ThermalShock_FrostPlusFire_TriggersReactionInReverseOrder()
        {
            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            float initialHealth = enemy.CurrentHealth;

            // Apply Burn first
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 20f, 4.0f, "fire_mage"));
            Assert.AreEqual(ElementalReactionType.None, lastReactionType, "Burn alone must not trigger reaction.");

            // Apply Slow second
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 4.0f, "frost_mage"));

            Assert.AreEqual(ElementalReactionType.ThermalShock, lastReactionType, "Frost applied to burning target must trigger ThermalShock.");
            Assert.AreEqual("frost_mage", lastReactionHeroId);
            Assert.That(enemy.CurrentHealth, Is.LessThan(initialHealth - 25f), "Thermal Shock burst must scale with Burn potency.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Overload_ShockPlusBurn_TriggersReactionAndDischargesNearbyEnemies()
        {
            Enemy primary = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            Enemy adjacent = SpawnEnemy(300f, new Vector3(2f, 0f, 5f)); // within 5m
            Enemy distant = SpawnEnemy(300f, new Vector3(20f, 0f, 5f)); // outside 5m

            float primaryInitial = primary.CurrentHealth;
            float adjacentInitial = adjacent.CurrentHealth;
            float distantInitial = distant.CurrentHealth;

            // Apply Shock to primary
            primary.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1.0f, 3.0f, "electric_engineer"));

            // Apply Burn to primary -> Overload reaction
            primary.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 15f, 3.0f, "fire_mage"));

            Assert.AreEqual(ElementalReactionType.Overload, lastReactionType, "Shock + Burn must trigger Overload.");
            Assert.That(primary.CurrentHealth, Is.LessThan(primaryInitial - 20f), "Primary must take direct Overload burst damage.");
            Assert.That(adjacent.CurrentHealth, Is.LessThan(adjacentInitial - 10f), "Adjacent enemy within 5m must receive arc discharge damage.");
            Assert.AreEqual(distantInitial, distant.CurrentHealth, "Distant enemy beyond 5m must not be damaged.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Shatter_PhysicalDamageOnSlowedEnemy_DealsBonusDamage()
        {
            Enemy baselineEnemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            Enemy slowedEnemy = SpawnEnemy(300f, new Vector3(0f, 0f, 10f));

            // Apply slow to second enemy
            slowedEnemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.3f, 5.0f, "frost_mage"));

            // Deal physical damage
            float baseHit = baselineEnemy.TakeDamage(100f, true, false, "archer");
            float shatterHit = slowedEnemy.TakeDamage(100f, true, false, "archer");

            Assert.AreEqual(100f, baseHit, 0.01f);
            Assert.AreEqual(130f, shatterHit, 0.01f, "Shatter should grant +30% damage multiplier against slowed targets.");
            Assert.AreEqual(ElementalReactionType.Shatter, lastReactionType, "Shatter reaction event should be emitted.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementalReaction_RunModifiers_AmplifyReactionDamage()
        {
            Enemy enemy1 = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            enemy1.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.4f, 4f, "frost_mage"));
            enemy1.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 10f, 4f, "fire_mage"));
            float damageUnbuffed = 300f - enemy1.CurrentHealth;

            // Add Elemental Reaction Modifier Card (+50% reaction damage)
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "elemental_resonance";
            card.displayName = "Elemental Resonance";
            card.modifierType = CardModifierType.ElementalReactionDamageMultiplier;
            card.modifierValue = 0.50f;
            card.targetType = CardTargetType.Global;
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy2 = SpawnEnemy(300f, new Vector3(0f, 0f, 10f));
            enemy2.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.4f, 4f, "frost_mage"));
            enemy2.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 10f, 4f, "fire_mage"));
            float damageBuffed = 300f - enemy2.CurrentHealth;

            Assert.That(damageBuffed, Is.GreaterThan(damageUnbuffed * 1.35f), "Elemental reaction damage must scale with ElementalReactionDamageMultiplier card.");

            yield return null;
        }
    }
}
