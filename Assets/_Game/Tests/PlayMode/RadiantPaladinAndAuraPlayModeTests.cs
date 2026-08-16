using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class RadiantPaladinAndAuraPlayModeTests
    {
        private GameObject enemyRegistryGO;
        private GameObject rosterManagerGO;
        private HeroRosterManager rosterManager;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
        private GameObject runModifierGO;
        private readonly List<Object> createdObjects = new List<Object>();

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

            rosterManagerGO = new GameObject("HeroRosterManager", typeof(HeroRosterManager));
            rosterManager = rosterManagerGO.GetComponent<HeroRosterManager>();
            createdObjects.Add(rosterManagerGO);

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

        private HeroDefinition CreateHeroDef(string id, float dmg = 15f, float fr = 1f, float range = 10f, HeroAbilityType ability = HeroAbilityType.None)
        {
            HeroDefinition def = ScriptableObject.CreateInstance<HeroDefinition>();
            def.id = id;
            def.displayName = id;
            def.baseDamage = dmg;
            def.baseFireRate = fr;
            def.baseRange = range;
            def.abilityType = ability;
            def.weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.weapon.attackType = AttackType.SingleTarget;
            def.heroPrefab = new GameObject(id + "_Prefab");
            def.heroPrefab.AddComponent<HeroAttack>();
            createdObjects.Add(def.heroPrefab);
            createdObjects.Add(def.weapon);
            createdObjects.Add(def);
            return def;
        }

        private Enemy SpawnEnemy(float health = 300f, Vector3 pos = default, float shield = 0f)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "test_grunt";
            data.enemyName = "test_grunt";
            data.health = health;
            data.moveSpeed = 1f;
            data.goldReward = 5;
            if (shield > 0f)
            {
                data.affix = EnemyAffixType.Shielded;
                data.shieldCapacity = shield;
            }
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
        public IEnumerator PaladinAura_BuffsAdjacentHeroFireRateAndCrit()
        {
            // Create Hero Slot 1 with Archer
            GameObject slot1GO = new GameObject("Slot1");
            HeroSlot slot1 = slot1GO.AddComponent<HeroSlot>();
            HeroDefinition archerDef = CreateHeroDef("archer", 10f, 1.0f);
            slot1.startingHero = archerDef;
            createdObjects.Add(slot1GO);

            // Create Hero Slot 2 with Paladin
            GameObject slot2GO = new GameObject("Slot2");
            slot2GO.transform.position = new Vector3(2f, 0f, 0f);
            HeroSlot slot2 = slot2GO.AddComponent<HeroSlot>();
            HeroDefinition paladinDef = CreateHeroDef("radiant_paladin", 20f, 1.0f);
            slot2.startingHero = paladinDef;
            createdObjects.Add(slot2GO);

            rosterManager.RegisterSlot(slot1);
            rosterManager.RegisterSlot(slot2);
            slot1.SpawnHero(archerDef);
            slot2.SpawnHero(paladinDef);

            HeroAttack archerAttack = slot1.CurrentHero;
            Assert.IsNotNull(archerAttack);

            // Paladin Aura grants +15% FireRate and +10% Crit
            Assert.That(archerAttack.GetModifiedFireRate(), Is.GreaterThan(1.10f), "Archer fire rate must be boosted by Paladin Aura.");
            Assert.That(archerAttack.GetModifiedCritChance(), Is.GreaterThanOrEqualTo(0.15f), "Archer crit chance must be boosted by Paladin Aura.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SanctuaryAura_CardUpgrade_IncreasesAuraBonus()
        {
            GameObject slot1GO = new GameObject("Slot1");
            HeroSlot slot1 = slot1GO.AddComponent<HeroSlot>();
            HeroDefinition archerDef = CreateHeroDef("archer", 10f, 1.0f);
            slot1.startingHero = archerDef;
            createdObjects.Add(slot1GO);

            GameObject slot2GO = new GameObject("Slot2");
            slot2GO.transform.position = new Vector3(2f, 0f, 0f);
            HeroSlot slot2 = slot2GO.AddComponent<HeroSlot>();
            HeroDefinition paladinDef = CreateHeroDef("radiant_paladin", 20f, 1.0f);
            slot2.startingHero = paladinDef;
            createdObjects.Add(slot2GO);

            rosterManager.RegisterSlot(slot1);
            rosterManager.RegisterSlot(slot2);
            slot1.SpawnHero(archerDef);
            slot2.SpawnHero(paladinDef);

            // Add Sanctuary Aura card upgrade (+25% FR, +15% Crit)
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "paladin_sanctuary_aura";
            card.displayName = "Sanctuary Aura";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.SanctuaryAura,
                targetHeroId = "radiant_paladin",
                floatValue = 0.25f
            };
            createdObjects.Add(card);
            RunModifierManager.Instance.AddCard(card);

            HeroAttack archerAttack = slot1.CurrentHero;
            Assert.That(archerAttack.GetModifiedFireRate(), Is.GreaterThanOrEqualTo(1.24f), "Sanctuary Aura must grant +25% fire rate.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Consecration_AbilitySmitesRadiusAndDispelsShields()
        {
            HeroDefinition paladinDef = CreateHeroDef("radiant_paladin", 30f, 1.0f, 12f, HeroAbilityType.Consecration);
            paladinDef.abilityRadius = 6f;
            paladinDef.abilityPowerMultiplier = 2f;

            GameObject paladinGO = new GameObject("Paladin");
            HeroAttack paladinAttack = paladinGO.AddComponent<HeroAttack>();
            paladinAttack.Configure(paladinDef);
            createdObjects.Add(paladinGO);

            Enemy enemy = SpawnEnemy(200f, new Vector3(0f, 0f, 5f), 50f);
            Assert.AreEqual(50f, enemy.CurrentShield, "Enemy must start with 50 shield.");

            paladinAttack.TrySetPriorityTarget(enemy);

            yield return new WaitForSeconds(0.5f);

            Assert.That(enemy.CurrentShield, Is.LessThanOrEqualTo(0f), "Consecration smite must break enemy kinetic shields.");
        }
    }
}
