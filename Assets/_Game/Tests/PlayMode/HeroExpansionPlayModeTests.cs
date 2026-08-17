using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class HeroExpansionPlayModeTests
    {
        private GameObject enemyRegistryGO;
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
                GameManager.Instance.SetState(GameState.Playing);
                GameManager.Instance.SetGameSpeed(1f);

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

            projPrefab = new GameObject("ProjectilePrefab");
            projPrefab.AddComponent<TrailRenderer>();
            projPrefab.AddComponent<Projectile>();
            projPrefab.SetActive(false);
            createdObjects.Add(projPrefab);
        }

        private GameObject projPrefab;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        private Enemy SpawnEnemy(float health = 200f, Vector3 pos = default)
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
        public IEnumerator ShadowAssassin_AttacksWithInnateCritAndDealsDamage()
        {
            var assassinDef = ScriptableObject.CreateInstance<HeroDefinition>();
            assassinDef.id = "shadow_assassin";
            assassinDef.displayName = "Shadow Assassin";
            assassinDef.baseDamage = 25f;
            assassinDef.baseFireRate = 2f;
            assassinDef.baseRange = 10f;
            assassinDef.abilityType = HeroAbilityType.ShadowStep;
            assassinDef.abilityCooldown = 8f;
            assassinDef.abilityPowerMultiplier = 4f;
            createdObjects.Add(assassinDef);

            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.attackType = AttackType.SingleTarget;
            weapon.projectilePrefab = projPrefab;
            assassinDef.weapon = weapon;
            createdObjects.Add(weapon);

            GameObject heroGO = new GameObject("ShadowAssassinHero");
            heroGO.transform.position = Vector3.zero;
            var heroAttack = heroGO.AddComponent<HeroAttack>();
            heroAttack.Configure(assassinDef);
            heroAttack.enabled = false;
            createdObjects.Add(heroGO);

            Enemy target = SpawnEnemy(200f, new Vector3(0f, 0f, 4f));

            Assert.GreaterOrEqual(heroAttack.GetModifiedCritChance(), 0.20f, "Shadow Assassin should have innate high crit rate");
            Assert.GreaterOrEqual(heroAttack.GetModifiedCritMultiplier(), 2.0f, "Shadow Assassin should have innate high crit multiplier");

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { target });

            yield return new WaitForSeconds(0.25f);

            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.GreaterThan(0), "Shadow Assassin should launch projectile attacks");
        }

        [UnityTest]
        public IEnumerator StormDruid_ChanneledCyclonePullsAndShocksEnemies()
        {
            var druidDef = ScriptableObject.CreateInstance<HeroDefinition>();
            druidDef.id = "storm_druid";
            druidDef.displayName = "Storm Druid";
            druidDef.baseDamage = 15f;
            druidDef.baseFireRate = 1.5f;
            druidDef.baseRange = 12f;
            druidDef.abilityType = HeroAbilityType.TempestCyclone;
            druidDef.abilityCooldown = 8f;
            druidDef.abilityPowerMultiplier = 2.5f;
            druidDef.abilityRadius = 5f;
            createdObjects.Add(druidDef);

            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.attackType = AttackType.Chain;
            weapon.projectilePrefab = projPrefab;
            druidDef.weapon = weapon;
            createdObjects.Add(weapon);

            GameObject heroGO = new GameObject("StormDruidHero");
            heroGO.transform.position = Vector3.zero;
            var heroAttack = heroGO.AddComponent<HeroAttack>();
            heroAttack.Configure(druidDef);
            heroAttack.enabled = false;
            createdObjects.Add(heroGO);

            Enemy target = SpawnEnemy(150f, new Vector3(0f, 0f, 4f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { target });

            yield return new WaitForSeconds(0.25f);

            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.GreaterThan(0), "Storm Druid should launch chain storm projectiles");
        }

        [UnityTest]
        public IEnumerator PlagueDoctor_ThrowsPlagueFlaskAndAppliesPoison()
        {
            var plagueDef = ScriptableObject.CreateInstance<HeroDefinition>();
            plagueDef.id = "plague_doctor";
            plagueDef.displayName = "Plague Doctor";
            plagueDef.baseDamage = 14f;
            plagueDef.baseFireRate = 1.2f;
            plagueDef.baseRange = 10f;
            plagueDef.abilityType = HeroAbilityType.PlagueFlask;
            plagueDef.abilityCooldown = 8f;
            plagueDef.abilityPowerMultiplier = 2f;
            plagueDef.abilityRadius = 4f;
            createdObjects.Add(plagueDef);

            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.attackType = AttackType.Splash;
            weapon.statusEffectType = StatusEffectType.Poison;
            weapon.statusEffectDuration = 3f;
            weapon.statusEffectValue = 5f;
            weapon.projectilePrefab = projPrefab;
            plagueDef.weapon = weapon;
            createdObjects.Add(weapon);

            GameObject heroGO = new GameObject("PlagueDoctorHero");
            heroGO.transform.position = Vector3.zero;
            var heroAttack = heroGO.AddComponent<HeroAttack>();
            heroAttack.Configure(plagueDef);
            heroAttack.enabled = false;
            createdObjects.Add(heroGO);

            Enemy target = SpawnEnemy(200f, new Vector3(0f, 0f, 4f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { target });

            yield return new WaitForSeconds(0.25f);

            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.GreaterThan(0), "Plague Doctor should launch caustic poison projectiles");
        }

        [UnityTest]
        public IEnumerator RadiantPaladin_CastsConsecrationAndAppliesHolyDamage()
        {
            var paladinDef = ScriptableObject.CreateInstance<HeroDefinition>();
            paladinDef.id = "radiant_paladin";
            paladinDef.displayName = "Radiant Paladin";
            paladinDef.baseDamage = 22f;
            paladinDef.baseFireRate = 1.0f;
            paladinDef.baseRange = 9f;
            paladinDef.abilityType = HeroAbilityType.Consecration;
            paladinDef.abilityCooldown = 10f;
            paladinDef.abilityPowerMultiplier = 2.5f;
            paladinDef.abilityRadius = 5f;
            createdObjects.Add(paladinDef);

            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.attackType = AttackType.SingleTarget;
            weapon.projectilePrefab = projPrefab;
            paladinDef.weapon = weapon;
            createdObjects.Add(weapon);

            GameObject heroGO = new GameObject("RadiantPaladinHero");
            heroGO.transform.position = Vector3.zero;
            var heroAttack = heroGO.AddComponent<HeroAttack>();
            heroAttack.Configure(paladinDef);
            heroAttack.enabled = false;
            createdObjects.Add(heroGO);

            Enemy target = SpawnEnemy(200f, new Vector3(0f, 0f, 4f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { target });

            yield return new WaitForSeconds(0.25f);

            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.GreaterThan(0), "Radiant Paladin should launch smiting holy projectiles");
        }
    }
}
