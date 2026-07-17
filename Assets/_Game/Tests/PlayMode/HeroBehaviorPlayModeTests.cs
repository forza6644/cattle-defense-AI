using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class HeroBehaviorPlayModeTests
    {
        private GameObject runModifierObject;
        private GameObject enemyRegistryObject;
        private GameObject enemyPoolObject;
        private EnemyPoolManager enemyPool;
        private Castle castle;
        private GameObject castleObject;
        private GameConfig castleConfig;

        private GameObject heroObject;
        private HeroAttack heroAttack;
        private HeroDefinition heroDef;
        private WeaponDefinition weaponDef;
        private GameObject projPrefab;

        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            // Setup Enemy registry and pool
            enemyRegistryObject = new GameObject("Enemy Registry");
            enemyRegistryObject.AddComponent<EnemyManager>();
            createdObjects.Add(enemyRegistryObject);

            enemyPoolObject = new GameObject("Enemy Pool");
            enemyPool = enemyPoolObject.AddComponent<EnemyPoolManager>();
            createdObjects.Add(enemyPoolObject);

            // Setup RunModifierManager
            if (RunModifierManager.Instance == null)
            {
                runModifierObject = new GameObject("RunModifierManager", typeof(RunModifierManager));
                createdObjects.Add(runModifierObject);
            }
            RunModifierManager.Instance.ClearModifiers();

            // Setup Castle
            castleConfig = ScriptableObject.CreateInstance<GameConfig>();
            castleConfig.castleMaxHealth = 100;
            createdObjects.Add(castleConfig);

            castleObject = new GameObject("Castle");
            castleObject.transform.position = new Vector3(-1000f, 0f, 0f);
            castleObject.SetActive(false);
            castle = castleObject.AddComponent<Castle>();
            var field = castle.GetType().GetField("config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(castle, castleConfig);
            castleObject.SetActive(true);
            createdObjects.Add(castleObject);

            // Create projectile prefab
            projPrefab = new GameObject("ProjectilePrefab");
            projPrefab.AddComponent<TrailRenderer>();
            projPrefab.AddComponent<Projectile>();
            projPrefab.SetActive(false);
            createdObjects.Add(projPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (RunModifierManager.Instance != null)
            {
                RunModifierManager.Instance.ClearModifiers();
            }

            foreach (var go in createdObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            createdObjects.Clear();
        }

        private Enemy SpawnEnemy(float health = 100f, Vector3 startPos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "grunt";
            data.enemyName = "grunt";
            data.health = health;
            data.moveSpeed = 1f;
            data.castleDamage = 1;
            data.goldReward = 5;
            createdObjects.Add(data);

            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.AddComponent<CapsuleCollider>();
            Enemy enemy = enemyGo.AddComponent<Enemy>();
            var dataField = enemy.GetType().GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dataField != null) dataField.SetValue(enemy, data);

            createdObjects.Add(enemyGo);

            enemy.PrepareForSpawn(data, startPos, Quaternion.identity);
            enemy.ActivateFromPool(new[] { startPos, new Vector3(1000f, 0f, 0f) }, castle);

            return enemy;
        }

        private void SetupHero(string heroId, AttackType attackType)
        {
            heroDef = ScriptableObject.CreateInstance<HeroDefinition>();
            heroDef.id = heroId;
            heroDef.baseDamage = 10f;
            heroDef.baseFireRate = 1f;
            heroDef.baseRange = 10f;
            heroDef.defaultTargetingMode = TargetingMode.ClosestToGoal;
            createdObjects.Add(heroDef);

            weaponDef = ScriptableObject.CreateInstance<WeaponDefinition>();
            weaponDef.attackType = attackType;
            weaponDef.projectilePrefab = projPrefab;
            weaponDef.splashRadius = 2f;
            weaponDef.statusEffectType = StatusEffectType.Slow;
            weaponDef.statusEffectValue = 0.5f;
            weaponDef.statusEffectDuration = 3f;
            createdObjects.Add(weaponDef);

            heroDef.GetType().GetField("weapon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).SetValue(heroDef, weaponDef);

            heroObject = new GameObject("Hero");
            heroAttack = heroObject.AddComponent<HeroAttack>();
            heroAttack.Configure(heroDef);
            heroAttack.enabled = false;
            createdObjects.Add(heroObject);
        }

        [UnityTest]
        public IEnumerator Archer_TwinVolley_FiresExtraProjectiles()
        {
            SetupHero("archer", AttackType.SingleTarget);

            // Apply Twin Volley behavior card (2 stacks)
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "archer_twin_volley";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "archer";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraProjectile,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                count = 1,
                maxStacks = 2
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);
            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));

            // Force attack fire delay path
            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { enemy1 });

            // Wait for delay: fire is at 0.15s, check at 0.22s (in flight)
            yield return new WaitForSeconds(0.22f);

            // Check if multiple projectiles were spawned (should be 3: 1 original + 2 from stacks)
            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Archer_PiercingArrows_HitsMultipleEnemies()
        {
            SetupHero("archer", AttackType.SingleTarget);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "archer_piercing_arrows";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "archer";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.Piercing,
                targetType = CardTargetType.HeroById,
                targetHeroId = "archer",
                maxStacks = 2
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));

            // Spawn projectile manually to trace piercing
            Projectile projectile = Projectile.Spawn(projPrefab, Vector3.zero);
            createdObjects.Add(projectile.gameObject);
            projectile.Init(enemy1, 10f, 0f, 1f, 0f, Color.white, "archer");

            // Setup projectile properties via reflection or public API if available
            var maxPiercesField = projectile.GetType().GetField("maxPierces", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var moveDirectionField = projectile.GetType().GetField("moveDirection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (maxPiercesField != null) maxPiercesField.SetValue(projectile, 2);
            if (moveDirectionField != null) moveDirectionField.SetValue(projectile, Vector3.right);

            // Wait for projectile to travel and process collisions
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Both enemies should have taken damage
            Assert.That(enemy1.CurrentHealth, Is.LessThan(100f));
            Assert.That(enemy2.CurrentHealth, Is.LessThan(100f));
        }

        [UnityTest]
        public IEnumerator Bombardier_ClusterShells_SpawnsSecondaryBlasts()
        {
            SetupHero("bombardier", AttackType.Splash);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "bombardier_cluster_shells";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "bombardier";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.SplitProjectile,
                targetType = CardTargetType.HeroById,
                targetHeroId = "bombardier",
                count = 2,
                maxStacks = 2
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));
            projectileLaunch(enemy1);

            yield return new WaitForSeconds(0.5f);

            // Check secondary blasts
            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude);
            Assert.That(projectiles.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Bombardier_WideBlast_ModifiesRadiusRespectsCap()
        {
            SetupHero("bombardier", AttackType.Splash);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "bombardier_wide_blast";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "bombardier";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExplosionRadius,
                targetType = CardTargetType.HeroById,
                targetHeroId = "bombardier",
                floatValue = 0.75f,
                maxStacks = 3
            };
            createdObjects.Add(card);

            // 1 stack
            RunModifierManager.Instance.AddCard(card);
            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.ExplosionRadius), Is.EqualTo(1));

            // Apply 4 cards, stack cap is 3
            RunModifierManager.Instance.AddCard(card);
            RunModifierManager.Instance.AddCard(card);
            RunModifierManager.Instance.AddCard(card);

            Assert.That(RunModifierManager.Instance.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.ExplosionRadius), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator FrostMage_ShardVolley_SlowsMultiple()
        {
            SetupHero("frost_mage", AttackType.SingleTarget);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "frost_mage_shard_volley";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "frost_mage";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraProjectile,
                targetType = CardTargetType.HeroById,
                targetHeroId = "frost_mage",
                count = 1,
                maxStacks = 2
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(0f, 0f, 0f));
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { enemy1 });

            // Fire delay is 0.18s, travel to 4m is 0.33s. Total 0.51s. Wait 0.65s.
            yield return new WaitForSeconds(0.65f);

            // Both should have Slow applied
            Assert.That(enemy1.IsSlowed, Is.True);
            Assert.That(enemy2.IsSlowed, Is.True);
        }

        [UnityTest]
        public IEnumerator FrostMage_EchoingNova_RepeatsFrostNova()
        {
            SetupHero("frost_mage", AttackType.SingleTarget);
            heroDef.abilityType = HeroAbilityType.FrostNova;

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "frost_mage_echoing_nova";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "frost_mage";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraCast,
                targetType = CardTargetType.HeroById,
                targetHeroId = "frost_mage",
                maxStacks = 1
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy = SpawnEnemy(200f, new Vector3(2f, 0f, 0f));

            var useAbilityMethod = heroAttack.GetType().GetMethod("UseSignatureAbility", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            useAbilityMethod.Invoke(heroAttack, new object[] { enemy });

            // Nova delay: 0f (instant hit in radius)
            yield return new WaitForSeconds(0.1f);
            float hpAfterFirst = enemy.CurrentHealth;

            // Wait for Echo delay: 1.0s
            yield return new WaitForSeconds(1.1f);
            Assert.That(enemy.CurrentHealth, Is.LessThan(hpAfterFirst));
        }

        [UnityTest]
        public IEnumerator ElectricEngineer_ExtendedCircuit_IncreasesChainLinks()
        {
            SetupHero("electric_engineer", AttackType.Chain);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "electric_engineer_extended_circuit";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "electric_engineer";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.ExtraChain,
                targetType = CardTargetType.HeroById,
                targetHeroId = "electric_engineer",
                maxStacks = 3
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));
            Enemy enemy3 = SpawnEnemy(100f, new Vector3(6f, 0f, 0f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { enemy1 });

            yield return new WaitForSeconds(0.4f);

            // Bounces are direct chain damage, all 3 should have taken damage
            Assert.That(enemy1.CurrentHealth, Is.LessThan(100f));
            Assert.That(enemy2.CurrentHealth, Is.LessThan(100f));
            Assert.That(enemy3.CurrentHealth, Is.LessThan(100f));
        }

        [UnityTest]
        public IEnumerator ElectricEngineer_ForkedCurrent_ForksCorrectly()
        {
            SetupHero("electric_engineer", AttackType.Chain);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.id = "electric_engineer_forked_current";
            card.cardCategory = CardCategory.HeroUpgrade;
            card.targetType = CardTargetType.HeroById;
            card.targetHeroId = "electric_engineer";
            card.behaviorUpgrade = new HeroBehaviorUpgradeData
            {
                effectType = HeroBehaviorEffectType.Ricochet,
                targetType = CardTargetType.HeroById,
                targetHeroId = "electric_engineer",
                maxStacks = 2
            };
            createdObjects.Add(card);

            RunModifierManager.Instance.AddCard(card);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(4f, 0f, 0f));

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { enemy1 });

            yield return new WaitForSeconds(0.4f);

            // Fork hits enemy2, both should take damage
            Assert.That(enemy1.CurrentHealth, Is.LessThan(100f));
            Assert.That(enemy2.CurrentHealth, Is.LessThan(100f));
        }

        [UnityTest]
        public IEnumerator StaleTargetSafety_ProtectsReusedEnemy()
        {
            SetupHero("archer", AttackType.SingleTarget);

            Enemy enemy1 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));
            int originalActivation = enemy1.ActivationId;

            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { enemy1 });

            // Despawn during delay
            enemy1.DespawnToPool();

            // Spawn another enemy so it takes the position/pool slot
            Enemy enemy2 = SpawnEnemy(100f, new Vector3(2f, 0f, 0f));

            yield return new WaitForSeconds(0.2f);

            // enemy2 should not have taken any damage from the delayed archer shot targeting the old activation
            Assert.That(enemy2.CurrentHealth, Is.EqualTo(100f));
        }

        private void projectileLaunch(Enemy target)
        {
            var fireMethod = heroAttack.GetType().GetMethod("Fire", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fireMethod.Invoke(heroAttack, new object[] { target });
        }
    }
}
