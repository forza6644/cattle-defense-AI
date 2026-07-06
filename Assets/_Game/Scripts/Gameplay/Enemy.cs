using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Runtime component placed on every enemy prefab. All stats (HP, speed, gold,
    /// castle damage) come from the assigned EnemyData asset. Registers itself with
    /// the EnemyManager registry while alive. Supports a simple non-stacking slow.
    /// Raises static events the UI listens to for damage numbers and gold popups.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>Raised whenever any enemy takes damage: (enemy, amount).</summary>
        public static event Action<Enemy, float> AnyDamaged;

        /// <summary>Raised when any enemy dies to a tower: (enemy, gold awarded).</summary>
        public static event Action<Enemy, int> AnyKilled;

        [SerializeField] private EnemyData data;
        [SerializeField] private float arriveDistance = 0.1f;

        private Transform target;
        private Castle targetCastle;
        private float currentHealth;
        private float slowMultiplier = 1f;
        private float slowTimer;
        private bool isDead;

        public EnemyData Data => data;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => data != null ? data.health : 0f;
        public bool IsSlowed => slowTimer > 0f;

        private void Awake()
        {
            if (data == null)
            {
                Debug.LogWarning(name + ": EnemyData not assigned.");
                enabled = false;
                return;
            }

            currentHealth = data.health;
        }

        private void OnEnable()
        {
            if (data != null)
            {
                EnemyManager.Register(this);
            }
        }

        private void OnDestroy()
        {
            EnemyManager.Unregister(this);
        }

        /// <summary>Called by the spawner right after this enemy is created.</summary>
        public void SetTarget(Transform castle)
        {
            target = castle;
            targetCastle = castle != null ? castle.GetComponent<Castle>() : null;
        }

        /// <summary>Called by projectiles. Kills the enemy (awarding gold) at 0 HP.</summary>
        public void TakeDamage(float amount)
        {
            if (isDead)
            {
                return;
            }

            currentHealth -= amount;
            AnyDamaged?.Invoke(this, amount);

            if (currentHealth <= 0f)
            {
                Kill();
            }
        }

        /// <summary>Non-stacking slow: the newest slow replaces the current one.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Clamp01(multiplier);
            slowTimer = duration;
        }

        /// <summary>Death by tower: awards gold, then removes the enemy.</summary>
        public void Kill()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            EnemyManager.Unregister(this);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(data.goldReward);
            }

            AnyKilled?.Invoke(this, data.goldReward);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (target == null || isDead)
            {
                return;
            }

            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    slowMultiplier = 1f;
                }
            }

            float speed = data.moveSpeed * slowMultiplier;
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) <= arriveDistance)
            {
                isDead = true;
                EnemyManager.Unregister(this);

                if (targetCastle != null)
                {
                    targetCastle.TakeDamage(data.castleDamage);
                }

                Destroy(gameObject); // Reached the castle: no gold reward.
            }
        }
    }
}
