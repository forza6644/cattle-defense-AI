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

        /// <summary>Raised whenever any enemy takes damage: (enemy, amount, isCrit).</summary>
        public static event Action<Enemy, float, bool> AnyDamagedDetailed;

        /// <summary>Raised when any enemy dies to a tower: (enemy, gold awarded).</summary>
        public static event Action<Enemy, int> AnyKilled;

        [SerializeField] private EnemyData data;
        [SerializeField] private float arriveDistance = 0.1f;

        private static int globalActivationCounter = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            globalActivationCounter = 0;
            AnyDamaged = null;
            AnyDamagedDetailed = null;
            AnyKilled = null;
        }

        private void AssignUniqueActivationId()
        {
            globalActivationCounter = globalActivationCounter == int.MaxValue ? 1 : globalActivationCounter + 1;
            activationId = globalActivationCounter;
        }

        private Vector3[] pathPoints;
        private int currentWaypointIndex;
        private Castle targetCastle;
        private ProceduralAnimator animator;
        private float currentHealth;
        private float slowMultiplier = 1f;
        private float slowTimer;
        private bool isDead;
        private bool isAttackingCastle;
        private EnemyPoolManager poolOwner;
        private string poolKey;
        private int activationId;
        private bool isActiveActivation;
        private bool isRegistered;
        private bool rewardClaimed;
        private bool castleDamageApplied;
        private StatusEffectController statusController;
        private EnemyHealthBar healthBar;
        private Collider[] colliders;
        private Renderer[] renderers;
        private Rigidbody[] rigidbodies;

        public EnemyData Data => data;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => data != null ? data.health : 0f;
        public bool IsSlowed => slowTimer > 0f;
        public bool IsDead => isDead;
        public bool IsAttackingCastle => isAttackingCastle;
        public int ActivationId => activationId;
        public bool IsActiveActivation => isActiveActivation;
        public bool IsTargetable => isActiveActivation && !isDead && gameObject.activeInHierarchy;
        public string PoolKey => poolKey;

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
                if (isAttackingCastle)
                {
                    return 0f;
                }

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
            CacheRuntimeComponents();
        }

        private void CacheRuntimeComponents()
        {
            currentHealth = data != null ? data.health : 0f;
            healthBar = GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<EnemyHealthBar>();
            }
            if (data != null)
            {
                healthBar.Configure(this);
            }
            animator = GetComponent<ProceduralAnimator>();
            statusController = GetComponent<StatusEffectController>();
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            if (rigidbodies == null) rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        }

        private void OnEnable()
        {
            // Registration is intentionally owned by ActivateFromPool. Prewarmed
            // instances may briefly enable while Unity constructs them.
            if (poolOwner == null && data != null && !isActiveActivation)
            {
                AssignUniqueActivationId();
                isActiveActivation = true;
                currentHealth = data.health;
                rewardClaimed = false;
                castleDamageApplied = false;
                slowMultiplier = 1f;
                slowTimer = 0f;
                isDead = false;
                healthBar?.Configure(this);
                RegisterOnce();
            }
        }

        private void OnDisable()
        {
            UnregisterOnce();
            isActiveActivation = false;
        }

        private void OnDestroy()
        {
            UnregisterOnce();
        }

        internal void BindPool(EnemyPoolManager owner, string key)
        {
            if (poolOwner != null && poolOwner != owner)
            {
                Debug.LogError($"{name}: cannot move an enemy between pools.", this);
                return;
            }
            poolOwner = owner;
            poolKey = key;
        }

        public void PrepareForSpawn(EnemyData spawnData, Vector3 position, Quaternion rotation)
        {
            data = spawnData;
            CacheRuntimeComponents();
            transform.SetPositionAndRotation(position, rotation);
            currentHealth = data != null ? data.health : 0f;
            slowMultiplier = 1f;
            slowTimer = 0f;
            isDead = false;
            isAttackingCastle = false;
            currentWaypointIndex = 0;
            rewardClaimed = false;
            castleDamageApplied = false;
            pathPoints = null;
            targetCastle = null;

            statusController?.ResetController();
            animator?.ResetForReuse();
            SetRuntimeComponentsActive(true);
            healthBar.Configure(this);
        }

        public void ActivateFromPool(Vector3[] points, Castle castle, float laneOffset = 0f, float spawnDepthOffset = 0f)
        {
            AssignUniqueActivationId();
            isActiveActivation = true;
            SetPath(points, castle, laneOffset, spawnDepthOffset);
            RegisterOnce();
            animator?.SetMoving(true);
        }

        public void DespawnToPool()
        {
            UnregisterOnce();
            isActiveActivation = false;
            StopAllCoroutines();
            statusController?.ResetController();
            animator?.ResetForReuse();
            slowMultiplier = 1f;
            slowTimer = 0f;
            isDead = false;
            isAttackingCastle = false;
            rewardClaimed = false;
            castleDamageApplied = false;
            pathPoints = null;
            targetCastle = null;
            currentWaypointIndex = 0;
            SetRuntimeComponentsActive(false);
            healthBar.ResetForReuse();
            gameObject.SetActive(false);
        }

        public bool MatchesActivation(int expectedActivationId)
        {
            return isActiveActivation && activationId == expectedActivationId;
        }

        /// <summary>Called by the spawner right after this enemy is created to set its path.</summary>
        public void SetPath(Vector3[] points, Castle castle, float laneOffset = 0f, float spawnDepthOffset = 0f)
        {
            if (points != null)
            {
                if (pathPoints == null || pathPoints.Length != points.Length)
                {
                    pathPoints = new Vector3[points.Length];
                }
                for (int i = 0; i < points.Length; i++)
                {
                    float localJitter = i > 0 && i < points.Length - 1
                        ? UnityEngine.Random.Range(-0.25f, 0.25f)
                        : 0f;
                    pathPoints[i] = points[i] + Vector3.right * (laneOffset + localJitter);
                    if (i == 0)
                    {
                        pathPoints[i] += Vector3.forward * spawnDepthOffset;
                    }
                    else if (i == 1)
                    {
                        pathPoints[i] += Vector3.forward * (spawnDepthOffset * 0.45f);
                    }
                }
            }
            else
            {
                pathPoints = null;
            }

            currentWaypointIndex = 0;
            targetCastle = castle;
            isAttackingCastle = false;
            if (pathPoints != null && pathPoints.Length > 0)
            {
                transform.position = pathPoints[0];
            }
        }

        /// <summary>Called by projectiles. Kills the enemy (awarding gold) at 0 HP.</summary>
        public float TakeDamage(float amount, bool ignoreArmor = false, bool isCrit = false)
        {
            if (isDead || (poolOwner != null && !isActiveActivation))
            {
                return 0f;
            }

            float reducedAmount = amount;
            if (!ignoreArmor && data != null && data.armor > 0f)
            {
                reducedAmount = Mathf.Max(1f, amount - data.armor);
            }

            if (statusController != null && statusController.IsShocked())
            {
                // INTENTIONAL MVP BEHAVIOR: Shock increases all incoming damage by +30%, including Burn DoT ticks.
                // This creates a synergy between Fire (Burn) and Electric (Shock) heroes.
                // Note: Balance tuning might be required later.
                reducedAmount *= 1.3f;
            }

            currentHealth -= reducedAmount;
            AnyDamaged?.Invoke(this, reducedAmount);
            AnyDamagedDetailed?.Invoke(this, reducedAmount, isCrit);

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
            if (isDead || (poolOwner != null && !isActiveActivation)) return;

            if (statusController == null)
            {
                statusController = gameObject.AddComponent<StatusEffectController>();
            }
            statusController.ApplyEffect(effect);
        }

        /// <summary>Non-stacking slow: the newest slow replaces the current one.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            ApplyStatusEffect(new StatusEffect(StatusEffectType.Slow, multiplier, duration));
        }

        /// <summary>Death by tower: awards gold, then removes the enemy.</summary>
        public void Kill()
        {
            if (isDead || (poolOwner != null && !isActiveActivation))
            {
                return;
            }

            isDead = true;
            UnregisterOnce();

            if (!rewardClaimed && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(data.goldReward);
            }

            if (!rewardClaimed)
            {
                rewardClaimed = true;
                AnyKilled?.Invoke(this, data.goldReward);
            }

            int deathActivationId = activationId;
            Action complete = () =>
            {
                if (poolOwner != null)
                {
                    poolOwner.Despawn(this, deathActivationId);
                }
                else if (this != null)
                {
                    Destroy(gameObject);
                }
            };

            if (animator != null)
            {
                animator.PlayDeath(complete);
            }
            else
            {
                complete();
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (!isActiveActivation || pathPoints == null || pathPoints.Length == 0 || isDead)
            {
                return;
            }

            if (isAttackingCastle)
            {
                AttackCastle();
                return;
            }

            if (targetCastle != null)
            {
                float distToCastle = Vector3.Distance(transform.position, targetCastle.transform.position);
                if (distToCastle <= 2.2f)
                {
                    ReachCastle();
                    return;
                }
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
            if (isDead || !isActiveActivation || castleDamageApplied) return;
            isAttackingCastle = true;
            if (animator != null)
            {
                animator.SetMoving(false);
                animator.PlayAttack();
            }
            AttackCastle();
        }

        private void AttackCastle()
        {
            if (!isActiveActivation || castleDamageApplied || targetCastle == null || targetCastle.IsGameOver)
            {
                return;
            }

            Vector3 direction = targetCastle.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    12f * Time.deltaTime);
            }

            castleDamageApplied = true;
            targetCastle.TakeDamage(data.castleDamage);
            int arrivalActivationId = activationId;
            if (poolOwner != null)
            {
                poolOwner.Despawn(this, arrivalActivationId);
            }
        }

        private void RegisterOnce()
        {
            if (isRegistered || !isActiveActivation) return;
            EnemyManager.Register(this);
            isRegistered = true;
        }

        private void UnregisterOnce()
        {
            if (!isRegistered) return;
            EnemyManager.Unregister(this);
            isRegistered = false;
        }

        private void SetRuntimeComponentsActive(bool active)
        {
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null) colliders[i].enabled = active;
                }
            }
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].enabled = active;
                }
            }
            if (rigidbodies != null)
            {
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    Rigidbody body = rigidbodies[i];
                    if (body == null) continue;
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.Sleep();
                }
            }
        }
    }
}
