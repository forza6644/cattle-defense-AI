using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Stonehold.Tests
{
    public class EnemyPoolingPlayModeTests
    {
        private GameObject poolObject;
        private GameObject registryObject;
        private EnemyPoolManager pool;
        private EnemyData data;
        private GameObject prefab;
        private readonly List<GameObject> createdPrefabs = new List<GameObject>();
        private Castle castle;
        private GameObject castleObject;
        private GameConfig castleConfig;
        private int keySequence;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            registryObject = new GameObject("Enemy Registry Test");
            registryObject.AddComponent<EnemyManager>();
            poolObject = new GameObject("Enemy Pool Test");
            pool = poolObject.AddComponent<EnemyPoolManager>();
            data = CreateEnemyData(false);
            castle = CreateCastle();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (poolObject != null) Object.DestroyImmediate(poolObject);
            if (registryObject != null) Object.DestroyImmediate(registryObject);
            for (int i = 0; i < createdPrefabs.Count; i++)
            {
                if (createdPrefabs[i] != null) Object.DestroyImmediate(createdPrefabs[i]);
            }
            createdPrefabs.Clear();
            if (data != null) Object.DestroyImmediate(data);
            if (castleObject != null) Object.DestroyImmediate(castleObject);
            if (castleConfig != null) Object.DestroyImmediate(castleConfig);
        }

        [Test]
        public void Pool_ReusesSameInstanceAfterDespawn()
        {
            pool.EnsurePool(data, 1);
            Enemy first = Spawn();
            int activation = first.ActivationId;
            Assert.That(pool.Despawn(first, activation), Is.True);
            Enemy second = Spawn();
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.ActivationId, Is.Not.EqualTo(activation));
        }

        [Test]
        public void Spawn_ResetsHealthToEnemyDataHealth()
        {
            Enemy enemy = Spawn();
            enemy.TakeDamage(25f, true);
            pool.Despawn(enemy, enemy.ActivationId);
            enemy = Spawn();
            Assert.That(enemy.CurrentHealth, Is.EqualTo(data.health));
        }

        [Test]
        public void Spawn_ResetsDeadAndRewardState()
        {
            int kills = 0;
            Action<Enemy, int> handler = (_, __) => kills++;
            Enemy.AnyKilled += handler;
            try
            {
                Enemy enemy = Spawn();
                enemy.Kill();
                enemy = Spawn();
                enemy.Kill();
                Assert.That(kills, Is.EqualTo(2));
            }
            finally
            {
                Enemy.AnyKilled -= handler;
            }
        }

        [Test]
        public void Spawn_ResetsAllStatusEffects()
        {
            pool.EnsurePool(data, 1);
            Enemy enemy = Spawn();
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 10f, "test"));
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 2f, 10f, "test"));
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 10f, "test"));
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Stun, 0f, 10f, "test"));
            StatusEffectController controller = enemy.GetComponent<StatusEffectController>();
            Assert.That(controller.ActiveEffects.Count, Is.GreaterThan(0));
            pool.Despawn(enemy, enemy.ActivationId);
            enemy = Spawn();
            controller = enemy.GetComponent<StatusEffectController>();
            Assert.That(controller.ActiveEffects, Is.Empty);
            Assert.That(enemy.SlowMultiplier, Is.EqualTo(1f));
            Assert.That(enemy.SlowTimer, Is.Zero);
        }

        [Test]
        public void Spawn_RestoresMovementAndColliders()
        {
            Enemy enemy = Spawn();
            Collider collider = enemy.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.enabled, Is.True);
            pool.Despawn(enemy, enemy.ActivationId);
            enemy = Spawn();
            Assert.That(enemy.enabled, Is.True);
            Assert.That(enemy.GetComponent<Collider>().enabled, Is.True);
        }

        [Test]
        public void Spawn_AssignsTargetAndPath()
        {
            Enemy enemy = Spawn(new[] { new Vector3(0f, 0f, 4f), Vector3.zero });
            Assert.That(enemy.RemainingDistanceToTarget, Is.LessThan(float.PositiveInfinity));
            Assert.That(enemy.IsActiveActivation, Is.True);
        }

        [Test]
        public void ActiveEnemy_RegistersExactlyOnce()
        {
            Enemy enemy = Spawn();
            Assert.That(EnemyManager.AliveCount, Is.EqualTo(1));
            EnemyManager.Register(enemy);
            Assert.That(EnemyManager.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void Despawn_UnregistersExactlyOnce()
        {
            Enemy enemy = Spawn();
            int activation = enemy.ActivationId;
            Assert.That(pool.Despawn(enemy, activation), Is.True);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(pool.Despawn(enemy, activation), Is.False);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
        }

        [Test]
        public void DuplicateDespawn_IsRejectedAndDiagnosed()
        {
            Enemy enemy = Spawn();
            string key = data.stableId;
            int activation = enemy.ActivationId;
            pool.Despawn(enemy, activation);
            Assert.That(pool.Despawn(enemy, activation), Is.False);
            Assert.That(pool.TryGetDiagnostics(key, out EnemyPoolManager.PoolDiagnostics diagnostics), Is.True);
            Assert.That(diagnostics.InvalidReturns, Is.EqualTo(1));
        }

        [Test]
        public void CombatDeath_AwardsKillEventExactlyOnce()
        {
            int kills = 0;
            Action<Enemy, int> handler = (_, reward) => { kills++; Assert.That(reward, Is.EqualTo(data.goldReward)); };
            Enemy.AnyKilled += handler;
            try
            {
                Enemy enemy = Spawn();
                enemy.TakeDamage(data.health * 2f, true);
                enemy.Kill();
                Assert.That(kills, Is.EqualTo(1));
                Assert.That(EnemyManager.AliveCount, Is.Zero);
            }
            finally
            {
                Enemy.AnyKilled -= handler;
            }
        }

        [UnityTest]
        public IEnumerator CastleArrival_DamagesOnceAndAwardsNoKillReward()
        {
            int kills = 0;
            Action<Enemy, int> handler = (_, __) => kills++;
            Enemy.AnyKilled += handler;
            try
            {
                int before = castle.CurrentHealth;
                Spawn(new[] { castle.transform.position });
                yield return null;
                yield return null;
                Assert.That(castle.CurrentHealth, Is.EqualTo(before - data.castleDamage));
                Assert.That(kills, Is.Zero);
                Assert.That(EnemyManager.AliveCount, Is.Zero);
            }
            finally
            {
                Enemy.AnyKilled -= handler;
            }
        }

        [Test]
        public void AliveCount_RemainsCorrectAcrossRepeatedCycles()
        {
            for (int i = 0; i < 12; i++)
            {
                Enemy enemy = Spawn();
                Assert.That(EnemyManager.AliveCount, Is.EqualTo(1));
                pool.Despawn(enemy, enemy.ActivationId);
                Assert.That(EnemyManager.AliveCount, Is.Zero);
            }
        }

        [Test]
        public void RestartCleanup_DespawnsAllActiveEnemies()
        {
            Spawn();
            Spawn();
            Spawn();
            Assert.That(EnemyManager.AliveCount, Is.EqualTo(3));
            pool.DespawnAllActive();
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [Test]
        public void SceneStyleLifecycle_LeavesNoRegistryEntries()
        {
            Spawn();
            Spawn();
            Object.DestroyImmediate(poolObject);
            poolObject = null;
            Assert.That(EnemyManager.PruneInvalidEntries(), Is.EqualTo(0));
            Assert.That(EnemyManager.AliveCount, Is.Zero);
        }

        [Test]
        public void WarlordBoss_CanBePooledAndReused()
        {
            ReplaceData(CreateEnemyData(true));
            Enemy first = Spawn();
            pool.Despawn(first, first.ActivationId);
            Enemy second = Spawn();
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Data.classification, Is.EqualTo(EnemyClassification.Boss));
        }

        [UnityTest]
        public IEnumerator OldProjectileToken_CannotDamageReusedEnemy()
        {
            pool.EnsurePool(data, 1);
            Enemy enemy = Spawn(new[] { new Vector3(0f, 0f, 3f), Vector3.zero });
            GameObject projectilePrefab = new GameObject("Pooling Projectile Test Prefab");
            projectilePrefab.AddComponent<TrailRenderer>();
            projectilePrefab.AddComponent<Projectile>();
            Projectile projectile = Projectile.Spawn(projectilePrefab, enemy.transform.position + Vector3.right * 0.05f);
            projectile.Init(enemy, 30f, 0f, 1f, 0f, Color.white, "test");
            int oldActivation = enemy.ActivationId;
            pool.Despawn(enemy, oldActivation);
            Enemy reused = Spawn();
            Assert.That(reused, Is.SameAs(enemy));
            yield return null;
            Assert.That(reused.CurrentHealth, Is.EqualTo(data.health));
            Object.Destroy(projectilePrefab);
        }

        [Test]
        public void OldDeathCallback_CannotDespawnReusedEnemy()
        {
            ReplaceData(CreateEnemyData(false, true));
            pool.EnsurePool(data, 1);
            Enemy enemy = Spawn();
            int oldActivation = enemy.ActivationId;
            enemy.Kill();
            Assert.That(pool.Despawn(enemy, oldActivation), Is.True);
            Enemy reused = Spawn();
            Assert.That(pool.Despawn(reused, oldActivation), Is.False);
            Assert.That(reused.IsActiveActivation, Is.True);
            Assert.That(EnemyManager.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void PreviousActivationStatus_DoesNotSurviveReuse()
        {
            pool.EnsurePool(data, 1);
            Enemy enemy = Spawn();
            enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 4f, 30f, "old"));
            int oldActivation = enemy.ActivationId;
            pool.Despawn(enemy, oldActivation);
            Enemy reused = Spawn();
            Assert.That(reused.GetComponent<StatusEffectController>().ActiveEffects, Is.Empty);
            Assert.That(reused.ActivationId, Is.Not.EqualTo(oldActivation));
        }

        [Test]
        public void Pool_ExpandsWhenCapacityIsExhausted()
        {
            Assert.That(pool.EnsurePool(data, 0), Is.True);
            Spawn();
            Spawn();
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics diagnostics), Is.True);
            Assert.That(diagnostics.Created, Is.EqualTo(2));
            Assert.That(diagnostics.Expansions, Is.EqualTo(2));
            Assert.That(diagnostics.PeakActive, Is.EqualTo(2));
        }

        [Test]
        public void PrewarmedStressCycle_ReusesWithoutNewInstances()
        {
            Assert.That(pool.EnsurePool(data, 4), Is.True);
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics before), Is.True);
            for (int i = 0; i < 100; i++)
            {
                Enemy enemy = Spawn();
                enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, 0.5f, 1f, "stress"));
                pool.Despawn(enemy, enemy.ActivationId);
            }
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics after), Is.True);
            Assert.That(after.Created, Is.EqualTo(before.Created));
            Assert.That(after.Expansions, Is.EqualTo(before.Expansions));
            Assert.That(after.ReuseCount, Is.GreaterThanOrEqualTo(100));
            Assert.That(after.InvalidReturns, Is.Zero);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
        }

        private Enemy Spawn(Vector3[] path = null)
        {
            return pool.Spawn(
                data,
                new Vector3(0f, 0f, 4f),
                Quaternion.identity,
                path ?? DefaultPath(),
                castle,
                0f,
                0f);
        }

        private static Vector3[] DefaultPath()
        {
            return new[] { new Vector3(0f, 0f, 4f), Vector3.zero };
        }

        private EnemyData CreateEnemyData(bool boss, bool withAnimator = false)
        {
            keySequence++;
            EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.stableId = (boss ? "boss" : "grunt") + "-pool-test-" + keySequence;
            enemyData.enemyName = boss ? "Warlord Boss" : "Grunt";
            enemyData.classification = boss ? EnemyClassification.Boss : EnemyClassification.Normal;
            enemyData.health = 100f;
            enemyData.moveSpeed = 1f;
            enemyData.goldReward = 7;
            enemyData.xpValue = 5;
            enemyData.castleDamage = 3;

            prefab = new GameObject(enemyData.enemyName + " Prefab");
            createdPrefabs.Add(prefab);
            prefab.AddComponent<CapsuleCollider>();
            if (withAnimator)
            {
                prefab.AddComponent<ProceduralAnimator>();
            }
            prefab.AddComponent<Enemy>();
            enemyData.prefab = prefab;
            return enemyData;
        }

        private void ReplaceData(EnemyData replacement)
        {
            if (data != null) Object.DestroyImmediate(data);
            data = replacement;
        }

        private Castle CreateCastle()
        {
            castleConfig = ScriptableObject.CreateInstance<GameConfig>();
            castleConfig.castleMaxHealth = 50;
            castleObject = new GameObject("Castle Pool Test");
            castleObject.SetActive(false);
            Castle result = castleObject.AddComponent<Castle>();
            SetPrivateField(result, "config", castleConfig);
            castleObject.SetActive(true);
            return result;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }
    }
}
