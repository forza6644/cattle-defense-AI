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

        public int CurrentWave { get; private set; }
        public int TotalWaves => activeWaves != null ? activeWaves.Length : 0;
        public bool IsWaitingForWave { get; private set; }
        public float NextWaveCountdown { get; private set; }

        private WaveData[] activeWaves;
        private Castle castleComponent;
        private WaypointPath waypointPath;
        private bool startNextWaveRequested;
        private int spawnSequence;

        private bool IsGameOver => castleComponent != null && castleComponent.IsGameOver;

        private void Start()
        {
            if (config != null && config.stages != null && config.stages.Length > SaveManager.SelectedStageIndex)
            {
                var stage = config.stages[SaveManager.SelectedStageIndex];
                if (stage != null && stage.waves != null && stage.waves.Length > 0)
                {
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

            castleComponent = castle.GetComponent<Castle>();

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
                    float waveProgress = activeWaves.Length > 1
                        ? (float)w / (activeWaves.Length - 1)
                        : 0f;
                    float progressionDensity = Mathf.Lerp(0.78f, 1.25f, waveProgress);
                    float randomDensity = UnityEngine.Random.Range(1f - countVariance, 1f + countVariance);
                    int adjustedCount = Mathf.Max(
                        1,
                        Mathf.CeilToInt(entry.count * enemyCountMultiplier * progressionDensity * randomDensity));
                    for (int i = 0; i < adjustedCount; i++)
                    {
                        if (IsGameOver)
                        {
                            yield break;
                        }

                        SpawnEnemy(entry.enemy);
                        float baseInterval = Mathf.Max(0.05f, entry.spawnInterval * spawnIntervalMultiplier);
                        float randomizedInterval = baseInterval * UnityEngine.Random.Range(0.7f, 1.3f);
                        yield return new WaitForSeconds(randomizedInterval);
                    }
                }

                // Wave ends when every spawned enemy is gone (killed or reached castle).
                float waitingForClearSeconds = 0f;
                float nextRegistrySweepSeconds = 0f;
                bool waitWarningLogged = false;
                while (EnemyManager.AliveCount > 0)
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
                    }

                    if (!waitWarningLogged && waitingForClearSeconds >= WaveClearWaitWarningSeconds)
                    {
                        waitWarningLogged = true;
                        Debug.LogWarning($"WaveManager: Wave {CurrentWave} has waited {WaveClearWaitWarningSeconds:0}s for {EnemyManager.AliveCount} registered enemies after spawning finished. Continuing to monitor and pruning stale/null entries only.");
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

        public void StartNextWaveNow()
        {
            if (IsWaitingForWave)
            {
                startNextWaveRequested = true;
            }
        }

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

        private void SpawnEnemy(EnemyData enemyData)
        {
            if (enemyData == null || enemyData.prefab == null)
            {
                Debug.LogWarning("WaveManager: wave entry has no enemy/prefab assigned.");
                return;
            }

            GameObject spawned = Instantiate(enemyData.prefab, spawnPoint.transform.position, Quaternion.identity);

            Enemy enemy = spawned.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (waypointPath != null && waypointPath.Points != null && waypointPath.Points.Length > 0)
                {
                    enemy.SetPath(waypointPath.Points, castleComponent, NextLaneOffset(), NextDepthOffset());
                }
                else
                {
                    Vector3[] fallbackPoints = new Vector3[] { spawnPoint.transform.position, castle.transform.position };
                    enemy.SetPath(fallbackPoints, castleComponent, NextLaneOffset(), NextDepthOffset());
                }
            }
        }

        private float NextLaneOffset()
        {
            // Golden-ratio spacing avoids repeated clumps while retaining organic variation.
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
