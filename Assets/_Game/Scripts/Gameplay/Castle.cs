using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// The base the player defends. Holds castle HP and loses it when an enemy
    /// reaches it. When HP hits zero the game is over (spawning stops).
    /// </summary>
    public class Castle : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;

        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            currentHealth = maxHealth;
            IsGameOver = false;
        }

        /// <summary>Called by an enemy when it reaches the castle.</summary>
        public void TakeDamage(int amount)
        {
            if (IsGameOver)
            {
                return;
            }

            currentHealth -= amount;
            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            Debug.Log("Castle hit! HP = " + currentHealth + " / " + maxHealth);

            if (currentHealth == 0)
            {
                IsGameOver = true;
                Debug.Log("GAME OVER");
            }
        }
    }
}
