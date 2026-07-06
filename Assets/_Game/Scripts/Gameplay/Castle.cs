using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// The base the player defends. Max HP comes from GameConfig; HP drops when an
    /// enemy reaches the castle. At 0 HP the run is over (Defeated is raised and
    /// spawning stops).
    /// </summary>
    public class Castle : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Raised whenever HP changes.</summary>
        public event Action HealthChanged;

        /// <summary>Raised once when HP reaches zero.</summary>
        public event Action Defeated;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => config != null ? config.castleMaxHealth : 0;
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("Castle: GameConfig not assigned.");
            }

            CurrentHealth = MaxHealth;
        }

        /// <summary>Called by an enemy when it reaches the castle.</summary>
        public void TakeDamage(int amount)
        {
            if (IsGameOver)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Debug.Log("Castle hit! HP = " + CurrentHealth + " / " + MaxHealth);
            HealthChanged?.Invoke();

            if (CurrentHealth == 0)
            {
                IsGameOver = true;
                Debug.Log("GAME OVER");
                Defeated?.Invoke();
            }
        }
    }
}
