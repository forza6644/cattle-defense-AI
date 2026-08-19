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
        [Header("Three-Lane Layout")]
#pragma warning disable 0414
        [SerializeField, HideInInspector] private float laneHalfWidth = 5.2f;
#pragma warning restore 0414
        [SerializeField, Range(0f, 1.2f)] private float withinLaneHalfWidth = CombatLaneRouting.DefaultWithinLaneHalfWidth;
        [SerializeField, Min(0.5f)] private float fallbackLaneSeparation = CombatLaneRouting.DefaultFallbackLaneSeparation;
        [SerializeField, Min(1f)] private float enemyCountMultiplier = 1.8f;
        [SerializeField, Range(0.2f, 1f)] private float spawnIntervalMultiplier = 0.65f;
        [SerializeField, Range(0f, 0.3f)] private float countVariance = 0.12f;
        [SerializeField, Min(0f)] private float spawnDepthJitter = 1.25f;

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
                if (pathObj.GetComponent<WaypointPath>() == null)
                {
                    pathObj.AddComponent<WaypointPath>();
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

                        SpawnEnemy(entry.enemy, entry.laneAssignment);
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
                QuestManager.Instance?.ReportWaveCleared();

                // Card drafts are driven by player level-ups, not wave completion.
            }

            if (EndlessSurvivalManager.Instance != null && EndlessSurvivalManager.Instance.IsEndlessActive)
            {
                yield return StartCoroutine(RunEndlessWaves());
                yield break;
            }

            Debug.Log("All " + TotalWaves + " waves cleared - VICTORY");
            AllWavesCleared?.Invoke();
        }

        public void TriggerEndlessModeAfterVictory()
        {
            if (EndlessSurvivalManager.Instance != null)
            {
                EndlessSurvivalManager.Instance.StartEndlessMode();
                StartCoroutine(RunEndlessWaves());
            }
        }

        private IEnumerator RunEndlessWaves()
        {
            var allEnemies = Resources.LoadAll<EnemyData>("Enemies");
            if (allEnemies == null || allEnemies.Length == 0)
            {
                allEnemies = activeWaves != null && activeWaves.Length > 0 && activeWaves[0].spawns != null && activeWaves[0].spawns.Length > 0
                    ? new EnemyData[] { activeWaves[0].spawns[0].enemy }
                    : null;
            }

            while (!IsGameOver && EndlessSurvivalManager.Instance != null && EndlessSurvivalManager.Instance.IsEndlessActive)
            {
                int abyssalWave = EndlessSurvivalManager.Instance.AbyssalWaveNumber;
                CurrentWave = (activeWaves != null ? activeWaves.Length : 10) + abyssalWave;

                yield return WaitForWaveStart(CurrentWave, null);
                if (IsGameOver) yield break;

                WaveStarted?.Invoke(CurrentWave, null);
                Debug.Log($"[WaveManager] Abyssal Wave {abyssalWave} starting! (Wave {CurrentWave})");

                if (allEnemies != null && allEnemies.Length > 0)
                {
                    int enemyCount = EndlessSurvivalManager.Instance.GetAbyssalEnemyCount(abyssalWave);
                    for (int i = 0; i < enemyCount; i++)
                    {
                        if (IsGameOver) yield break;
                        var enemyToSpawn = allEnemies[UnityEngine.Random.Range(0, allEnemies.Length)];
                        if (enemyToSpawn != null)
                        {
                            SpawnEnemy(enemyToSpawn, WaveLaneAssignment.Auto);
                        }
                        yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.5f));
                    }
                }

                while (enemyPool != null && enemyPool.ActiveCount > 0)
                {
                    if (IsGameOver) yield break;
                    yield return null;
                }

                if (IsGameOver) yield break;

                Debug.Log($"[WaveManager] Abyssal Wave {abyssalWave} cleared!");
                WaveCleared?.Invoke(CurrentWave, null);
                QuestManager.Instance?.ReportWaveCleared();
                SaveManager.RecordEndlessAbyssWave(CurrentWave, abyssalWave * 1000);
                EndlessSurvivalManager.Instance.AdvanceAbyssalWave();
                UIManager.Instance?.ShowAbyssalDraftModal(EndlessSurvivalManager.Instance.RollOverchargeDraft(3));
            }
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
                        SpawnEnemy(entry.enemy, WaveLaneAssignment.Auto);
                    }
                }
            }
            Debug.Log($"[WaveManager] Debug jumped to Wave {CurrentWave}/{TotalWaves}");
        }
