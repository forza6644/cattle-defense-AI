using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Stonehold.Tests
{
    public class ThreeLaneRoutingPlayModeTests
    {
        private GameObject poolObject;
        private GameObject registryObject;
        private GameObject waveObject;
        private GameObject castleObject;
        private GameObject spawnPointObject;
        private EnemyPoolManager pool;
        private WaveManager waveManager;
        private EnemyData data;
        private GameConfig castleConfig;
        private Castle castle;
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<EnemySpawnPortal> portals = new List<EnemySpawnPortal>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                PropertyInfo state = typeof(GameManager).GetProperty(nameof(GameManager.State));
                state?.SetValue(GameManager.Instance, GameState.Playing);
            }

            registryObject = new GameObject("ThreeLane Registry");
            registryObject.AddComponent<EnemyManager>();

            poolObject = new GameObject("ThreeLane Pool");
            pool = poolObject.AddComponent<EnemyPoolManager>();

            castleConfig = ScriptableObject.CreateInstance<GameConfig>();
            castleConfig.castleMaxHealth = 50;
            castleObject = new GameObject("ThreeLane Castle");
            castleObject.SetActive(false);
            castle = castleObject.AddComponent<Castle>();
            SetPrivateField(castle, "config", castleConfig);
            castleObject.transform.position = new Vector3(0f, 0.1f, 0.4f);
            castleObject.SetActive(true);

            spawnPointObject = new GameObject("SpawnPoint");
            spawnPointObject.transform.position = new Vector3(0f, 0.1f, 38.6f);

            portals.Add(CreatePortal("Portal_Left", new Vector3(-10.5f, 0.1f, 38.6f)));
            portals.Add(CreatePortal("Portal_Center", new Vector3(0f, 0.1f, 38.6f)));
            portals.Add(CreatePortal("Portal_Right", new Vector3(10.5f, 0.1f, 38.6f)));

            data = CreateEnemyData();

            waveObject = new GameObject("ThreeLane WaveManager");
            waveObject.SetActive(false);
            waveManager = waveObject.AddComponent<WaveManager>();
            waveManager.ConfigureForTests(
                castleConfig,
                spawnPointObject,
                castleObject,
                portals.ToArray(),
                pool);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (pool != null)
            {
                pool.DespawnAllActive();
            }

            Destroy(waveObject);
            Destroy(poolObject);
            Destroy(registryObject);
            Destroy(castleObject);
            Destroy(spawnPointObject);
            for (int i = 0; i < created.Count; i++)
            {
                Destroy(created[i]);
            }
            created.Clear();
            portals.Clear();
            if (data != null)
            {
                Object.DestroyImmediate(data);
            }
            if (castleConfig != null)
            {
                Object.DestroyImmediate(castleConfig);
            }
        }

        [Test]
        public void WaveManager_LeftPortal_SpawnsLeftLane()
        {
            Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemy.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(enemy.transform.position.x, Is.EqualTo(-10.5f).Within(1.2f));
            AssertPathStaysOnLane(enemy, -10.5f);
        }

        [Test]
        public void WaveManager_CenterPortal_SpawnsCenterLane()
        {
            Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Center);
            Assert.That(enemy.LaneIndex, Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(enemy.transform.position.x, Is.EqualTo(0f).Within(1.2f));
            AssertPathStaysOnLane(enemy, 0f);
        }

        [Test]
        public void WaveManager_RightPortal_SpawnsRightLane()
        {
            Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            Assert.That(enemy.LaneIndex, Is.EqualTo(CombatLaneRouting.Right));
            Assert.That(enemy.transform.position.x, Is.EqualTo(10.5f).Within(1.2f));
            AssertPathStaysOnLane(enemy, 10.5f);
        }

        [Test]
        public void PoolReuse_ReassignsLaneIndex_AndDoesNotKeepPreviousLane()
        {
            pool.EnsurePool(data, 1);
            Enemy first = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            int firstId = first.ActivationId;
            Assert.That(first.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(pool.Despawn(first, firstId), Is.True);
            Assert.That(first.LaneIndex, Is.EqualTo(-1));

            Enemy second = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.ActivationId, Is.Not.EqualTo(firstId));
            Assert.That(second.LaneIndex, Is.EqualTo(CombatLaneRouting.Right));
            Assert.That(second.transform.position.x, Is.GreaterThan(8f));
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotDriftIntoAnotherLane()
        {
            data.moveSpeed = 40f;
            Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            for (int i = 0; i < 45 && enemy != null && enemy.IsActiveActivation && !enemy.IsDead; i++)
            {
                if (enemy.IsAttackingCastle)
                {
                    break;
                }

                if (enemy.transform.position.z > 6f)
                {
                    Assert.That(enemy.transform.position.x, Is.LessThan(-2.0f),
                        "Left-lane enemy collapsed into the center highway at t=" + i);
                }

                Assert.That(enemy.transform.position.x, Is.LessThan(0.35f),
                    "Left-lane enemy crossed into another lane at t=" + i);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EachLane_ReachesCastle_AndDealsDamage()
        {
            data.moveSpeed = 80f;
            data.castleDamage = 4;
            int startHealth = castle.CurrentHealth;

            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            yield return WaitUntilInactive(left, 6f);
            Assert.That(castle.CurrentHealth, Is.EqualTo(startHealth - 4));

            Enemy center = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Center);
            yield return WaitUntilInactive(center, 6f);
            Assert.That(castle.CurrentHealth, Is.EqualTo(startHealth - 8));

            Enemy right = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            yield return WaitUntilInactive(right, 6f);
            Assert.That(castle.CurrentHealth, Is.EqualTo(startHealth - 12));
            Assert.That(castle.IsGameOver, Is.False);
        }

        [Test]
        public void HeroTargeting_StillFindsLaneEnemiesByRange()
        {
            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Enemy right = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            Assert.That(left.IsTargetable, Is.True);
            Assert.That(right.IsTargetable, Is.True);

            Enemy found = EnemyManager.FindTarget(new Vector3(0f, 0.1f, 0.4f), 50f, TargetingMode.Nearest);
            Assert.That(found, Is.Not.Null);
            Assert.That(found == left || found == right, Is.True);
        }

        [Test]
        public void HeroRange_EngagesConvergingLanesWithoutLaneLock()
        {
            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Enemy right = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            left.transform.position = new Vector3(-5.5f, 0.1f, 10f);
            right.transform.position = new Vector3(5.5f, 0.1f, 10f);

            Vector3 wallHero = new Vector3(0f, 2.7f, -0.4f);
            Enemy nearLeft = EnemyManager.FindTarget(wallHero, 18f, TargetingMode.Nearest);
            Assert.That(nearLeft, Is.Not.Null);
            Assert.That(nearLeft == left || nearLeft == right, Is.True);

            Enemy onlyLeft = EnemyManager.FindTarget(new Vector3(-4f, 2.7f, -0.4f), 14f, TargetingMode.Nearest);
            Assert.That(onlyLeft, Is.EqualTo(left));
            Enemy onlyRight = EnemyManager.FindTarget(new Vector3(4f, 2.7f, -0.4f), 14f, TargetingMode.Nearest);
            Assert.That(onlyRight, Is.EqualTo(right));
        }

        [Test]
        public void StarterCrystalRange_StillTargetsLaneEnemies()
        {
            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            left.transform.position = new Vector3(-4.5f, 0.1f, 8f);
            Vector3 crystalPose = new Vector3(0f, 2.4f, 1.2f);
            Enemy found = EnemyManager.FindTarget(crystalPose, 22f, TargetingMode.ClosestToGoal);
            Assert.That(found, Is.EqualTo(left));
        }

        [Test]
        public void Routes_ConvergeNearCastle_WhileKeepingEarlySeparation()
        {
            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Enemy right = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            Vector3[] leftPath = left.DebugPathPoints;
            Vector3[] rightPath = right.DebugPathPoints;
            Assert.That(Mathf.Abs(leftPath[1].x - rightPath[1].x), Is.GreaterThan(14f));
            Assert.That(Mathf.Abs(leftPath[leftPath.Length - 1].x - rightPath[rightPath.Length - 1].x), Is.LessThan(0.75f));
            Assert.That(Mathf.Abs(leftPath[leftPath.Length - 2].x), Is.LessThan(4.0f));
        }

        [Test]
        public void WithinLaneScatter_RemainsBounded()
        {
            for (int i = 0; i < 12; i++)
            {
                Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
                Assert.That(Mathf.Abs(enemy.transform.position.x + 10.5f), Is.LessThanOrEqualTo(CombatLaneRouting.MaxWithinLaneHalfWidth + 0.05f));
                Assert.That(enemy.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            }
        }

        [UnityTest]
        public IEnumerator LaneIndex_PreservedUntilDespawn()
        {
            data.moveSpeed = 40f;
            Enemy enemy = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            int lane = enemy.LaneIndex;
            for (int i = 0; i < 20 && enemy.IsActiveActivation && !enemy.IsDead; i++)
            {
                Assert.That(enemy.LaneIndex, Is.EqualTo(lane));
                yield return null;
            }

            if (enemy.IsActiveActivation)
            {
                Assert.That(enemy.LaneIndex, Is.EqualTo(CombatLaneRouting.Right));
            }
            else
            {
                Assert.That(enemy.LaneIndex, Is.EqualTo(-1));
            }
        }

        [Test]
        public void ConvergingRoutes_StaySeparatedUntilLateApproach()
        {
            Enemy left = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Enemy center = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Center);
            Enemy right = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Right);
            Assert.That(CombatLaneRouting.RoutesAreDistinct(left.DebugPathPoints, center.DebugPathPoints, right.DebugPathPoints, 2.5f), Is.True);
        }

        [Test]
        public void ActivationId_StillUniqueAfterLaneSpawn()
        {
            Enemy a = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Left);
            Enemy b = waveManager.SpawnEnemyOnLane(data, CombatLaneRouting.Center);
            Assert.That(a.ActivationId, Is.Not.EqualTo(b.ActivationId));
            Assert.That(a.MatchesActivation(a.ActivationId), Is.True);
            Assert.That(b.MatchesActivation(a.ActivationId), Is.False);
        }

        [Test]
        public void AutoAssignment_RoundRobinsThreeLanes()
        {
            Enemy left = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Auto);
            Enemy center = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Auto);
            Enemy right = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Auto);
            Assert.That(left.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(center.LaneIndex, Is.EqualTo(CombatLaneRouting.Center));
            Assert.That(right.LaneIndex, Is.EqualTo(CombatLaneRouting.Right));
        }

        [Test]
        public void SingleLanePressure_KeepsEverySpawnOnLeft()
        {
            Enemy first = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Left);
            Enemy second = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Left);
            Assert.That(first.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(second.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(first.transform.position.x, Is.LessThan(-8f));
            Assert.That(second.transform.position.x, Is.LessThan(-8f));
        }

        [Test]
        public void TwoLanePressure_UsesLeftAndRightOnly()
        {
            Enemy left = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Left);
            Enemy right = waveManager.SpawnWithAssignment(data, WaveLaneAssignment.Right);
            Assert.That(left.LaneIndex, Is.EqualTo(CombatLaneRouting.Left));
            Assert.That(right.LaneIndex, Is.EqualTo(CombatLaneRouting.Right));
            Assert.That(Mathf.Abs(right.transform.position.x - left.transform.position.x), Is.GreaterThan(15f));
        }

        private IEnumerator WaitUntilInactive(Enemy enemy, float timeout)
        {
            float elapsed = 0f;
            while (enemy != null && enemy.IsActiveActivation && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(elapsed, Is.LessThan(timeout), "Enemy never reached the castle in time.");
        }

        private static void AssertPathStaysOnLane(Enemy enemy, float laneX)
        {
            Vector3[] path = enemy.DebugPathPoints;
            Assert.That(path, Is.Not.Null);
            Assert.That(path.Length, Is.GreaterThan(2));
            int lane = laneX < -2f ? CombatLaneRouting.Left : (laneX > 2f ? CombatLaneRouting.Right : CombatLaneRouting.Center);
            Assert.That(CombatLaneRouting.RouteKeepsLaneIdentity(path, lane), Is.True);
            Assert.That(Mathf.Abs(path[0].x - laneX), Is.LessThan(1.3f), "Spawn point left the assigned portal column.");
            if (lane != CombatLaneRouting.Center)
            {
                Assert.That(Mathf.Abs(path[1].x - laneX), Is.LessThan(2.4f), "Far-field waypoint collapsed too early.");
            }
        }

        private EnemySpawnPortal CreatePortal(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            created.Add(go);
            return go.AddComponent<EnemySpawnPortal>();
        }

        private EnemyData CreateEnemyData()
        {
            EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.stableId = "three-lane-grunt";
            enemyData.enemyName = "Lane Grunt";
            enemyData.classification = EnemyClassification.Normal;
            enemyData.health = 40f;
            enemyData.moveSpeed = 8f;
            enemyData.goldReward = 1;
            enemyData.xpValue = 1;
            enemyData.castleDamage = 2;

            GameObject prefab = new GameObject("Lane Grunt Prefab");
            created.Add(prefab);
            prefab.AddComponent<CapsuleCollider>();
            prefab.AddComponent<Enemy>();
            enemyData.prefab = prefab;
            return enemyData;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        private static void Destroy(GameObject go)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
