using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class StarterCrystalPlayModeTests
    {
        private GameObject crystalGO;
        private StarterCrystal crystal;
        private StarterCrystalDefinition def;
        private GameObject enemyRegistryGO;
        private GameObject castleGO;
        private Castle castle;
        private GameConfig castleConfig;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            // Setup Enemy Registry
            enemyRegistryGO = new GameObject("Enemy Registry", typeof(EnemyManager));
            createdObjects.Add(enemyRegistryGO);

            // Setup Castle
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

            // Setup Crystal
            crystalGO = new GameObject("Starter Crystal");
            crystal = crystalGO.AddComponent<StarterCrystal>();
            createdObjects.Add(crystalGO);
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

        private Enemy SpawnEnemy(float health = 100f, Vector3 pos = default)
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.stableId = "grunt";
            data.enemyName = "grunt";
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
        public IEnumerator LightningCrystal_AcquiresTarget_AndDealsDamage()
        {
            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_lightning";
            def.element = CrystalElement.Lightning;
            def.baseDamage = 20f;
            def.attacksPerSecond = 2f;
            def.attackRange = 10f;
            def.chainTargets = 3;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            Enemy enemy = SpawnEnemy(100f, new Vector3(0f, 0f, 3f));

            yield return new WaitForSeconds(0.6f);

            Assert.That(enemy.CurrentHealth, Is.LessThan(100f), "Lightning Crystal must acquire and damage target.");
        }

        [UnityTest]
        public IEnumerator LightningCrystal_ChainTargets_NeverHitsSameEnemyTwice()
        {
            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_lightning";
            def.element = CrystalElement.Lightning;
            def.baseDamage = 10f;
            def.attacksPerSecond = 2f;
            def.attackRange = 10f;
            def.chainTargets = 4;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            Enemy e1 = SpawnEnemy(100f, new Vector3(0f, 0f, 2f));
            Enemy e2 = SpawnEnemy(100f, new Vector3(0f, 0f, 4f));

            yield return new WaitForSeconds(0.3f);

            // Single cast damage: primary and chained targets must receive equal damage
            Assert.That(e1.CurrentHealth, Is.LessThan(100f), "Primary target must be hit.");
            Assert.That(e2.CurrentHealth, Is.EqualTo(e1.CurrentHealth), "Chained target must be hit for identical damage as primary target.");
        }

        [UnityTest]
        public IEnumerator FireCrystal_AppliesSplashAndBurn()
        {
            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_fire";
            def.element = CrystalElement.Fire;
            def.baseDamage = 20f;
            Enemy primary = SpawnEnemy(100f, new Vector3(0f, 0f, 2f));
            Enemy nearby = SpawnEnemy(100f, new Vector3(1f, 0f, 2f));
            Enemy far = SpawnEnemy(100f, new Vector3(10f, 0f, 2f));

            def.attacksPerSecond = 1f;
            def.attackRange = 10f;
            def.splashRadius = 3f;
            def.damageOverTime = 5f;
            def.damageOverTimeDuration = 2f;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            yield return new WaitForSeconds(0.35f);

            Assert.That(primary.CurrentHealth, Is.LessThan(100f), "Primary target takes direct fire damage.");
            Assert.That(nearby.CurrentHealth, Is.LessThan(100f), "Nearby enemy takes splash fire damage.");
            Assert.That(far.CurrentHealth, Is.EqualTo(100f), "Far enemy outside splash radius is untouched.");
        }

        [UnityTest]
        public IEnumerator IceCrystal_AppliesSlowStatus()
        {
            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_ice";
            def.element = CrystalElement.Ice;
            def.baseDamage = 10f;
            def.attacksPerSecond = 2f;
            def.attackRange = 10f;
            def.statusMagnitude = 0.4f;
            def.statusDuration = 3f;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            Enemy enemy = SpawnEnemy(100f, new Vector3(0f, 0f, 3f));

            yield return new WaitForSeconds(0.6f);

            Assert.That(enemy.IsSlowed, Is.True, "Ice crystal must apply slow status to enemy.");
        }

        [UnityTest]
        public IEnumerator GameplayIntegrationScene_StarterCrystal_IsPresent_AndOnlyOneActive()
        {
            var sceneReq = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameplayIntegration_V2");
            while (!sceneReq.isDone) yield return null;

            StarterCrystal activeCrystal = Object.FindFirstObjectByType<StarterCrystal>();
            Assert.That(activeCrystal, Is.Not.Null, "StarterCrystal component must exist in GameplayIntegration_V2.");
            Assert.That(activeCrystal.gameObject.activeInHierarchy, Is.True, "StarterCrystal object must be active.");
            Assert.That(activeCrystal.Definition, Is.Not.Null, "StarterCrystal definition must resolve and be non-null.");
            Assert.That(activeCrystal.Definition.crystalId, Is.EqualTo("crystal_lightning"), "Default active crystal ID must be crystal_lightning.");

            StarterCrystal[] allCrystals = Object.FindObjectsByType<StarterCrystal>(FindObjectsSortMode.None);
            Assert.That(allCrystals.Length, Is.EqualTo(1), "Exactly one runtime StarterCrystal component must exist.");

            HeroRosterManager roster = Object.FindFirstObjectByType<HeroRosterManager>();
            Assert.That(roster, Is.Not.Null, "HeroRosterManager must exist in scene.");
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0), "Run must start with 0 owned heroes.");
            Assert.That(roster.EmptySlotCount, Is.GreaterThanOrEqualTo(3), "HeroSlots must remain empty before recruitment.");
        }

        [UnityTest]
        public IEnumerator LobbySelection_FireCrystal_LoadsGameplayIntegration_V2_WithFireCrystal()
        {
            SaveManager.SetSelectedStarterCrystal("crystal_fire");

            var sceneReq = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameplayIntegration_V2");
            while (!sceneReq.isDone) yield return null;

            StarterCrystal activeCrystal = Object.FindFirstObjectByType<StarterCrystal>();
            Assert.That(activeCrystal, Is.Not.Null, "StarterCrystal component must exist in GameplayIntegration_V2.");
            Assert.That(activeCrystal.Definition, Is.Not.Null, "StarterCrystal definition must resolve.");
            Assert.That(activeCrystal.Definition.crystalId, Is.EqualTo("crystal_fire"), "Selected Fire crystal must load in GameplayIntegration_V2.");

            HeroRosterManager roster = Object.FindFirstObjectByType<HeroRosterManager>();
            Assert.That(roster, Is.Not.Null);
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0), "Run must start with 0 heroes.");
        }

        [UnityTest]
        public IEnumerator TopSpawn_EnemyAcquisition_Succeeds()
        {
            crystalGO.transform.position = new Vector3(0f, 4.25f, 0.40f);

            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_lightning";
            def.element = CrystalElement.Lightning;
            def.baseDamage = 20f;
            def.attacksPerSecond = 2f;
            def.attackRange = 14f;
            def.chainTargets = 3;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            // Enemy placed at top spawn (~38m away)
            Enemy topSpawnEnemy = SpawnEnemy(100f, new Vector3(0f, 0.1f, 38f));

            yield return new WaitForSeconds(0.6f);

            Assert.That(topSpawnEnemy.CurrentHealth, Is.LessThan(100f), "Starter Crystal at fortress center must acquire and attack enemy at top spawn.");
        }

        [UnityTest]
        public IEnumerator Lightning_ChainRange_DoesNotHitFarEnemy()
        {
            def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "crystal_lightning";
            def.element = CrystalElement.Lightning;
            def.baseDamage = 15f;
            def.attacksPerSecond = 2f;
            def.chainTargets = 3;
            createdObjects.Add(def);

            crystal.Configure(def);
            crystal.enabled = true;

            Enemy primary = SpawnEnemy(100f, new Vector3(0f, 0f, 2f));
            Enemy farEnemy = SpawnEnemy(100f, new Vector3(0f, 0f, 20f)); // 18m away from primary, well outside 5m chain range

            yield return new WaitForSeconds(0.4f);

            Assert.That(primary.CurrentHealth, Is.LessThan(100f), "Primary enemy takes direct lightning hit.");
            Assert.That(farEnemy.CurrentHealth, Is.EqualTo(100f), "Far enemy outside 5m bounce range is not hit by chain lightning.");
        }
    }
}

