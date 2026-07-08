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

        private Vector3[] pathPoints;
        private int currentWaypointIndex;
        private Castle targetCastle;
        private ProceduralAnimator animator;
        private float currentHealth;
        private float slowMultiplier = 1f;
        private float slowTimer;
        private bool isDead;

        public EnemyData Data => data;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => data != null ? data.health : 0f;
        public bool IsSlowed => slowTimer > 0f;
        public bool IsDead => isDead;

        public float SlowMultiplier
        {
            get => slowMultiplier;
            set => slowMultiplier = value;
        }

        public float SlowTimer
        {
            get => slowTimer;
            set => slowTimer = value;
        }
        public float RemainingDistanceToTarget
        {
            get
            {
                if (pathPoints == null || pathPoints.Length == 0 || currentWaypointIndex >= pathPoints.Length)
                {
                    return float.PositiveInfinity;
                }

                float distance = Vector3.Distance(transform.position, pathPoints[currentWaypointIndex]);
                for (int i = currentWaypointIndex; i < pathPoints.Length - 1; i++)
                {
                    distance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
                }
                return distance;
            }
        }

        private void Awake()
        {
            if (data == null)
            {
                Debug.LogWarning(name + ": EnemyData not assigned.");
                enabled = false;
                return;
            }

            currentHealth = data.health;
            animator = GetComponent<ProceduralAnimator>();
            if (animator != null)
            {
                animator.SetMoving(true);
            }
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

        /// <summary>Called by the spawner right after this enemy is created to set its path.</summary>
        public void SetPath(Vector3[] points, Castle castle)
        {
            pathPoints = points;
            currentWaypointIndex = 0;
            targetCastle = castle;
            if (pathPoints != null && pathPoints.Length > 0)
            {
                transform.position = pathPoints[0];
            }
        }

        /// <summary>Called by projectiles. Kills the enemy (awarding gold) at 0 HP.</summary>
        public float TakeDamage(float amount)
        {
            if (isDead)
            {
                return 0f;
            }

            float reducedAmount = amount;
            if (data != null && data.armor > 0f)
            {
                reducedAmount = Mathf.Max(1f, amount - data.armor);
            }

            StatusEffectController statusController = GetComponent<StatusEffectController>();
            if (statusController != null && statusController.IsShocked())
            {
                reducedAmount *= 1.3f;
            }

            currentHealth -= reducedAmount;
            AnyDamaged?.Invoke(this, reducedAmount);

            if (currentHealth <= 0f)
            {
                Kill();
            }
            else if (animator != null)
            {
                animator.PlayHit();
            }

            return reducedAmount;
        }

        /// <summary>Applies a status effect to the enemy.</summary>
        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (isDead) return;

            StatusEffectController controller = GetComponent<StatusEffectController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<StatusEffectController>();
            }
            controller.ApplyEffect(effect);
        }

        /// <summary>Non-stacking slow: the newest slow replaces the current one.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, multiplier, duration));
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

            if (animator != null)
            {
                animator.PlayDeath(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (pathPoints == null || pathPoints.Length == 0 || isDead)
            {
                return;
            }

            if (GetComponent<StatusEffectController>() == null)
            {
                if (slowTimer > 0f)
                {
                    slowTimer -= Time.deltaTime;
                    if (slowTimer <= 0f)
                    {
                        slowMultiplier = 1f;
                    }
                }
            }

            if (currentWaypointIndex >= pathPoints.Length)
            {
                ReachCastle();
                return;
            }

            Vector3 targetPosition = pathPoints[currentWaypointIndex];
            float speed = data.moveSpeed * slowMultiplier;

            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    12f * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= pathPoints.Length)
                {
                    ReachCastle();
                }
            }
        }

        private void ReachCastle()
        {
            if (isDead) return;
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
