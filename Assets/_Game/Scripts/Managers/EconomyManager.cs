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

        public int Gold { get; private set; }

        private void Awake()
        {
            Instance = this;

            if (config == null)
            {
                Debug.LogWarning("EconomyManager: GameConfig not assigned.");
            }

            Gold = config != null ? config.startingGold : 0;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GoldChanged?.Invoke();
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
