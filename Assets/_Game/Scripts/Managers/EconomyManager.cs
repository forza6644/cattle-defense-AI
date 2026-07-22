using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tracks the single currency (Gold). Starting amount comes from GameConfig.
    /// Enemies killed add gold; placing, upgrading and selling towers move it.
    /// Exposed via a simple Instance so gameplay code can reach it without wiring.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private GameConfig config;

        /// <summary>Raised whenever the gold amount changes.</summary>
        public event Action GoldChanged;
        public event Action<int, int> WaveClearBonusAwarded;

        public int Gold { get; private set; }

        private WaveManager waves;

        private void Awake()
        {
            Instance = this;

            if (config == null)
            {
                Debug.LogWarning("EconomyManager: GameConfig not assigned.");
            }

            Gold = config != null ? config.startingGold : 0;
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
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GoldChanged?.Invoke();
        }

        private void OnWaveCleared(int waveNumber, WaveData wave)
        {
            if (config != null && config.draftRunMode)
            {
                WaveClearBonusAwarded?.Invoke(waveNumber, 0);
                return;
            }

            int bonus = config != null ? config.waveClearGoldBonus : 0;
            if (bonus <= 0)
            {
                return;
            }

            AddGold(bonus);
            WaveClearBonusAwarded?.Invoke(waveNumber, bonus);
            Debug.Log("Wave " + waveNumber + " clear bonus: +" + bonus + " gold");
        }

        /// <summary>Spend gold if there is enough. Returns false (and spends nothing) otherwise.</summary>
        public bool TrySpend(int amount)
        {
            if (Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            GoldChanged?.Invoke();
            return true;
        }
    }
}
