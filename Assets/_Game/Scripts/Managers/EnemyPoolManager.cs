using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>Scene-owned pools for the enemy prefabs used by the active run.</summary>
    public sealed class EnemyPoolManager : MonoBehaviour
    {
        public readonly struct PoolDiagnostics
        {
            public PoolDiagnostics(string key, int created, int active, int inactive, int peakActive, int expansions, int invalidReturns, int reuseCount)
            {
                Key = key;
                Created = created;
                Active = active;
                Inactive = inactive;
                PeakActive = peakActive;
                Expansions = expansions;
                InvalidReturns = invalidReturns;
                ReuseCount = reuseCount;
            }

            public string Key { get; }
            public int Created { get; }
            public int Active { get; }
            public int Inactive { get; }
            public int PeakActive { get; }
            public int Expansions { get; }
            public int InvalidReturns { get; }
            public int ReuseCount { get; }
        }

        private sealed class EnemyPool
        {
            public string Key;
            public EnemyData Data;
            public Transform Container;
            public readonly Queue<Enemy> Inactive = new Queue<Enemy>();
            public readonly HashSet<Enemy> Active = new HashSet<Enemy>();
            public int Created;
            public int PeakActive;
            public int Expansions;
            public int InvalidReturns;
            public int ReuseCount;
        }

        public static EnemyPoolManager Instance { get; private set; }

        [SerializeField, Min(0)] private int defaultPrewarmCount = 3;
        [SerializeField, Min(0)] private int bossPrewarmCount = 1;
        [SerializeField, Min(0)] private int elitePrewarmCount = 1;

        private readonly Dictionary<string, EnemyPool> poolsByKey = new Dictionary<string, EnemyPool>(StringComparer.Ordinal);
        private readonly Dictionary<Enemy, EnemyPool> ownership = new Dictionary<Enemy, EnemyPool>();
        private readonly List<Enemy> recoveryBuffer = new List<Enemy>(8);
        private Transform inactiveRoot;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (EnemyPool pool in poolsByKey.Values)
                {
                    count += pool.Active.Count;
                }
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("EnemyPoolManager: duplicate scene pool manager detected.");
                enabled = false;
                return;
            }

            Instance = this;
            GameObject root = new GameObject("EnemyPool_Inactive");
            root.transform.SetParent(transform, false);
            inactiveRoot = root.transform;
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            DespawnAllActive();
            ownership.Clear();
            poolsByKey.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool EnsurePool(EnemyData data, int prewarmOverride = -1)
        {
            if (!TryValidateData(data, out string key))
            {
                return false;
            }

            if (poolsByKey.TryGetValue(key, out EnemyPool existing))
            {
                if (existing.Data != data)
                {
                    Debug.LogError($"EnemyPoolManager: duplicate pool key '{key}' is assigned to more than one EnemyData asset.", data);
                    return false;
                }
                return true;
            }

            GameObject containerObject = new GameObject(key);
            containerObject.transform.SetParent(inactiveRoot, false);
            EnemyPool pool = new EnemyPool
            {
                Key = key,
                Data = data,
                Container = containerObject.transform
            };
            poolsByKey.Add(key, pool);

            int prewarm = prewarmOverride >= 0
                ? prewarmOverride
                : data.classification == EnemyClassification.Boss
                    ? bossPrewarmCount
                    : data.classification == EnemyClassification.Elite ? elitePrewarmCount : defaultPrewarmCount;
            for (int i = 0; i < prewarm; i++)
            {
                Enemy enemy = CreateInstance(pool, false);
                if (enemy == null)
                {
                    return false;
                }
                pool.Inactive.Enqueue(enemy);
            }

            return true;
        }

        public Enemy Spawn(
            EnemyData data,
            Vector3 position,
            Quaternion rotation,
            Vector3[] pathPoints,
            Castle castle,
            float laneOffset,
            float spawnDepthOffset)
        {
            if (!EnsurePool(data) || !poolsByKey.TryGetValue(data.stableId, out EnemyPool pool))
            {
                return null;
            }

            bool reused = pool.Inactive.Count > 0;
            Enemy enemy = reused ? pool.Inactive.Dequeue() : CreateInstance(pool, true);
            if (enemy == null)
            {
                return null;
            }

            if (!pool.Active.Add(enemy))
            {
                Debug.LogError($"EnemyPoolManager: '{enemy.name}' was already active in pool '{pool.Key}'.", enemy);
                pool.InvalidReturns++;
                return null;
            }

            if (reused)
            {
                pool.ReuseCount++;
            }
            pool.PeakActive = Mathf.Max(pool.PeakActive, pool.Active.Count);

            enemy.transform.SetParent(null, false);
            enemy.PrepareForSpawn(data, position, rotation);
            enemy.gameObject.SetActive(true);
            enemy.ActivateFromPool(pathPoints, castle, laneOffset, spawnDepthOffset);
            return enemy;
        }

        public bool Despawn(Enemy enemy, int expectedActivationId)
        {
            if (enemy == null || !ownership.TryGetValue(enemy, out EnemyPool pool))
            {
                return false;
            }

            if (!enemy.MatchesActivation(expectedActivationId) || !pool.Active.Remove(enemy))
            {
                pool.InvalidReturns++;
                return false;
            }

            enemy.DespawnToPool();
            enemy.transform.SetParent(pool.Container, false);
            pool.Inactive.Enqueue(enemy);
            return true;
        }

        public void DespawnAllActive()
        {
            foreach (EnemyPool pool in poolsByKey.Values)
            {
                if (pool.Active.Count == 0)
                {
                    continue;
                }

                Enemy[] snapshot = new Enemy[pool.Active.Count];
                pool.Active.CopyTo(snapshot);
                for (int i = 0; i < snapshot.Length; i++)
                {
                    Enemy enemy = snapshot[i];
                    if (enemy != null)
                    {
                        Despawn(enemy, enemy.ActivationId);
                    }
                }
            }
        }

        public int RecoverUnexpectedlyDisabledEnemies()
        {
            int recovered = 0;
            foreach (EnemyPool pool in poolsByKey.Values)
            {
                recoveryBuffer.Clear();
                foreach (Enemy enemy in pool.Active)
                {
                    if (enemy == null || !enemy.IsActiveActivation || !enemy.gameObject.activeInHierarchy)
                    {
                        recoveryBuffer.Add(enemy);
                    }
                }

                for (int i = 0; i < recoveryBuffer.Count; i++)
                {
                    Enemy enemy = recoveryBuffer[i];
                    if (!pool.Active.Remove(enemy)) continue;
                    recovered++;
                    if (enemy == null) continue;
                    enemy.DespawnToPool();
                    enemy.transform.SetParent(pool.Container, false);
                    pool.Inactive.Enqueue(enemy);
                }
            }
            recoveryBuffer.Clear();
            return recovered;
        }

        public bool TryGetDiagnostics(string key, out PoolDiagnostics diagnostics)
        {
            if (key != null && poolsByKey.TryGetValue(key, out EnemyPool pool))
            {
                diagnostics = new PoolDiagnostics(
                    pool.Key,
                    pool.Created,
                    pool.Active.Count,
                    pool.Inactive.Count,
                    pool.PeakActive,
                    pool.Expansions,
                    pool.InvalidReturns,
                    pool.ReuseCount);
                return true;
            }

            diagnostics = default;
            return false;
        }

        public void LogDiagnostics()
        {
            foreach (EnemyPool pool in poolsByKey.Values)
            {
                Debug.Log(
                    $"[EnemyPool] key={pool.Key} created={pool.Created} active={pool.Active.Count} " +
                    $"inactive={pool.Inactive.Count} peak={pool.PeakActive} expansions={pool.Expansions} " +
                    $"reuses={pool.ReuseCount} invalidReturns={pool.InvalidReturns}");
            }
        }

        private Enemy CreateInstance(EnemyPool pool, bool expansion)
        {
            GameObject instance = Instantiate(pool.Data.prefab, pool.Container);
            Enemy enemy = instance.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError($"EnemyPoolManager: prefab '{pool.Data.prefab.name}' has no Enemy component.", pool.Data.prefab);
                Destroy(instance);
                return null;
            }

            if (ownership.ContainsKey(enemy))
            {
                Debug.LogError($"EnemyPoolManager: enemy '{enemy.name}' already belongs to a pool.", enemy);
                Destroy(instance);
                return null;
            }

            pool.Created++;
            if (expansion)
            {
                pool.Expansions++;
            }
            ownership.Add(enemy, pool);
            enemy.BindPool(this, pool.Key);
            enemy.gameObject.SetActive(false);
            return enemy;
        }

        private static bool TryValidateData(EnemyData data, out string key)
        {
            key = data != null ? data.stableId : null;
            if (data == null)
            {
                Debug.LogError("EnemyPoolManager: cannot create a pool for null EnemyData.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError($"EnemyPoolManager: EnemyData '{data.name}' has an empty stable ID.", data);
                return false;
            }
            if (data.prefab == null)
            {
                Debug.LogError($"EnemyPoolManager: EnemyData '{data.name}' has no prefab.", data);
                return false;
            }
            return true;
        }
    }
}
