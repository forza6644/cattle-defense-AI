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
    public sealed class EnemyRosterExpansionPlayModeTests
    {
        private readonly List<Object> created = new List<Object>();
        private GameObject registryObject;
        private GameObject poolObject;
        private EnemyPoolManager pool;
        private Castle castle;
        private int sequence;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
                GameManager.Instance.SetGameSpeed(1f);
            }
            registryObject = Track(new GameObject("Task13E Registry"));
            registryObject.AddComponent<EnemyManager>();
            poolObject = Track(new GameObject("Task13E Pool"));
            pool = poolObject.AddComponent<EnemyPoolManager>();
            castle = CreateCastle(200);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            EnemyCastleProjectile.DespawnAllActive();
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        [Test]
        public void CrossbowRaider_DefaultPoolPrewarmsAndReuses()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            Assert.That(pool.EnsurePool(data), Is.True);
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics before), Is.True);
            Assert.That(before.Created, Is.EqualTo(3));
            Enemy first = Spawn(data, new Vector3(0f, 0f, 8f));
            pool.Despawn(first, first.ActivationId);
            Enemy second = Spawn(data, new Vector3(0f, 0f, 8f));
            Assert.That(second, Is.Not.Null);
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics after), Is.True);
            Assert.That(after.Created, Is.EqualTo(before.Created));
            Assert.That(after.ReuseCount, Is.EqualTo(2));
        }

        [Test]
        public void WarShaman_DefaultPoolPrewarmIsOneAndReuses()
        {
            EnemyData data = CreateData(EnemySpecialRole.HealingElite);
            Assert.That(pool.EnsurePool(data), Is.True);
            Assert.That(pool.TryGetDiagnostics(data.stableId, out EnemyPoolManager.PoolDiagnostics before), Is.True);
            Assert.That(before.Created, Is.EqualTo(1));
            Enemy first = Spawn(data, new Vector3(0f, 0f, 7f));
            pool.Despawn(first, first.ActivationId);
            Assert.That(Spawn(data, new Vector3(0f, 0f, 7f)), Is.SameAs(first));
        }

        [UnityTest]
        public IEnumerator CrossbowRaider_StopsAtStandOffRangeAndStartsWindUp()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            data.moveSpeed = 30f;
            data.rangedAttack.windUpSeconds = 5f;
            Enemy raider = Spawn(data, new Vector3(0f, 0f, 8f));
            for (int i = 0; i < 20; i++) yield return null;
            float distance = Vector3.Distance(raider.transform.position, castle.transform.position);
            Assert.That(distance, Is.GreaterThanOrEqualTo(data.rangedAttack.standOffRange - 0.35f));
        }

        [UnityTest]
        public IEnumerator CrossbowWindUp_IsCancelledOnDeath()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            data.rangedAttack.windUpSeconds = 5f;
            Enemy raider = Spawn(data, new Vector3(0f, 0f, 5f));
            EnemySpecialBehavior behavior = raider.GetComponent<EnemySpecialBehavior>();
            behavior.ForceActionReadyForTests();
            behavior.Tick();
            Assert.That(behavior.IsWindingUp, Is.True);
            raider.Kill();
            Assert.That(behavior.IsWindingUp, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossbowProjectile_DamagesCastleExactlyOnce()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            data.rangedAttack.windUpSeconds = 0.01f;
            data.rangedAttack.projectileSpeed = 100f;
            int before = castle.CurrentHealth;
            Enemy raider = Spawn(data, new Vector3(0f, 0f, 5f));
            FireImmediately(raider);
            yield return new WaitForSeconds(0.75f);
            Assert.That(castle.CurrentHealth, Is.EqualTo(before - data.castleDamage));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(castle.CurrentHealth, Is.EqualTo(before - data.castleDamage));
        }

        [UnityTest]
        public IEnumerator StaleRangedProjectile_CannotDamageAfterSourceReuse()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            Enemy raider = Spawn(data, new Vector3(0f, 0f, 7f));
            EnemyCastleProjectile projectile = EnemyCastleProjectile.Spawn(data.rangedAttack.projectilePrefab, new Vector3(0f, 0f, 6f));
            projectile.Initialize(raider, raider.ActivationId, castle, 20, 1f);
            int before = castle.CurrentHealth;
            int oldActivation = raider.ActivationId;
            pool.Despawn(raider, oldActivation);
            Enemy reused = Spawn(data, new Vector3(0f, 0f, 7f));
            yield return null;
            Assert.That(reused.ActivationId, Is.Not.EqualTo(oldActivation));
            Assert.That(castle.CurrentHealth, Is.EqualTo(before));
            Assert.That(EnemyCastleProjectile.StaleHitCount, Is.Zero);
        }

        [Test]
        public void RangedProjectile_StateResetsOnReturn()
        {
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            Enemy raider = Spawn(data, new Vector3(0f, 0f, 7f));
            EnemyCastleProjectile first = EnemyCastleProjectile.Spawn(data.rangedAttack.projectilePrefab, Vector3.zero);
            first.Initialize(raider, raider.ActivationId, castle, 20, 5f);
            first.ReturnToPool();
            EnemyCastleProjectile second = EnemyCastleProjectile.Spawn(data.rangedAttack.projectilePrefab, Vector3.one);
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.HasHit, Is.False);
        }

        [Test]
        public void WarShaman_CastIsTelegraphed()
        {
            Enemy shaman = Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(0f, 0f, 5f));
            EnemySpecialBehavior behavior = shaman.GetComponent<EnemySpecialBehavior>();
            behavior.ForceActionReadyForTests();
            Assert.That(behavior.Tick(), Is.True);
            Assert.That(behavior.IsCasting, Is.True);
        }

        [Test]
        public void WarShaman_HealsDamagedNearbyEnemyWithoutOverheal()
        {
            EnemyData shamanData = CreateData(EnemySpecialRole.HealingElite);
            EnemyData targetData = CreateData(EnemySpecialRole.None);
            Enemy shaman = Spawn(shamanData, new Vector3(0f, 0f, 5f));
            Enemy target = Spawn(targetData, new Vector3(1f, 0f, 5f));
            target.TakeDamage(80f, true);
            float before = target.CurrentHealth;
            ExecutePulse(shaman);
            Assert.That(target.CurrentHealth, Is.GreaterThan(before));
            Assert.That(target.CurrentHealth, Is.LessThanOrEqualTo(target.MaxHealth));
        }

        [Test]
        public void WarShaman_IgnoresFullHealthTargets()
        {
            Enemy shaman = Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(0f, 0f, 5f));
            Spawn(CreateData(EnemySpecialRole.None), new Vector3(1f, 0f, 5f));
            ExecutePulse(shaman);
            Assert.That(shaman.GetComponent<EnemySpecialBehavior>().LastHealCount, Is.Zero);
        }

        [Test]
        public void WarShaman_ExcludesWarlordBoss()
        {
            Enemy shaman = Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(0f, 0f, 5f));
            EnemyData bossData = CreateData(EnemySpecialRole.None, EnemyClassification.Boss);
            Enemy boss = Spawn(bossData, new Vector3(1f, 0f, 5f));
            boss.TakeDamage(50f, true);
            float before = boss.CurrentHealth;
            ExecutePulse(shaman);
            Assert.That(boss.CurrentHealth, Is.EqualTo(before));
        }

        [Test]
        public void WarShaman_CannotReviveDeadOrPooledEnemy()
        {
            Enemy shaman = Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(0f, 0f, 5f));
            Enemy target = Spawn(CreateData(EnemySpecialRole.None), new Vector3(1f, 0f, 5f));
            target.Kill();
            ExecutePulse(shaman);
            Assert.That(target.IsTargetable, Is.False);
        }

        [Test]
        public void WarShaman_CastCancelsOnPoolReturnAndReuse()
        {
            EnemyData data = CreateData(EnemySpecialRole.HealingElite);
            Enemy shaman = Spawn(data, new Vector3(0f, 0f, 5f));
            EnemySpecialBehavior behavior = shaman.GetComponent<EnemySpecialBehavior>();
            behavior.ForceActionReadyForTests();
            behavior.Tick();
            int oldActivation = shaman.ActivationId;
            pool.Despawn(shaman, oldActivation);
            shaman = Spawn(data, new Vector3(0f, 0f, 5f));
            behavior = shaman.GetComponent<EnemySpecialBehavior>();
            Assert.That(behavior.IsCasting, Is.False);
            Assert.That(behavior.BoundActivationId, Is.EqualTo(shaman.ActivationId));
            Assert.That(shaman.ActivationId, Is.Not.EqualTo(oldActivation));
        }

        [Test]
        public void WarShaman_DoesNotRetainOldHealTargetsAfterReuse()
        {
            EnemyData data = CreateData(EnemySpecialRole.HealingElite);
            EnemyData targetData = CreateData(EnemySpecialRole.None);
            Enemy shaman = Spawn(data, new Vector3(0f, 0f, 5f));
            Enemy target = Spawn(targetData, new Vector3(1f, 0f, 5f));
            target.TakeDamage(50f, true);
            ExecutePulse(shaman);
            pool.Despawn(shaman, shaman.ActivationId);
            shaman = Spawn(data, new Vector3(0f, 0f, 5f));
            target.transform.position = new Vector3(100f, 0f, 100f);
            float before = target.CurrentHealth;
            ExecutePulse(shaman);
            Assert.That(target.CurrentHealth, Is.EqualTo(before));
        }

        [TestCase("archer")]
        [TestCase("bombardier")]
        [TestCase("frost_mage")]
        [TestCase("fire_mage")]
        [TestCase("electric_engineer")]
        [TestCase("sniper")]
        public void AllHeroDamageSources_CanDamageBothExpansionEnemies(string heroId)
        {
            GameObject trackerObject = Track(new GameObject("Damage Tracker " + heroId));
            DamageTracker tracker = trackerObject.AddComponent<DamageTracker>();
            Enemy raider = Spawn(CreateData(EnemySpecialRole.RangedCastleAttacker), new Vector3(-1f, 0f, 7f));
            Enemy shaman = Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(1f, 0f, 7f));
            float raiderDamage = raider.TakeDamage(2f, true);
            float shamanDamage = shaman.TakeDamage(2f, true);
            DamageTracker.RecordDamage(heroId, raiderDamage + shamanDamage);
            Assert.That(raider.CurrentHealth, Is.LessThan(raider.MaxHealth));
            Assert.That(shaman.CurrentHealth, Is.LessThan(shaman.MaxHealth));
            Assert.That(tracker.DamageByHeroId[heroId], Is.EqualTo(4f));
        }

        [Test]
        public void StatusEffectsAndRewards_ResetAndAwardOnce()
        {
            EnemyData data = CreateData(EnemySpecialRole.HealingElite);
            int kills = 0;
            Action<Enemy, int> handler = (_, __) => kills++;
            Enemy.AnyKilled += handler;
            try
            {
                Enemy enemy = Spawn(data, new Vector3(0f, 0f, 7f));
                enemy.ApplyStatusEffect(new StatusEffect(StatusEffectType.Burn, 2f, 10f, "fire_mage"));
                enemy.Kill();
                enemy.Kill();
                Assert.That(kills, Is.EqualTo(1));
                Enemy reused = Spawn(data, new Vector3(0f, 0f, 7f));
                Assert.That(reused.GetComponent<StatusEffectController>().ActiveEffects, Is.Empty);
            }
            finally { Enemy.AnyKilled -= handler; }
        }

        [Test]
        public void RestartCleanup_LeavesNoExpansionEnemiesOrProjectiles()
        {
            EnemyData raiderData = CreateData(EnemySpecialRole.RangedCastleAttacker);
            Enemy raider = Spawn(raiderData, new Vector3(0f, 0f, 7f));
            EnemyCastleProjectile projectile = EnemyCastleProjectile.Spawn(raiderData.rangedAttack.projectilePrefab, Vector3.zero);
            projectile.Initialize(raider, raider.ActivationId, castle, 2, 1f);
            Spawn(CreateData(EnemySpecialRole.HealingElite), new Vector3(1f, 0f, 7f));
            pool.DespawnAllActive();
            EnemyCastleProjectile.DespawnAllActive();
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.Zero);
        }

        [Test]
        public void ActivationId_WrapRemainsUniqueForExpansionEnemies()
        {
            FieldInfo counter = typeof(Enemy).GetField("globalActivationCounter", BindingFlags.Static | BindingFlags.NonPublic);
            counter.SetValue(null, int.MaxValue - 1);
            EnemyData data = CreateData(EnemySpecialRole.RangedCastleAttacker);
            Enemy first = Spawn(data, new Vector3(-1f, 0f, 7f));
            Enemy second = Spawn(data, new Vector3(1f, 0f, 7f));
            Assert.That(first.ActivationId, Is.EqualTo(int.MaxValue));
            Assert.That(second.ActivationId, Is.EqualTo(1));
            Assert.That(first.ActivationId, Is.Not.EqualTo(second.ActivationId));
        }

        [Test]
        public void PoolStress_500RaidersAnd200Shamans_CleansCompletely()
        {
            EnemyData raiderData = CreateData(EnemySpecialRole.RangedCastleAttacker);
            EnemyData shamanData = CreateData(EnemySpecialRole.HealingElite);
            pool.EnsurePool(raiderData, 4);
            pool.EnsurePool(shamanData, 1);
            for (int i = 0; i < 500; i++)
            {
                Enemy enemy = Spawn(raiderData, new Vector3(0f, 0f, 8f));
                pool.Despawn(enemy, enemy.ActivationId);
            }
            for (int i = 0; i < 200; i++)
            {
                Enemy enemy = Spawn(shamanData, new Vector3(0f, 0f, 8f));
                pool.Despawn(enemy, enemy.ActivationId);
            }
            AssertDiagnostics(raiderData.stableId, 500);
            AssertDiagnostics(shamanData.stableId, 200);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.Zero);
            Assert.That(EnemyCastleProjectile.StaleHitCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ControlledEncounter_RangedAndHealingRolesEndCleanly()
        {
            EnemyData raiderData = CreateData(EnemySpecialRole.RangedCastleAttacker);
            raiderData.rangedAttack.windUpSeconds = 0.02f;
            raiderData.rangedAttack.projectileSpeed = 40f;
            EnemyData shamanData = CreateData(EnemySpecialRole.HealingElite);
            EnemyData gruntData = CreateData(EnemySpecialRole.None);
            Enemy raiderA = Spawn(raiderData, new Vector3(-1f, 0f, 5f));
            Enemy raiderB = Spawn(raiderData, new Vector3(1f, 0f, 5f));
            Enemy shaman = Spawn(shamanData, new Vector3(0f, 0f, 6f));
            Enemy grunt = Spawn(gruntData, new Vector3(0.5f, 0f, 6f));
            grunt.TakeDamage(40f, true);
            ExecutePulse(shaman);
            Assert.That(grunt.CurrentHealth, Is.GreaterThan(60f));
            int before = castle.CurrentHealth;
            FireImmediately(raiderA);
            FireImmediately(raiderB);
            yield return new WaitForSeconds(0.75f);
            Assert.That(castle.CurrentHealth, Is.LessThan(before));
            shaman.Kill();
            raiderA.Kill();
            raiderB.Kill();
            grunt.Kill();
            EnemyCastleProjectile.DespawnAllActive();
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ControlledEncounter_FourActiveHeroes_MultipleProjectilesAndPulses_CleansCompletely()
        {
            GameObject trackerObject = Track(new GameObject("Task13E Controlled Damage Tracker"));
            trackerObject.AddComponent<DamageTracker>();
            GameObject heroProjectilePrefab = Track(new GameObject("Task13E Hero Projectile Prefab"));
            heroProjectilePrefab.AddComponent<TrailRenderer>();
            heroProjectilePrefab.AddComponent<Projectile>();
            heroProjectilePrefab.SetActive(false);

            HeroAttack archer = CreateActiveHero("archer", AttackType.SingleTarget, StatusEffectType.None, heroProjectilePrefab, new Vector3(-3f, 0f, 0f));
            HeroAttack bombardier = CreateActiveHero("bombardier", AttackType.Splash, StatusEffectType.None, heroProjectilePrefab, new Vector3(-1f, 0f, 0f));
            HeroAttack frost = CreateActiveHero("frost_mage", AttackType.Slow, StatusEffectType.Slow, heroProjectilePrefab, new Vector3(1f, 0f, 0f));
            HeroAttack engineer = CreateActiveHero("electric_engineer", AttackType.Chain, StatusEffectType.Shock, heroProjectilePrefab, new Vector3(3f, 0f, 0f));

            EnemyData raiderData = CreateData(EnemySpecialRole.RangedCastleAttacker);
            raiderData.health = 400f;
            raiderData.rangedAttack.projectileSpeed = 2f;
            EnemyData shamanData = CreateData(EnemySpecialRole.HealingElite);
            shamanData.health = 600f;
            EnemyData allyData = CreateData(EnemySpecialRole.None);
            allyData.health = 300f;

            Enemy raiderA = Spawn(raiderData, new Vector3(-1.5f, 0f, 5f));
            Enemy raiderB = Spawn(raiderData, new Vector3(1.5f, 0f, 5f));
            Enemy shamanA = Spawn(shamanData, new Vector3(-0.75f, 0f, 6f));
            Enemy shamanB = Spawn(shamanData, new Vector3(0.75f, 0f, 6f));
            Enemy ally = Spawn(allyData, new Vector3(0f, 0f, 5.5f));
            ally.TakeDamage(180f, true);

            ExecutePulse(shamanA);
            ExecutePulse(shamanB);
            Assert.That(ally.CurrentHealth, Is.GreaterThan(120f));

            FireImmediately(raiderA);
            FireImmediately(raiderB);
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.GreaterThanOrEqualTo(2));

            FireHero(archer, raiderA);
            FireHero(bombardier, raiderB);
            FireHero(frost, shamanA);
            FireHero(engineer, shamanB);
            yield return new WaitForSeconds(1.25f);

            Assert.That(raiderA.CurrentHealth, Is.LessThan(raiderA.MaxHealth));
            Assert.That(raiderB.CurrentHealth, Is.LessThan(raiderB.MaxHealth));
            Assert.That(shamanA.CurrentHealth, Is.LessThan(shamanA.MaxHealth));
            Assert.That(shamanB.CurrentHealth, Is.LessThan(shamanB.MaxHealth));
            Assert.That(shamanA.IsSlowed, Is.True);

            pool.DespawnAllActive();
            EnemyCastleProjectile.DespawnAllActive();
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(EnemyManager.AliveCount, Is.Zero);
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.Zero);
        }

        private void ExecutePulse(Enemy shaman)
        {
            EnemySpecialBehavior behavior = shaman.GetComponent<EnemySpecialBehavior>();
            behavior.ForceActionReadyForTests();
            behavior.Tick();
            behavior.CompleteActionForTests();
            behavior.Tick();
        }

        private void FireImmediately(Enemy raider)
        {
            EnemySpecialBehavior behavior = raider.GetComponent<EnemySpecialBehavior>();
            behavior.ForceActionReadyForTests();
            behavior.Tick();
            behavior.CompleteActionForTests();
            behavior.Tick();
            Assert.That(EnemyCastleProjectile.ActiveCount, Is.GreaterThan(0));
        }

        private HeroAttack CreateActiveHero(string heroId, AttackType attackType, StatusEffectType statusType, GameObject projectilePrefab, Vector3 position)
        {
            HeroDefinition hero = Track(ScriptableObject.CreateInstance<HeroDefinition>());
            hero.id = heroId;
            hero.baseDamage = 10f;
            hero.baseFireRate = 1f;
            hero.baseRange = 12f;
            hero.defaultTargetingMode = TargetingMode.ClosestToGoal;

            WeaponDefinition weapon = Track(ScriptableObject.CreateInstance<WeaponDefinition>());
            weapon.attackType = attackType;
            weapon.projectilePrefab = projectilePrefab;
            weapon.splashRadius = 2f;
            weapon.statusEffectType = statusType;
            weapon.statusEffectValue = 0.5f;
            weapon.statusEffectDuration = 3f;
            SetPrivateField(hero, "weapon", weapon);

            GameObject heroObject = Track(new GameObject("Task13E Hero " + heroId));
            heroObject.transform.position = position;
            HeroAttack attack = heroObject.AddComponent<HeroAttack>();
            attack.Configure(hero);
            attack.enabled = true;
            return attack;
        }

        private static void FireHero(HeroAttack hero, Enemy target)
        {
            MethodInfo fire = typeof(HeroAttack).GetMethod("Fire", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fire, Is.Not.Null);
            fire.Invoke(hero, new object[] { target });
        }

        private void AssertDiagnostics(string key, int minimumReuse)
        {
            Assert.That(pool.TryGetDiagnostics(key, out EnemyPoolManager.PoolDiagnostics diagnostics), Is.True);
            Assert.That(diagnostics.ReuseCount, Is.GreaterThanOrEqualTo(minimumReuse));
            Assert.That(diagnostics.InvalidReturns, Is.Zero);
        }

        private Enemy Spawn(EnemyData data, Vector3 position)
        {
            Vector3[] path = { position, castle.transform.position };
            return pool.Spawn(data, position, Quaternion.identity, path, castle, 0f, 0f);
        }

        private EnemyData CreateData(EnemySpecialRole role, EnemyClassification classification = EnemyClassification.Normal)
        {
            sequence++;
            EnemyData data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.stableId = role + "-test-" + sequence;
            data.enemyName = data.stableId;
            data.classification = classification;
            data.health = role == EnemySpecialRole.HealingElite ? 75f : 100f;
            data.moveSpeed = 1f;
            data.goldReward = 7;
            data.xpValue = 5;
            data.castleDamage = 2;
            data.specialRole = role;

            GameObject prefab = Track(new GameObject(data.enemyName + " Prefab"));
            prefab.AddComponent<CapsuleCollider>();
            prefab.AddComponent<Enemy>();
            if (role != EnemySpecialRole.None) prefab.AddComponent<EnemySpecialBehavior>();
            data.prefab = prefab;

            if (role == EnemySpecialRole.RangedCastleAttacker)
            {
                data.rangedAttack.standOffRange = 5.5f;
                data.rangedAttack.windUpSeconds = 0.1f;
                data.rangedAttack.cooldownSeconds = 10f;
                data.rangedAttack.projectileSpeed = 20f;
                GameObject projectilePrefab = Track(new GameObject("Crossbow Bolt Test Prefab " + sequence));
                projectilePrefab.AddComponent<TrailRenderer>();
                projectilePrefab.AddComponent<EnemyCastleProjectile>();
                data.rangedAttack.projectilePrefab = projectilePrefab;
            }
            else if (role == EnemySpecialRole.HealingElite)
            {
                data.classification = EnemyClassification.Elite;
                data.healingPulse.intervalSeconds = 5f;
                data.healingPulse.castSeconds = 1f;
                data.healingPulse.radius = 4f;
                data.healingPulse.maxHealthFraction = 0.12f;
                data.healingPulse.selfHealMultiplier = 0.5f;
                data.healingPulse.targetCap = 5;
                data.healingPulse.excludeBoss = true;
            }
            return data;
        }

        private Castle CreateCastle(int health)
        {
            GameConfig config = Track(ScriptableObject.CreateInstance<GameConfig>());
            config.castleMaxHealth = health;
            GameObject castleObject = Track(new GameObject("Task13E Castle"));
            castleObject.SetActive(false);
            Castle result = castleObject.AddComponent<Castle>();
            SetPrivateField(result, "config", config);
            castleObject.SetActive(true);
            return result;
        }

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
