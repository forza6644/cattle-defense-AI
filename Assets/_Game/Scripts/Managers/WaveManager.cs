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
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject spawnPoint;
        [SerializeField] private GameObject castle;

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
        public int TotalWaves => config != null && config.waves != null ? config.waves.Length : 0;
        public bool IsWaitingForWave { get; private set; }
        public float NextWaveCountdown { get; private set; }

        private Castle castleComponent;
        private bool startNextWaveRequested;

        private bool IsGameOver => castleComponent != null && castleComponent.IsGameOver;

        private void Start()
        {
            if (config == null || config.waves == null || config.waves.Length == 0 || spawnPoint == null || castle == null)
            {
                Debug.LogWarning("WaveManager: assign config (with waves), spawnPoint and castle in the Inspector.");
                return;
            }

            castleComponent = castle.GetComponent<Castle>();
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            for (int w = 0; w < config.waves.Length; w++)
            {
                if (IsGameOver)
                {
                    yield break;
                }

                WaveData wave = config.waves[w];
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
                    for (int i = 0; i < entry.count; i++)
                    {
                        if (IsGameOver)
                        {
                            yield break;
                        }

                        SpawnEnemy(entry.enemy);
                        yield return new WaitForSeconds(entry.spawnInterval);
                    }
                }

                // Wave ends when every spawned enemy is gone (killed or reached castle).
                while (EnemyManager.AliveCount > 0)
                {
                    if (IsGameOver)
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (IsGameOver)
                {
                    yield break;
                }

                Debug.Log("Wave " + CurrentWave + " cleared");
                WaveCleared?.Invoke(CurrentWave, wave);

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
                enemy.SetTarget(castle.transform);
            }
        }
    }
}
