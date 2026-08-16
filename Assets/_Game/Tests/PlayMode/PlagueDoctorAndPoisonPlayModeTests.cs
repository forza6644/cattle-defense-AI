using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class PlagueDoctorAndPoisonPlayModeTests
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
            Time.timeScale = 3f;

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

            enemyRegistryGO = new GameObject("Enemy Registry", typeof(EnemyManager));
            createdObjects.Add(enemyRegistryGO);

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

            if (RunModifierManager.Instance == null)
            {
                runModifierGO = new GameObject("RunModifierManager", typeof(RunModifierManager));
                createdObjects.Add(runModifierGO);
            }
            RunModifierManager.Instance.ClearModifiers();

            lastReactionType = ElementalReactionType.None;
            reactionCount = 0;
            StatusEffectController.OnElementalReaction += HandleElementalReaction;
        }

        [TearDown]
        public void TearDown()
        {
            StatusEffectController.OnElementalReaction -= HandleElementalReaction;
            SpectralMinion.ClearAll();
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
        public IEnumerator Poison_AppliesStatusEffect_AndDealsTickingDecayDamage()
        {
            Enemy enemy = SpawnEnemy(200f, new Vector3(0f, 0f, 5f));
            float initialHealth = enemy.CurrentHealth;

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 10f, 3.0f, "plague_doctor"));

            StatusEffectController sec = enemy.GetComponent<StatusEffectController>();
            Assert.IsNotNull(sec, "StatusEffectController must be attached.");
            Assert.AreEqual(1, sec.ActiveEffects.Count, "Poison effect must be registered.");

            yield return new WaitForSeconds(1.2f);

            Assert.That(enemy.CurrentHealth, Is.LessThan(initialHealth - 15f), "Poison must tick decay damage over time.");
        }

        [UnityTest]
        public IEnumerator CorrosiveBlast_PoisonPlusFire_TriggersReactionAndDealsAoE()
        {
            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            Enemy nearbyEnemy = SpawnEnemy(300f, new Vector3(1f, 0f, 5f));
            float initialNearbyHp = nearbyEnemy.CurrentHealth;

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 15f, 4.0f, "plague_doctor"));
            Assert.AreEqual(ElementalReactionType.None, lastReactionType, "Poison alone must not trigger reaction.");

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 15f, 4.0f, "fire_mage"));

            Assert.AreEqual(ElementalReactionType.CorrosiveBlast, lastReactionType, "Poison + Burn must trigger CorrosiveBlast.");
            Assert.That(nearbyEnemy.CurrentHealth, Is.LessThan(initialNearbyHp), "Corrosive blast must splash nearby targets.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Neurotoxin_PoisonPlusShock_TriggersStunAndShockSpread()
        {
            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            Enemy nearbyEnemy = SpawnEnemy(300f, new Vector3(1.5f, 0f, 5f));

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 12f, 4.0f, "plague_doctor"));
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 3.0f, "electric_engineer"));

            Assert.AreEqual(ElementalReactionType.Neurotoxin, lastReactionType, "Poison + Shock must trigger Neurotoxin.");

            StatusEffectController nearbySec = nearbyEnemy.GetComponent<StatusEffectController>();
            Assert.IsTrue(nearbySec != null && nearbySec.ActiveEffects.Count > 0, "Neurotoxin must spread Shock to nearby enemies.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator BrittleBlight_PoisonPlusFrost_TriggersReaction()
        {
            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            float initialHealth = enemy.CurrentHealth;

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 12f, 4.0f, "plague_doctor"));
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 3.0f, "frost_mage"));

            Assert.AreEqual(ElementalReactionType.BrittleBlight, lastReactionType, "Poison + Frost must trigger BrittleBlight.");
            Assert.That(enemy.CurrentHealth, Is.LessThan(initialHealth - 10f), "Brittle Blight must deal decay burst damage.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SpectralMinion_SpawnsOnPoisonedEnemyDemise_AndAttacksNearbyEnemies()
        {
            Enemy enemy = SpawnEnemy(15f, new Vector3(0f, 0f, 4f));
            Enemy nextEnemy = SpawnEnemy(200f, new Vector3(0.5f, 0f, 4f));
            float nextInitialHp = nextEnemy.CurrentHealth;

            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Poison, 30f, 4.0f, "plague_doctor"));
            enemy.TakeDamage(30f, true);

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(enemy.IsDead, "Enemy must die to heavy poison.");
            Assert.That(SpectralMinion.ActiveMinions.Count, Is.GreaterThan(0), "A Spectral Minion must spawn upon poisoned enemy death.");

            yield return new WaitForSeconds(1.2f);

            Assert.That(nextEnemy.CurrentHealth, Is.LessThan(nextInitialHp), "Spectral minion must strike nearby incoming enemy.");
        }

        [UnityTest]
        public IEnumerator VenomBurst_CardModifier_IncreasesPoisonDamage()
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "test_venom_burst";
            card.displayName = "Venom Burst";
            card.cardCategory = CardCategory.Modifier;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "plague_doctor";
            card.modifierType = CardModifierType.PoisonDamageAdd;
            card.modifierValue = 12f;
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);
            Assert.AreEqual(12f, RunModifierManager.Instance.GetPoisonDamageAdd("plague_doctor"));

            yield return null;
        }
    }
}