#endif

        private IEnumerator WaitForWaveStart(int waveNumber, WaveData wave)
        {
            float waitTime = Mathf.Max(0f, config.timeBetweenWaves);
            if (AscensionManager.Instance != null)
            {
                waitTime *= AscensionManager.Instance.GetWaveCountdownMultiplier();
            }
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
                float dt = Time.deltaTime > 0f ? Time.deltaTime : (Application.isPlaying ? 0f : 0.05f);
                NextWaveCountdown = Mathf.Max(0f, NextWaveCountdown - dt);
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
                    spawnPortals = found;
                }
            }

            SortPortalsByWorldX(spawnPortals);

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

        private Vector3[] GetRoutePoints(int laneIndex, Vector3 spawnPos)
        {
            Vector3 castlePos = castle != null
                ? new Vector3(castle.transform.position.x, spawnPos.y, castle.transform.position.z)
                : new Vector3(0f, spawnPos.y, 0.4f);
            return CombatLaneRouting.BuildRoute(laneIndex, spawnPos, castlePos);
        }

        private Enemy SpawnEnemy(EnemyData enemyData, WaveLaneAssignment assignment = WaveLaneAssignment.Auto)
        {
            return SpawnEnemyOnLane(enemyData, CombatLaneRouting.ResolveLane(
                assignment,
                spawnSequence,
                enemyData != null ? enemyData.classification : EnemyClassification.Normal));
        }

        internal Enemy SpawnWithAssignment(EnemyData enemyData, WaveLaneAssignment assignment)
        {
            return SpawnEnemy(enemyData, assignment);
        }

        internal Enemy SpawnEnemyOnLane(EnemyData enemyData, int laneIndex)
        {
            if (enemyData == null || enemyData.prefab == null)
            {
                Debug.LogWarning("WaveManager: wave entry has no enemy/prefab assigned.");
                return null;
            }

            EnsurePortals();
            if (enemyPool == null)
            {
                Debug.LogError("WaveManager: EnemyPoolManager is required for enemy spawning.");
                return null;
            }

            if (castleComponent == null && castle != null)
            {
                castleComponent = castle.GetComponent<Castle>();
            }

            int lane = CombatLaneRouting.ClampLane(laneIndex);
            spawnSequence++;

            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;
            EnemySpawnPortal selectedPortal = GetPresentationPortal(lane);

            Vector3[] portalPositions = CollectPortalPositions();
            Vector3 fallbackOrigin = selectedPortal != null
                ? selectedPortal.SpawnAnchor.position
                : (spawnPoint != null ? spawnPoint.transform.position : new Vector3(0f, 0.1f, 16f));

            spawnPos = CombatLaneRouting.ResolveSpawnPosition(
                lane,
                portalPositions,
                fallbackOrigin,
                fallbackLaneSeparation);
            spawnRot = selectedPortal != null ? selectedPortal.SpawnAnchor.rotation : Quaternion.identity;

            Vector3[] points = GetRoutePoints(lane, spawnPos);
            float laneOffset = CombatLaneRouting.ClampWithinLaneOffset(
                NextWithinLaneOffset(),
                withinLaneHalfWidth);
            float depthOffset = NextDepthOffset();

            Enemy enemy = enemyPool.Spawn(
                enemyData,
                spawnPos,
                spawnRot,
                points,
                castleComponent,
                laneOffset,
                depthOffset,
                lane);

            if (enemy == null)
            {
                Debug.LogError($"WaveManager: failed to spawn pooled enemy '{enemyData.name}'.", enemyData);
                return null;
            }

            selectedPortal?.PlaySpawnFlare();
            return enemy;
        }

        internal void ConfigureForTests(
            GameConfig testConfig,
            GameObject testSpawnPoint,
            GameObject testCastle,
            EnemySpawnPortal[] testPortals,
            EnemyPoolManager testPool)
        {
            config = testConfig;
            spawnPoint = testSpawnPoint;
            castle = testCastle;
            spawnPortals = testPortals;
            enemyPool = testPool;
            castleComponent = testCastle != null ? testCastle.GetComponent<Castle>() : null;
            spawnSequence = 0;
        }

        private Vector3[] CollectPortalPositions()
        {
            if (spawnPortals == null || spawnPortals.Length == 0)
            {
                return null;
            }

            SortPortalsByWorldX(spawnPortals);

            Vector3[] positions = new Vector3[spawnPortals.Length];
            int written = 0;
            for (int i = 0; i < spawnPortals.Length; i++)
            {
                if (spawnPortals[i] == null)
                {
                    continue;
                }

                positions[written++] = spawnPortals[i].SpawnAnchor.position;
            }

            if (written != positions.Length)
            {
                System.Array.Resize(ref positions, written);
            }

            return positions;
        }

        private EnemySpawnPortal GetPresentationPortal(int laneIndex)
        {
            if (spawnPortals != null && spawnPortals.Length >= CombatLaneRouting.LaneCount)
            {
                return spawnPortals[CombatLaneRouting.ClampLane(laneIndex)];
            }

            if (spawnPortals != null && spawnPortals.Length > 0)
            {
                return spawnPortals[0];
            }

            return spawnPortalPresentation;
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

        private float NextWithinLaneOffset()
        {
            float normalized = Mathf.Repeat(spawnSequence * 0.61803398875f, 1f);
            float halfWidth = Mathf.Clamp(withinLaneHalfWidth, 0f, CombatLaneRouting.MaxWithinLaneHalfWidth);
            return Mathf.Lerp(-halfWidth, halfWidth, normalized);
        }

        private float NextDepthOffset()
        {
            float jitter = Mathf.Min(spawnDepthJitter, 1.5f);
            return UnityEngine.Random.Range(-jitter, jitter);
        }

        private static void SortPortalsByWorldX(EnemySpawnPortal[] portals)
        {
            if (portals == null || portals.Length < 2)
            {
                return;
            }

            Array.Sort(portals, (a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            withinLaneHalfWidth = Mathf.Clamp(withinLaneHalfWidth, 0f, CombatLaneRouting.MaxWithinLaneHalfWidth);
            fallbackLaneSeparation = Mathf.Max(0.5f, fallbackLaneSeparation);
        }

        private void OnDrawGizmos()
        {
            EnemySpawnPortal[] gizmosPortals = spawnPortals;
            if (gizmosPortals == null || gizmosPortals.Length < CombatLaneRouting.LaneCount)
            {
                gizmosPortals = FindObjectsByType<EnemySpawnPortal>(FindObjectsSortMode.None);
            }

            if (gizmosPortals == null || gizmosPortals.Length < CombatLaneRouting.LaneCount)
            {
                return;
            }

            gizmosPortals = (EnemySpawnPortal[])gizmosPortals.Clone();
            SortPortalsByWorldX(gizmosPortals);

            Vector3 castlePos = castle != null
                ? castle.transform.position
                : new Vector3(0f, 0.1f, 0.4f);
            Color[] colors =
            {
                new Color(0.25f, 0.75f, 1f, 0.95f),
                new Color(1f, 0.85f, 0.2f, 0.95f),
                new Color(1f, 0.35f, 0.35f, 0.95f)
            };

            for (int lane = 0; lane < CombatLaneRouting.LaneCount; lane++)
            {
                if (gizmosPortals[lane] == null)
                {
                    continue;
                }

                Vector3[] route = CombatLaneRouting.BuildRoute(
                    lane,
                    gizmosPortals[lane].SpawnAnchor.position,
                    castlePos);
                Gizmos.color = colors[lane];
                for (int i = 0; i < route.Length - 1; i++)
                {
                    Gizmos.DrawLine(route[i], route[i + 1]);
                    Gizmos.DrawSphere(route[i], 0.28f);
                }

                Gizmos.DrawSphere(route[route.Length - 1], 0.35f);
                string label = lane == CombatLaneRouting.Left ? "LEFT" : (lane == CombatLaneRouting.Right ? "RIGHT" : "CENTER");
                UnityEditor.Handles.color = colors[lane];
                UnityEditor.Handles.Label(route[0] + Vector3.up * 1.2f, label);
            }
        }
#endif
    }
}
