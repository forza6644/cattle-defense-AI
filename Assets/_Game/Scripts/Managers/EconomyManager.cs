using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tracks the single currency (Gold). Enemies killed add gold; placing and
    /// upgrading towers spend it. Exposed via a simple Instance for easy access
    /// from enemies, slots and towers without wiring references everywhere.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private int startingGold = 100;

        public int Gold { get; private set; }

        private void Awake()
        {
            Instance = this;
            Gold = startingGold;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
        }

        /// <summary>Spend gold if there is enough. Returns false (and spends nothing) otherwise.</summary>
        public bool TrySpend(int amount)
        {
            if (Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            return true;
        }
    }
}
