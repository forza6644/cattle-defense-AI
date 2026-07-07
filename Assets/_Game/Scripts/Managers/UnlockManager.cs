using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Controls the tower unlock flow: Arrow from start, Cannon after Wave 1,
    /// Frost after Wave 2.
    /// </summary>
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        public event Action UnlocksChanged;
        public event Action<string> TowerUnlocked;

        private WaveManager waves;
        private int completedWaves;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            waves = FindFirstObjectByType<WaveManager>();
            if (waves != null)
            {
                waves.WaveCleared += OnWaveCleared;
            }
        }

        private void OnDestroy()
        {
            if (waves != null)
            {
                waves.WaveCleared -= OnWaveCleared;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsTowerUnlocked(TowerData tower)
        {
            return completedWaves >= GetRequiredClearedWaves(tower);
        }

        public string GetLockMessage(TowerData tower)
        {
            int requiredWave = GetRequiredClearedWaves(tower);
            return requiredWave > 0 ? "Unlocks after Wave " + requiredWave : string.Empty;
        }

        private void OnWaveCleared(int waveNumber, WaveData wave)
        {
            int previousCompletedWaves = completedWaves;
            completedWaves = Mathf.Max(completedWaves, waveNumber);

            if (completedWaves == previousCompletedWaves)
            {
                return;
            }

            UnlocksChanged?.Invoke();

            if (previousCompletedWaves < 1 && completedWaves >= 1)
            {
                TowerUnlocked?.Invoke("Cannon Defender unlocked");
            }

            if (previousCompletedWaves < 2 && completedWaves >= 2)
            {
                TowerUnlocked?.Invoke("Frost Defender unlocked");
            }
        }

        private static int GetRequiredClearedWaves(TowerData tower)
        {
            if (tower == null || string.IsNullOrEmpty(tower.towerName))
            {
                return 0;
            }

            string towerName = tower.towerName.ToLowerInvariant();
            if (towerName.Contains("frost"))
            {
                return 2;
            }

            if (towerName.Contains("cannon"))
            {
                return 1;
            }

            return 0;
        }
    }
}
