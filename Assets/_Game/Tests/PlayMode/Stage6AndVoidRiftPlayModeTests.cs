using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class Stage6AndVoidRiftPlayModeTests
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

        private Enemy SpawnEnemyWithRole(EnemySpecialRole role, float health = 300f, Vector3 pos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "test_void_" + role;
            data.enemyName = "Test Void " + role;
            data.health = health;
            data.moveSpeed = 1f;
            data.goldReward = 10;
            data.specialRole = role;
            data.castleDamage = 5;
            createdObjects.Add(data);

            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.AddComponent<CapsuleCollider>();
            Enemy enemy = enemyGo.AddComponent<Enemy>();
            EnemySpecialBehavior special = enemyGo.AddComponent<EnemySpecialBehavior>();
            
            var dataField = enemy.GetType().GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dataField != null) dataField.SetValue(enemy, data);
            createdObjects.Add(enemyGo);

            enemy.PrepareForSpawn(data, pos, Quaternion.identity);
            enemy.ActivateFromPool(new[] { pos, new Vector3(0f, 0f, 0f) }, castle);

            return enemy;
        }

        [UnityTest]
        public IEnumerator VoidStalker_PhaseShiftsDuringLaneTraversal_BecomingUntargetable()
        {
            // Spawn at distance 15m from Castle at 0,0,0
            Enemy stalker = SpawnEnemyWithRole(EnemySpecialRole.VoidPhaseStalker, 150f, new Vector3(0f, 0f, 15f));

            yield return new WaitForSeconds(0.2f);

            EnemySpecialBehavior behavior = stalker.GetComponent<EnemySpecialBehavior>();
            Assert.IsNotNull(behavior);
            Assert.IsTrue(behavior.IsPhaseShifted, "Void Stalker must enter Phase Shift while traversing lane.");
            Assert.IsFalse(stalker.IsTargetable, "Phase shifted stalker must be untargetable by standard targeting.");

            // Move stalker close to castle (< 5m)
            stalker.transform.position = new Vector3(0f, 0f, 4f);

            yield return new WaitForSeconds(0.2f);

            Assert.IsFalse(behavior.IsPhaseShifted, "Void Stalker must drop out of Phase Shift near the castle.");
            Assert.IsTrue(stalker.IsTargetable, "Unshifted stalker must be targetable again.");
        }

        [UnityTest]
        public IEnumerator VoidNullifier_EmitsCleansingPulse_RemovesStatusEffectsFromAllies()
        {
            Enemy nullifier = SpawnEnemyWithRole(EnemySpecialRole.VoidNullifier, 350f, new Vector3(0f, 0f, 50f));
            Enemy ally = SpawnEnemyWithRole(EnemySpecialRole.None, 200f, new Vector3(1f, 0f, 50f));
            nullifier.Data.moveSpeed = 0f;
            ally.Data.moveSpeed = 0f;

            // Apply Burn and Slow to ally
            ally.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 10f, 6f, "fire_mage"));
            ally.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 6f, "frost_mage"));

            Assert.AreEqual(2, ally.GetComponent<StatusEffectController>().ActiveEffects.Count);

            // Wait for nullifier pulse tick (actionTimer = 2.5s)
            yield return new WaitForSeconds(2.7f);

            Assert.AreEqual(0, ally.GetComponent<StatusEffectController>().ActiveEffects.Count, "Void Nullifier must cleanse status effects from nearby allies.");
        }

        [UnityTest]
        public IEnumerator VoidLordBoss_EntersPhase3_ChannelsSupernova()
        {
            Enemy voidLord = SpawnEnemyWithRole(EnemySpecialRole.VoidLordBoss, 1000f, new Vector3(0f, 0f, 8f));

            // Damage void lord down to 20% HP (Phase 3 threshold is <= 25%)
            voidLord.TakeDamage(850f, true);

            yield return new WaitForSeconds(0.2f);

            EnemySpecialBehavior behavior = voidLord.GetComponent<EnemySpecialBehavior>();
            Assert.IsTrue(behavior.IsSupernovaCharging, "Void Lord at < 25% HP must begin charging Void Supernova.");
        }

        [UnityTest]
        public IEnumerator Stage6_DataAsset_LoadsWithCorrectMultipliers()
        {
            StageData stage6 = ScriptableObject.CreateInstance<StageData>();
            stage6.stageId = "stage_6_abyssal_void_rift";
            stage6.stageDisplayName = "Abyssal Void Rift";
            stage6.stageNumber = 6;
            stage6.enemyCountMultiplier = 1.55f;
            stage6.spawnIntervalMultiplier = 0.75f;
            createdObjects.Add(stage6);

            Assert.AreEqual("stage_6_abyssal_void_rift", stage6.stageId);
            Assert.AreEqual(6, stage6.stageNumber);
            Assert.AreEqual(1.55f, stage6.enemyCountMultiplier);
            Assert.AreEqual(0.75f, stage6.spawnIntervalMultiplier);

            yield return null;
        }
    }
}
