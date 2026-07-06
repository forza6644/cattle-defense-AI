using System.Collections;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Spawns continuous waves of enemies. Each wave spawns a random 5-10 enemies
    /// one at a time; the wave ends once every enemy is gone (killed or reached the
    /// castle). After a short delay the next wave begins. Loops until game over.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject spawnPoint;
        [SerializeField] private GameObject castle;

        [Header("Wave Settings")]
        [SerializeField] private int minEnemiesPerWave = 5;
        [SerializeField] private int maxEnemiesPerWave = 10;
        [SerializeField] private float spawnInterval = 0.8f;
        [SerializeField] private float timeBetweenWaves = 3f;

        private int waveNumber;

        private void Start()
        {
            if (enemyPrefab == null || spawnPoint == null || castle == null)
            {
                Debug.LogWarning("WaveManager: assign enemyPrefab, spawnPoint and castle in the Inspector.");
                return;
            }

            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            Castle castleComponent = castle.GetComponent<Castle>();

            while (castleComponent == null || !castleComponent.IsGameOver)
            {
                waveNumber++;
                int enemyCount = Random.Range(minEnemiesPerWave, maxEnemiesPerWave + 1);
                Debug.Log("Wave " + waveNumber + " starting - " + enemyCount + " enemies");

                for (int i = 0; i < enemyCount; i++)
                {
                    if (castleComponent != null && castleComponent.IsGameOver)
                    {
                        yield break;
                    }

                    SpawnEnemy();
                    yield return new WaitForSeconds(spawnInterval);
                }

                // Wave ends when every spawned enemy is gone (killed or reached castle).
                while (AnyEnemiesAlive())
                {
                    if (castleComponent != null && castleComponent.IsGameOver)
                    {
                        yield break;
                    }

                    yield return null;
                }

                Debug.Log("Wave " + waveNumber + " cleared");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private void SpawnEnemy()
        {
            GameObject spawned = Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);

            Enemy enemy = spawned.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.SetTarget(castle.transform);
            }
        }

        private bool AnyEnemiesAlive()
        {
            return Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0;
        }
    }
}
