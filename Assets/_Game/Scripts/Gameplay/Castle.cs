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
        private const float RegenIntervalSeconds = 5f;
        private float regenTimer;


        /// <summary>Raised whenever HP changes.</summary>
        public event Action HealthChanged;

        /// <summary>Raised after actual damage is applied. The argument is the applied amount.</summary>
        public event Action<int> DamageTaken;

        /// <summary>Raised after actual healing is applied. The argument is the applied amount.</summary>
        public event Action<int> Healed;

        /// <summary>Raised once when HP reaches zero.</summary>
        public event Action Defeated;

        public int CurrentHealth { get; private set; }
        public int MaxHealth
        {
            get
            {
                int baseMax = config != null ? config.castleMaxHealth : 0;
                if (MetaUpgradeManager.Instance != null)
                {
                    baseMax += MetaUpgradeManager.Instance.GetCastleHpBonus();
                }
                return baseMax;
            }
        }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("Castle: GameConfig not assigned.");
            }

            CurrentHealth = MaxHealth;
        }

        private void Update()
        {
            if (IsGameOver)
            {
                return;
            }

            regenTimer += Time.deltaTime;
            if (regenTimer < RegenIntervalSeconds)
            {
                return;
            }

            int elapsedTicks = Mathf.FloorToInt(regenTimer / RegenIntervalSeconds);
            regenTimer -= elapsedTicks * RegenIntervalSeconds;

            if (CurrentHealth >= MaxHealth)
            {
                return;
            }

            int regenPerTick = MetaUpgradeManager.Instance != null
                ? MetaUpgradeManager.Instance.GetCastleRegenPerTick()
                : 1;
            Repair(regenPerTick * elapsedTicks);
        }

        /// <summary>Called by an enemy when it reaches the castle.</summary>
        public void TakeDamage(int amount)
        {
            if (IsGameOver || amount <= 0)
            {
                return;
            }

            int appliedDamage = Mathf.Min(CurrentHealth, amount);
            CurrentHealth -= appliedDamage;
            Debug.Log("Castle hit! HP = " + CurrentHealth + " / " + MaxHealth);
            DamageTaken?.Invoke(appliedDamage);
            HealthChanged?.Invoke();

            if (CurrentHealth == 0)
            {
                IsGameOver = true;
                Debug.Log("GAME OVER");
                Defeated?.Invoke();
            }
        }

        /// <summary>Repairs the castle by a specified amount, capped at max health.</summary>
        public void Repair(int amount)
        {
            if (IsGameOver || CurrentHealth <= 0 || amount <= 0)
            {
                return;
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            int appliedHealing = CurrentHealth - previousHealth;
            if (appliedHealing <= 0)
            {
                return;
            }

            Debug.Log("Castle repaired! HP = " + CurrentHealth + " / " + MaxHealth);
            Healed?.Invoke(appliedHealing);
            HealthChanged?.Invoke();
        }
    }
}
