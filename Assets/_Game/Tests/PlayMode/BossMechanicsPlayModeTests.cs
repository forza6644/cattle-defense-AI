using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests.PlayMode
{
    [TestFixture]
    public class BossMechanicsPlayModeTests
    {
        private GameObject testRoot;
        private BossMechanicController bossController;
        private EnvironmentalHazardZone hazardZone;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("BossTestRoot");
            bossController = testRoot.AddComponent<BossMechanicController>();

            GameObject hazardObj = new GameObject("HazardObj");
            hazardObj.transform.SetParent(testRoot.transform);
            hazardZone = hazardObj.AddComponent<EnvironmentalHazardZone>();
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
        public IEnumerator BossMechanicController_HealthThresholds_TransitionsPhases()
        {
            Assert.AreEqual(BossPhase.Phase1_Normal, bossController.currentPhase);

            // Damage to 50% (triggers Phase 2)
            bossController.CheckHealthTransition(500f, 1000f);
            Assert.AreEqual(BossPhase.Phase2_Enraged, bossController.currentPhase);

            // Damage to 20% (triggers Phase 3)
            bossController.CheckHealthTransition(200f, 1000f);
            Assert.AreEqual(BossPhase.Phase3_ApexOverdrive, bossController.currentPhase);
            Assert.AreEqual(bossController.maxKineticShield, bossController.currentKineticShield);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossMechanicController_KineticShield_AbsorbsIncomingDamage()
        {
            bossController.TransitionToPhase(BossPhase.Phase3_ApexOverdrive);
            Assert.AreEqual(250f, bossController.currentKineticShield);

            // 100 damage absorbed completely
            float leftover1 = bossController.AbsorbDamage(100f);
            Assert.AreEqual(0f, leftover1);
            Assert.AreEqual(150f, bossController.currentKineticShield);

            // 200 damage breaks shield (150 absorbed, 50 leftover)
            float leftover2 = bossController.AbsorbDamage(200f);
            Assert.AreEqual(50f, leftover2);
            Assert.AreEqual(0f, bossController.currentKineticShield);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossMechanicController_ExecuteSlamDamage_DamagesEntitiesInRadius()
        {
            GameObject castleGo = new GameObject("TestCastle");
            castleGo.transform.position = testRoot.transform.position + new Vector3(1f, 0f, 0f);
            var col = castleGo.AddComponent<SphereCollider>();
            col.radius = 2f;
            var castle = castleGo.AddComponent<Castle>();

            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.castleMaxHealth = 500;
            var configField = typeof(Castle).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(castle, config);
            var awakeMethod = typeof(Castle).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(castle, null);

            bossController.ExecuteSlamDamage();
            Assert.Less(castle.CurrentHealth, 500);

            Object.DestroyImmediate(castleGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnvironmentalHazardZone_ApplyDirectTick_DealsDamageAndAppliesStatusEffect()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.health = 200f;
            data.enemyName = "HazardTestEnemy";

            GameObject enemyGo = new GameObject("TestHazardEnemy");
            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.PrepareForSpawn(data, Vector3.zero, Quaternion.identity);
            enemy.ActivateFromPool(new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, null);

            hazardZone.damagePerTick = 25f;
            hazardZone.appliedEffect = StatusEffectType.Burn;
            hazardZone.ApplyDirectTickToTarget(enemy);

            var statusCtrl = enemy.GetComponent<StatusEffectController>();
            Assert.IsNotNull(statusCtrl);
            Assert.IsTrue(statusCtrl.HasEffect(StatusEffectType.Burn));

            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(data);
            yield return null;
        }
    }
}
