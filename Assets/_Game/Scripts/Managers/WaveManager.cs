using System;
using System.Collections;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Runs the scripted waves defined in GameConfig, in order. Each WaveData lists
    /// spawn entries (which enemy, how many, how fast); entries spawn sequentially.
    /// A wave ends when the EnemyManager registry is empty. Clearing the final wave
    /// raises AllWavesCleared (the run's win condition).
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        private const float WaveClearWaitWarningSeconds = 45f;
        private const float EnemyRegistrySweepInterval = 2f;

        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject spawnPoint;
        [SerializeField] private GameObject castle;
        [SerializeField] private EnemySpawnPortal spawnPortalPresentation;
        [SerializeField] private EnemySpawnPortal[] spawnPortals;
        [Header("Mobile Swarm Layout")]
        [SerializeField, Min(0f)] private float laneHalfWidth = 5.2f;
        [SerializeField, Min(1f)] private float enemyCountMultiplier = 1.8f;
        [SerializeField, Range(0.2f, 1f)] private float spawnIntervalMultiplier = 0.65f;
        [SerializeField, Range(0f, 0.3f)] private float countVariance = 0.12f;
        [SerializeField, Min(0f)] private float spawnDepthJitter = 3f;

        /// <summary>Raised at the start of each wave: (wave number, wave data).</summary>
        public event Action<int, WaveData> WaveStarted;

        /// <summary>Raised while waiting to start a wave: (wave number, wave data, seconds remaining).</summary>
        public event Action<int, WaveData, float> WaveCountdownStarted;

        /// <summary>Raised each frame while waiting to start the next wave.</summary>
        public event Action<float> WaveCountdownChanged;

        /// <summary>Raised when countdown ends or the player starts the wave early.</summary>
        public event Action WaveCountdownFinished;

        /// <summary>Raised after a wave has no enemies left: (wave number, wave data).</summary>
        public event Action<int, WaveData> WaveCleared;

        /// <summary>Raised when the last scripted wave has been cleared.</summary>
        public event Action AllWavesCleared;

        public GameConfig Config => config;
        public int CurrentWave { get; private set; }
        public int TotalWaves => activeWaves != null ? activeWaves.Length : 0;
        public bool IsWaitingForWave { get; private set; }
        public float NextWaveCountdown { get; private set; }

        private WaveData[] activeWaves;
        private StageData activeStage;
        private Castle castleComponent;
        private WaypointPath waypointPath;
        private bool startNextWaveRequested;
        private int spawnSequence;
        private EnemyPoolManager enemyPool;
        private GameObject stageFixtureInstance;

        public StageData ActiveStage => activeStage;

        private bool IsGameOver => castleComponent != null && castleComponent.IsGameOver;

        private void Start()
        {
            if (ExpansionRunContext.StageOverride != null)
            {
                activeStage = ExpansionRunContext.StageOverride;
                activeWaves = activeStage.waves;
            }
            else if (config != null && config.stages != null && config.stages.Length > SaveManager.SelectedStageIndex)
            {
                var stage = config.stages[SaveManager.SelectedStageIndex];
                if (stage != null && stage.waves != null && stage.waves.Length > 0)
                {
                    activeStage = stage;
                    activeWaves = stage.waves;
                }
            }

            if (activeWaves == null || activeWaves.Length == 0)
            {
                activeWaves = config != null ? config.waves : null;
            }

            if (config == null || activeWaves == null || activeWaves.Length == 0 || spawnPoint == null || castle == null)
            {
                Debug.LogWarning("WaveManager: assign config (with waves/stages), spawnPoint and castle in the Inspector.");
                return;
            }

            ConfigureStageOverrides();

            castleComponent = castle.GetComponent<Castle>();
            enemyPool = EnemyPoolManager.Instance != null
                ? EnemyPoolManager.Instance
                : FindFirstObjectByType<EnemyPoolManager>();
            if (enemyPool == null)
            {
                Debug.LogError("WaveManager: EnemyPoolManager is required for enemy spawning.");
                return;
            }

            PrewarmActiveEnemyPools();

            GameObject pathObj = GameObject.Find("Path");
            if (pathObj != null)
            {
                waypointPath = pathObj.GetComponent<WaypointPath>();
                if (waypointPath == null)
                {
                    waypointPath = pathObj.AddComponent<WaypointPath>();
                }
            }
            else
            {
                Debug.LogWarning("WaveManager: No GameObject named 'Path' found in the scene.");
            }

            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            if (activeWaves == null || activeWaves.Length == 0)
            {
                yield break;
            }

            for (int w = 0; w < activeWaves.Length; w++)
            {
                if (IsGameOver)
                {
                    yield break;
                }

                WaveData wave = activeWaves[w];
                if (wave == null)
                {
                    Debug.LogError("WaveManager cannot start wave index " + w +
                        ": the active stage contains a null WaveData reference.");
                    yield break;
                }

                yield return WaitForWaveStart(w + 1, wave);

                if (IsGameOver)
                {
                    yield break;
                }

                CurrentWave = w + 1;
                WaveStarted?.Invoke(CurrentWave, wave);
                Debug.Log("Wave " + CurrentWave + "/" + TotalWaves + " (" + wave.waveLabel + ") starting");

                foreach (WaveData.SpawnEntry entry in wave.spawns)
                {
                    if (entry.startDelay > 0f)
                    {
                        yield return new WaitForSeconds(entry.startDelay);
                    }
                    float waveProgress = activeWaves.Length > 1
                        ? (float)w / (activeWaves.Length - 1)
                        : 0f;
                    float progressionDensity = Mathf.Lerp(0.78f, 1.25f, waveProgress);
                    float randomDensity = UnityEngine.Random.Range(1f - countVariance, 1f + countVariance);
                    float stageDensity = activeStage != null
                        ? Mathf.Max(0.5f, activeStage.enemyCountMultiplier)
                        : 1f;
                    int adjustedCount = activeStage != null && activeStage.useExactWaveCounts
                        ? Mathf.Max(1, entry.count)
                        : Mathf.Max(1, Mathf.CeilToInt(entry.count * enemyCountMultiplier * progressionDensity * randomDensity * stageDensity));
                    for (int i = 0; i < adjustedCount; i++)
                    {
                        if (IsGameOver)
                        {
                            yield break;
                        }

                        SpawnEnemy(entry.enemy);
                        float stageInterval = activeStage != null
                            ? Mathf.Clamp(activeStage.spawnIntervalMultiplier, 0.5f, 1.5f)
                            : 1f;
                        float baseInterval = Mathf.Max(0.05f, entry.spawnInterval * spawnIntervalMultiplier * stageInterval);
                        float randomizedInterval = baseInterval * UnityEngine.Random.Range(0.7f, 1.3f);
                        yield return new WaitForSeconds(randomizedInterval);
                    }
                }

                // Wave ends when every spawned enemy is gone (killed or reached castle).
                float waitingForClearSeconds = 0f;
                float nextRegistrySweepSeconds = 0f;
                bool waitWarningLogged = false;
                // Dying enemies leave the targetable registry immediately, but their
                // approved death presentation must finish before the wave clears.
                while (enemyPool.ActiveCount > 0)
                {
                    if (IsGameOver)
                    {
                        yield break;
                    }

                    waitingForClearSeconds += Time.deltaTime;
                    nextRegistrySweepSeconds += Time.deltaTime;

                    if (nextRegistrySweepSeconds >= EnemyRegistrySweepInterval)
                    {
                        nextRegistrySweepSeconds = 0f;
                        int pruned = EnemyManager.PruneInvalidEntries();
                        if (pruned > 0)
                        {
                            Debug.LogWarning($"WaveManager: Removed {pruned} stale enemy registry entr{(pruned == 1 ? "y" : "ies")} while waiting for wave {CurrentWave} to clear.");
                        }
                        int recovered = enemyPool.RecoverUnexpectedlyDisabledEnemies();
                        if (recovered > 0)
                        {
                            Debug.LogWarning($"WaveManager: Recovered {recovered} unexpectedly disabled pooled enem{(recovered == 1 ? "y" : "ies")} while waiting for wave {CurrentWave} to clear.");
                        }
                    }

                    if (!waitWarningLogged && waitingForClearSeconds >= WaveClearWaitWarningSeconds)
                    {
                        waitWarningLogged = true;
                        Debug.LogWarning($"WaveManager: Wave {CurrentWave} has waited {WaveClearWaitWarningSeconds:0}s for {enemyPool.ActiveCount} active pooled enemies ({EnemyManager.AliveCount} targetable) after spawning finished. Continuing to monitor lifecycle integrity.");
                    }

                    yield return null;
                }

                if (IsGameOver)
                {
                    yield break;
                }

                Debug.Log("Wave " + CurrentWave + " cleared");
                WaveCleared?.Invoke(CurrentWave, wave);

                // Card drafts are driven by player level-ups, not wave completion.
            }

            Debug.Log("All " + TotalWaves + " waves cleared - VICTORY");
            AllWavesCleared?.Invoke();
        }

        private void ConfigureStageOverrides()
        {
            if (activeStage == null) return;
            if (activeStage.cardPoolOverride != null)
            {
                CardDraftManager.Instance?.SetPoolOverrideForQualification(activeStage.cardPoolOverride);
            }
            if (activeStage.battlefieldFixturePrefab != null)
            {
                stageFixtureInstance = Instantiate(activeStage.battlefieldFixturePrefab);
                stageFixtureInstance.name = activeStage.battlefieldFixturePrefab.name + " Runtime";
            }
        }

        private void OnDestroy()
        {
            if (stageFixtureInstance != null) Destroy(stageFixtureInstance);
        }

        public void StartNextWaveNow()
        {
            if (IsWaitingForWave)
            {
                startNextWaveRequested = true;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void JumpToWave(int waveNumber)
        {
            if (activeWaves == null || waveNumber < 1 || waveNumber > activeWaves.Length) return;
            CurrentWave = waveNumber;
            WaveData wave = activeWaves[waveNumber - 1];
            WaveStarted?.Invoke(CurrentWave, wave);
            if (wave != null && wave.spawns != null && wave.spawns.Length > 0)
            {
                foreach (var entry in wave.spawns)
                {
                    if (entry.enemy != null)
                    {
                        SpawnEnemy(entry.enemy);
                    }
                }
            }
            Debug.Log($"[WaveManager] Debug jumped to Wave {CurrentWave}/{TotalWaves}");
        }
#endif

        private IEnumerator WaitForWaveStart(int waveNumber, WaveData wave)
        {
            float waitTime = Mathf.Max(0f, config.timeBetweenWaves);
            if (waitTime <= 0f)
            {
                yield break;
            }

            IsWaitingForWave = true;
            startNextWaveRequested = false;
            NextWaveCountdown = waitTime;
            WaveCountdownStarted?.Invoke(waveNumber, wave, NextWaveCountdown);
            WaveCountdownChanged?.Invoke(NextWaveCountdown);

            while (NextWaveCountdown > 0f && !startNextWaveRequested)
            {
                if (IsGameOver)
                {
                    break;
                }

                yield return null;
                NextWaveCountdown = Mathf.Max(0f, NextWaveCountdown - Time.deltaTime);
                WaveCountdownChanged?.Invoke(NextWaveCountdown);
            }

            IsWaitingForWave = false;
            startNextWaveRequested = false;
            NextWaveCountdown = 0f;
            WaveCountdownFinished?.Invoke();
        }

        private void EnsurePortals()
        {
            if (enemyPool == null)
            {
                enemyPool = EnemyPoolManager.Instance != null
                    ? EnemyPoolManager.Instance
                    : FindFirstObjectByType<EnemyPoolManager>();
            }

            if (spawnPortals == null || spawnPortals.Length == 0 || spawnPortals[0] == null)
            {
                var found = FindObjectsByType<EnemySpawnPortal>(FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                {
                    Array.Sort(found, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    spawnPortals = found;
                }
            }

            if (spawnPortals != null)
            {
                for (int i = 0; i < spawnPortals.Length; i++)
                {
                    if (spawnPortals[i] != null && !spawnPortals[i].IsActivePortal)
                    {
                        spawnPortals[i].SetActiveState(true);
                    }
                }
            }
        }

        private Vector3[] CreateStraightRoute(Vector3 start, Vector3 end, int pointCount = 5)
        {
            Vector3[] pts = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                pts[i] = Vector3.Lerp(start, end, t);
            }
            return pts;
        }

        private Vector3[] GetRoutePoints(int portalIndex, Vector3 spawnPos)
        {
            if (portalIndex == 0) // Left Portal: direct straight diagonal to (-5.0, 0.1, 0.4)
            {
                Vector3 target = new Vector3(-5.0f, 0.1f, 0.4f);
                return CreateStraightRoute(spawnPos, target, 5);
            }
            else if (portalIndex == 2) // Right Portal: direct straight diagonal to (5.0, 0.1, 0.4)
            {
                Vector3 target = new Vector3(5.0f, 0.1f, 0.4f);
                return CreateStraightRoute(spawnPos, target, 5);
            }
            else // Center Portal: direct straight line to (0.0, 0.1, 0.4)
            {
                Vector3 target = new Vector3(0.0f, 0.1f, 0.4f);
                return CreateStraightRoute(spawnPos, target, 5);
            }
        }

        private void SpawnEnemy(EnemyData enemyData)
        {
            if (enemyData == null || enemyData.prefab == null)
            {
                Debug.LogWarning("WaveManager: wave entry has no enemy/prefab assigned.");
                return;
            }

            EnsurePortals();

            EnemySpawnPortal selectedPortal = null;
            int portalIndex = 1;

            if (spawnPortals != null && spawnPortals.Length > 0)
            {
                portalIndex = UnityEngine.Random.Range(0, spawnPortals.Length);
                selectedPortal = spawnPortals[portalIndex];
            }
            else if (spawnPortalPresentation != null)
            {
                selectedPortal = spawnPortalPresentation;
            }

            Vector3 spawnPos = selectedPortal != null ? selectedPortal.SpawnAnchor.position : (spawnPoint != null ? spawnPoint.transform.position : Vector3.zero);
            Quaternion spawnRot = selectedPortal != null ? selectedPortal.SpawnAnchor.rotation : Quaternion.identity;

            Vector3[] points = GetRoutePoints(portalIndex, spawnPos);

            float laneOffset = NextLaneOffset();
            if (portalIndex != 1)
            {
                laneOffset *= 0.35f;
            }

            Enemy enemy = enemyPool.Spawn(
                enemyData,
                spawnPos,
                spawnRot,
                points,
                castleComponent,
                laneOffset,
                NextDepthOffset());

            if (enemy == null)
            {
                Debug.LogError($"WaveManager: failed to spawn pooled enemy '{enemyData.name}'.", enemyData);
                return;
            }

            selectedPortal?.PlaySpawnFlare();
        }

        private void PrewarmActiveEnemyPools()
        {
            if (activeWaves == null) return;

            for (int waveIndex = 0; waveIndex < activeWaves.Length; waveIndex++)
            {
                WaveData wave = activeWaves[waveIndex];
                if (wave == null || wave.spawns == null) continue;

                for (int spawnIndex = 0; spawnIndex < wave.spawns.Length; spawnIndex++)
                {
                    EnemyData eData = wave.spawns[spawnIndex].enemy;
                    if (eData != null)
                    {
                        enemyPool.EnsurePool(eData);
                    }
                }
            }
        }

        private float NextLaneOffset()
        {
            float normalized = Mathf.Repeat(spawnSequence++ * 0.61803398875f, 1f);
            float offset = Mathf.Lerp(-laneHalfWidth, laneHalfWidth, normalized);
            return Mathf.Clamp(offset + UnityEngine.Random.Range(-0.35f, 0.35f), -laneHalfWidth, laneHalfWidth);
        }

        private float NextDepthOffset()
        {
            return UnityEngine.Random.Range(-spawnDepthJitter, spawnDepthJitter);
        }
    }
}
