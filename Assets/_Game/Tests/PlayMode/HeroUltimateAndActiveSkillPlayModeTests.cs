using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class HeroUltimateAndActiveSkillPlayModeTests
    {
        private GameObject enemyRegistryGO;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
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

            HeroAttack.AutoCastSkills = true;
        }

        [TearDown]
        public void TearDown()
        {
            HeroAttack.AutoCastSkills = true;
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        private Enemy SpawnEnemy(float health = 500f, Vector3 pos = default)
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
        public IEnumerator HeroUltimate_CanBeManuallyTriggered_WhenReady()
        {
            HeroDefinition frostDef = ScriptableObject.CreateInstance<HeroDefinition>();
            frostDef.id = "frost_mage";
            frostDef.displayName = "Frost Mage";
            frostDef.baseDamage = 20f;
            frostDef.baseFireRate = 1f;
            frostDef.baseRange = 12f;
            frostDef.abilityType = HeroAbilityType.FrostNova;
            frostDef.abilityCooldown = 10f;
            frostDef.abilityRadius = 6f;
            frostDef.weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            frostDef.weapon.attackType = AttackType.SingleTarget;
            createdObjects.Add(frostDef.weapon);
            createdObjects.Add(frostDef);

            GameObject heroGO = new GameObject("Hero");
            HeroAttack attack = heroGO.AddComponent<HeroAttack>();
            attack.Configure(frostDef);
            attack.ResetAbilityCooldown();
            createdObjects.Add(heroGO);

            Enemy enemy = SpawnEnemy(300f, new Vector3(0f, 0f, 5f));
            float initialHp = enemy.CurrentHealth;

            // Initially ready
            Assert.IsTrue(attack.IsAbilityReady, "Ability should be ready on initialization.");

            bool triggered = attack.TriggerManualAbility();
            Assert.IsTrue(triggered, "TriggerManualAbility should return true when ready and target exists.");

            yield return new WaitForSeconds(0.4f);

            Assert.IsFalse(attack.IsAbilityReady, "Ability must be on cooldown after manual trigger.");
            Assert.That(attack.AbilityCooldownRemaining, Is.GreaterThan(0f), "Cooldown remaining must be greater than 0.");
            Assert.That(enemy.CurrentHealth, Is.LessThan(initialHp), "Enemy must take damage from Frost Nova.");
        }

        [UnityTest]
        public IEnumerator HeroUltimate_AutoCastToggle_DisablesAutomaticTriggering()
        {
            HeroDefinition frostDef = ScriptableObject.CreateInstance<HeroDefinition>();
            frostDef.id = "frost_mage";
            frostDef.displayName = "Frost Mage";
            frostDef.baseDamage = 20f;
            frostDef.baseFireRate = 1f;
            frostDef.baseRange = 12f;
            frostDef.abilityType = HeroAbilityType.FrostNova;
            frostDef.abilityCooldown = 10f;
            frostDef.weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            frostDef.weapon.attackType = AttackType.SingleTarget;
            createdObjects.Add(frostDef.weapon);
            createdObjects.Add(frostDef);

            GameObject heroGO = new GameObject("Hero");
            HeroAttack attack = heroGO.AddComponent<HeroAttack>();
            attack.Configure(frostDef);
            attack.ResetAbilityCooldown();
            attack.manualCastMode = true; // Set manual mode
            createdObjects.Add(heroGO);

            SpawnEnemy(300f, new Vector3(0f, 0f, 5f));

            yield return new WaitForSeconds(0.5f);

            // In manual mode, it must not auto-fire
            Assert.IsTrue(attack.IsAbilityReady, "In manual mode, ability must remain ready until manually triggered.");
        }
    }
}
