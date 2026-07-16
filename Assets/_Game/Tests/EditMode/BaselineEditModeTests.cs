using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    public class BaselineEditModeTests
    {
        private GameObject castleObject;
        private GameConfig castleConfig;
        private GameObject enemyObject;
        private EnemyData enemyData;

        [TearDown]
        public void TearDown()
        {
            if (castleObject != null) Object.DestroyImmediate(castleObject);
            if (castleConfig != null) Object.DestroyImmediate(castleConfig);
            if (enemyObject != null) Object.DestroyImmediate(enemyObject);
            if (enemyData != null) Object.DestroyImmediate(enemyData);
            Time.timeScale = 1f;
        }

        [Test]
        public void CastleDamage_RaisesDamageAndHealthEventsOnce()
        {
            Castle castle = CreateCastle(50);
            int damageEvents = 0;
            int healthEvents = 0;
            int appliedDamage = 0;
            castle.DamageTaken += amount => { damageEvents++; appliedDamage = amount; };
            castle.HealthChanged += () => healthEvents++;

            castle.TakeDamage(7);

            Assert.That(castle.CurrentHealth, Is.EqualTo(43));
            Assert.That(damageEvents, Is.EqualTo(1));
            Assert.That(healthEvents, Is.EqualTo(1));
            Assert.That(appliedDamage, Is.EqualTo(7));
        }

        [Test]
        public void CastleHealing_DoesNotRaiseDamageEvent()
        {
            Castle castle = CreateCastle(50);
            int damageEvents = 0;
            int healEvents = 0;
            castle.DamageTaken += _ => damageEvents++;
            castle.Healed += amount => healEvents += amount;
            castle.TakeDamage(10);
            damageEvents = 0;

            castle.Repair(4);

            Assert.That(castle.CurrentHealth, Is.EqualTo(44));
            Assert.That(damageEvents, Is.Zero);
            Assert.That(healEvents, Is.EqualTo(4));
        }

        [Test]
        public void CastleHealth_ClampsAndDefeatRaisesOnce()
        {
            Castle castle = CreateCastle(50);
            int defeated = 0;
            castle.Defeated += () => defeated++;

            castle.TakeDamage(500);
            castle.TakeDamage(1);
            castle.Repair(500);

            Assert.That(castle.CurrentHealth, Is.Zero);
            Assert.That(castle.IsGameOver, Is.True);
            Assert.That(defeated, Is.EqualTo(1));
        }

        [Test]
        public void StatusController_WithNoEffects_IsDisabled()
        {
            StatusEffectController controller = CreateEnemyWithStatusController();

            Assert.That(controller.ActiveEffects, Is.Empty);
            Assert.That(controller.enabled, Is.False);
        }

        [Test]
        public void Burn_LargeDeltaProcessesEveryRequiredTickAndDisables()
        {
            StatusEffectController controller = CreateEnemyWithStatusController();
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            controller.ApplyEffect(new StatusEffect(StatusEffectType.Burn, 10f, 2.5f, "fire_mage"));

            controller.ProcessEffects(2.5f);

            Assert.That(enemy.CurrentHealth, Is.EqualTo(80f).Within(0.001f));
            Assert.That(controller.ActiveEffects, Is.Empty);
            Assert.That(controller.enabled, Is.False);
        }

        [Test]
        public void Slow_ExpiresAndRestoresMovement()
        {
            StatusEffectController controller = CreateEnemyWithStatusController();
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            controller.ApplyEffect(new StatusEffect(StatusEffectType.Slow, 0.4f, 1f));

            Assert.That(enemy.SlowMultiplier, Is.EqualTo(0.4f).Within(0.001f));
            controller.ProcessEffects(1.1f);

            Assert.That(enemy.SlowMultiplier, Is.EqualTo(1f).Within(0.001f));
            Assert.That(controller.enabled, Is.False);
        }

        [Test]
        public void Shock_ReapplyRefreshesAndExpires()
        {
            StatusEffectController controller = CreateEnemyWithStatusController();
            controller.ApplyEffect(new StatusEffect(StatusEffectType.Shock, 1f, 1f));
            controller.ProcessEffects(0.75f);
            controller.ApplyEffect(new StatusEffect(StatusEffectType.Shock, 1f, 1f));

            controller.ProcessEffects(0.5f);
            Assert.That(controller.IsShocked(), Is.True);
            controller.ProcessEffects(0.6f);

            Assert.That(controller.IsShocked(), Is.False);
            Assert.That(controller.enabled, Is.False);
        }

        [Test]
        public void PooledParticleState_ClearsHierarchyAndRejectsDoubleReturn()
        {
            GameObject pool = new GameObject("Pool");
            GameObject root = new GameObject("Effect");
            ParticleSystem rootParticle = root.AddComponent<ParticleSystem>();
            GameObject child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            ParticleSystem childParticle = child.AddComponent<ParticleSystem>();
            TrailRenderer trail = child.AddComponent<TrailRenderer>();
            PooledParticleState state = PooledParticleState.GetOrCreate(rootParticle);
            rootParticle.Emit(4);
            childParticle.Emit(3);

            state.PrepareForPlay(false);
            rootParticle.Emit(2);
            childParticle.Emit(2);
            bool firstReturn = state.TryReturnCurrent(pool.transform);
            bool secondReturn = state.TryReturnCurrent(pool.transform);

            Assert.That(firstReturn, Is.True);
            Assert.That(secondReturn, Is.False);
            Assert.That(rootParticle.particleCount, Is.Zero);
            Assert.That(childParticle.particleCount, Is.Zero);
            Assert.That(trail.positionCount, Is.Zero);
            Assert.That(root.activeSelf, Is.False);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(pool);
        }

        [Test]
        public void PooledParticleState_StaleActivationCannotReturnReusedEffect()
        {
            GameObject pool = new GameObject("Pool");
            GameObject root = new GameObject("Effect");
            ParticleSystem particle = root.AddComponent<ParticleSystem>();
            PooledParticleState state = PooledParticleState.GetOrCreate(particle);
            int oldActivation = state.PrepareForPlay(false);
            int currentActivation = state.PrepareForPlay(true);

            Assert.That(state.TryReturn(oldActivation, pool.transform), Is.False);
            Assert.That(state.TryReturn(currentActivation, pool.transform), Is.True);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(pool);
        }

        private Castle CreateCastle(int maxHealth)
        {
            castleConfig = ScriptableObject.CreateInstance<GameConfig>();
            castleConfig.castleMaxHealth = maxHealth;
            castleObject = new GameObject("Castle Test");
            castleObject.SetActive(false);
            Castle castle = castleObject.AddComponent<Castle>();
            SetPrivateField(castle, "config", castleConfig);
            InvokeLifecycle(castle, "Awake");
            castleObject.SetActive(true);
            return castle;
        }

        private StatusEffectController CreateEnemyWithStatusController()
        {
            enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.health = 100f;
            enemyData.moveSpeed = 1f;
            enemyObject = new GameObject("Enemy Test");
            enemyObject.SetActive(false);
            Enemy enemy = enemyObject.AddComponent<Enemy>();
            SetPrivateField(enemy, "data", enemyData);
            SetPrivateField(enemy, "currentHealth", enemyData.health);
            StatusEffectController controller = enemyObject.AddComponent<StatusEffectController>();
            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "OnEnable");
            enemyObject.SetActive(true);
            return controller;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing test field: " + fieldName);
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing lifecycle method: " + methodName);
            method.Invoke(target, null);
        }
    }
}
