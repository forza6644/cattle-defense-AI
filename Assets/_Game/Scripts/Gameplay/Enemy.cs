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

        /// <summary>Raised when a boss transitions to a new phase: (bossEnemy, newPhaseIndex, healthPercent).</summary>
        public static event Action<Enemy, int, float> BossPhaseTransition;

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
            BossPhaseTransition = null;
        }

        private void AssignUniqueActivationId()
        {
            int attempts = Mathf.Max(2, EnemyManager.AliveCount + 2);
            do
            {
                globalActivationCounter = globalActivationCounter == int.MaxValue ? 1 : globalActivationCounter + 1;
                attempts--;
            }
            while (attempts > 0 && IsActivationIdInUse(globalActivationCounter));

            activationId = globalActivationCounter;
        }

        private bool IsActivationIdInUse(int candidate)
        {
            var enemies = EnemyManager.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && enemy != this && enemy.IsActiveActivation && enemy.ActivationId == candidate)
                {
                    return true;
                }
            }
            return false;
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
        private EnemySpecialBehavior specialBehavior;
        private BattlefieldDefenseRuntime blockingDefense;
        private float defenseAttackTimer;

        public EnemyData Data => data;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => data != null ? data.health : 0f;
        public bool IsSlowed => slowTimer > 0f;
        public bool IsDead => isDead;
        public bool IsAttackingCastle => isAttackingCastle;
        public int ActivationId => activationId;
        public bool IsActiveActivation => isActiveActivation;
        public bool IsTargetable => isActiveActivation && !isDead && gameObject.activeInHierarchy && (specialBehavior == null || !specialBehavior.IsPhaseShifted);
        public StatusEffectController StatusController => statusController;
        public string PoolKey => poolKey;
        public Castle TargetCastle => targetCastle;
        public int BossPhase { get; private set; } = 1;
        public float CurrentShield { get; private set; }
        public float MaxShield { get; private set; }
        public bool IsEnraged { get; private set; }

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
            specialBehavior = GetComponent<EnemySpecialBehavior>();
            if (specialBehavior == null && data != null && data.specialRole != EnemySpecialRole.None)
            {
                specialBehavior = gameObject.AddComponent<EnemySpecialBehavior>();
            }
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
            if (data != null && !string.IsNullOrEmpty(data.stableId))
            {
                BestiaryManager.Instance?.RegisterEncounter(data.stableId);
            }
            float hp = data != null ? data.health : 0f;
            if (data != null && (data.classification == EnemyClassification.Elite || data.classification == EnemyClassification.Boss))
            {
                if (AscensionManager.Instance != null)
                {
                    hp *= AscensionManager.Instance.GetEliteHealthMultiplier();
                }
            }
            currentHealth = hp;
            slowMultiplier = 1f;
            slowTimer = 0f;
            BossPhase = 1;
            MaxShield = data != null ? data.shieldCapacity : 0f;
            if (MaxShield <= 0f && AscensionManager.Instance != null && UnityEngine.Random.value < AscensionManager.Instance.GetNullifierShieldChance())
            {
                MaxShield = hp * 0.35f;
            }
            CurrentShield = MaxShield;
            IsEnraged = false;
            isDead = false;
            isAttackingCastle = false;
            currentWaypointIndex = 0;
            rewardClaimed = false;
            castleDamageApplied = false;
            pathPoints = null;
            targetCastle = null;

            statusController?.ResetController();
            animator?.ResetForReuse();
            specialBehavior?.PrepareForSpawn(this);
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
            specialBehavior?.Activate(targetCastle, activationId);
        }

        public void DespawnToPool()
        {
            UnregisterOnce();
            isActiveActivation = false;
            StopAllCoroutines();
            statusController?.ResetController();
            animator?.ResetForReuse();
            specialBehavior?.ResetForReuse();
            blockingDefense = null;
            defenseAttackTimer = 0f;
            slowMultiplier = 1f;
            slowTimer = 0f;
            CurrentShield = 0f;
            MaxShield = 0f;
            IsEnraged = false;
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
                    float t = points.Length > 1 ? (float)i / (points.Length - 1) : 0f;
                    pathPoints[i] = points[i] + Vector3.right * laneOffset + Vector3.forward * (spawnDepthOffset * (1f - t));
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
        public float TakeDamage(float amount, bool ignoreArmor = false, bool isCrit = false, string sourceHeroId = null)
        {
            if (isDead || (poolOwner != null && !isActiveActivation))
            {
                return 0f;
            }

            float reducedAmount = amount;
            float totalArmor = data != null ? data.armor : 0f;
            if (AscensionManager.Instance != null)
            {
                totalArmor += AscensionManager.Instance.GetEnemyArmorBonus();
            }
            if (!ignoreArmor && totalArmor > 0f)
            {
                reducedAmount = Mathf.Max(1f, amount - totalArmor);
            }

            if (statusController != null && statusController.IsShocked())
            {
                // INTENTIONAL MVP BEHAVIOR: Shock increases all incoming damage by +30%, including Burn DoT ticks.
                // This creates a synergy between Fire (Burn) and Electric (Shock) heroes.
                // Note: Balance tuning might be required later.
                reducedAmount *= 1.3f;
            }

            if (statusController != null && statusController.IsVulnerableToShatter)
            {
                bool isPhysicalHero = sourceHeroId == "archer" || sourceHeroId == "bombardier" || sourceHeroId == "sniper";
                if (isPhysicalHero || isCrit)
                {
                    float shatterMult = 1.30f;
                    if (!string.IsNullOrEmpty(sourceHeroId) && RunModifierManager.Instance != null)
                    {
                        shatterMult *= RunModifierManager.Instance.GetShatterBonusMultiplier(sourceHeroId);
                        if (RunModifierManager.Instance.HasBehavior(sourceHeroId, HeroBehaviorEffectType.ExecutionerCrit))
                        {
                            shatterMult += 0.35f;
                        }
                    }
                    reducedAmount *= shatterMult;
                    StatusEffectController.TriggerReactionEvent(ElementalReactionType.Shatter, transform.position, sourceHeroId);
                }
            }

            // Shield Absorption
            if (CurrentShield > 0f)
            {
                if (CurrentShield >= reducedAmount)
                {
                    CurrentShield -= reducedAmount;
                    AnyDamaged?.Invoke(this, reducedAmount);
                    AnyDamagedDetailed?.Invoke(this, reducedAmount, isCrit);
                    FloatingCombatTextManager.Instance?.SpawnCustomText(transform.position, $"🛡️ {Mathf.RoundToInt(reducedAmount)}", new Color(0.4f, 0.8f, 1f));
                    return reducedAmount;
                }
                else
                {
                    float absorbed = CurrentShield;
                    CurrentShield = 0f;
                    reducedAmount -= absorbed;
                    FloatingCombatTextManager.Instance?.SpawnCustomText(transform.position, "🛡️ BROKEN!", new Color(0.4f, 0.8f, 1f), 1.2f, true);
                }
            }

            currentHealth -= reducedAmount;
            AnyDamaged?.Invoke(this, reducedAmount);
            AnyDamagedDetailed?.Invoke(this, reducedAmount, isCrit);

            if (data != null && currentHealth > 0f)
            {
                float hpPercent = currentHealth / Mathf.Max(1f, data.health);
                if (data.classification == EnemyClassification.Boss)
                {
                    if (BossPhase == 1 && hpPercent <= 0.50f)
                    {
                        BossPhase = 2;
                        slowMultiplier *= 1.25f;
                        BossPhaseTransition?.Invoke(this, 2, hpPercent);
                    }
                }

                // Berserker Rage Affix check
                if ((data.affix == EnemyAffixType.BerserkerRage || data.classification == EnemyClassification.Elite) && hpPercent <= 0.40f && !IsEnraged)
                {
                    IsEnraged = true;
                    slowMultiplier = Mathf.Max(1.35f, slowMultiplier * 1.35f);
                    FloatingCombatTextManager.Instance?.SpawnCustomText(transform.position, "⚡ ENRAGED!", new Color(1f, 0.2f, 0.2f), 1.3f, true);
                }
            }

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

        public void RemoveStatusEffectsFromSource(string sourceId)
        {
            statusController?.RemoveEffectsFromSource(sourceId);
        }

        public float RestoreHealth(float amount)
        {
            if (amount <= 0f || isDead || !isActiveActivation || data == null)
            {
                return 0f;
            }

            float previous = currentHealth;
            currentHealth = Mathf.Min(data.health, currentHealth + amount);
            return currentHealth - previous;
        }

        /// <summary>Death by tower: awards gold, then removes the enemy.</summary>
        public void Kill()
        {
            if (isDead || (poolOwner != null && !isActiveActivation))
            {
                return;
            }

            isDead = true;
            specialBehavior?.CancelPendingActions();
            UnregisterOnce();

            if (data != null && !string.IsNullOrEmpty(data.stableId))
            {
                BestiaryManager.Instance?.RegisterKill(data.stableId);
            }
            CombatTelemetryManager.RecordKill();

            AchievementManager.Instance?.AddProgress("achv_first_blood", 1);
            AchievementManager.Instance?.AddProgress("achv_slayer_100", 1);
            AchievementManager.Instance?.AddProgress("achv_slayer_500", 1);
            if (data != null && data.classification == EnemyClassification.Boss)
            {
                AchievementManager.Instance?.AddProgress("achv_boss_slayer", 1);
            }

            int goldReward = data != null ? data.goldReward : 0;
            if (data != null && (data.classification == EnemyClassification.Elite || data.classification == EnemyClassification.Boss))
            {
                if (RelicManager.Instance != null)
                {
                    goldReward = Mathf.RoundToInt(goldReward * RelicManager.Instance.GetEliteBossGoldMultiplier());
                }
            }

            if (!rewardClaimed && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(goldReward);
            }

            if (!rewardClaimed)
            {
                rewardClaimed = true;
                AnyKilled?.Invoke(this, goldReward);
            }

            if (data != null && data.affix == EnemyAffixType.VolatileExplosive)
            {
                TriggerVolatileExplosion();
            }

            statusController?.OnEnemyDeath();

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


            if (TickBlockingDefense())
            {
                animator?.SetMoving(false);
                return;
            }

            if (specialBehavior != null && specialBehavior.Tick())
            {
                animator?.SetMoving(false);
                return;
            }
            animator?.SetMoving(true);

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

            if (AscensionManager.Instance != null && AscensionManager.Instance.GetEnemyRegenPercent() > 0f && !isDead && data != null)
            {
                float maxHp = data.health * (data.classification == EnemyClassification.Elite || data.classification == EnemyClassification.Boss ? (AscensionManager.Instance != null ? AscensionManager.Instance.GetEliteHealthMultiplier() : 1f) : 1f);
                if (currentHealth < maxHp)
                {
                    currentHealth = Mathf.Min(maxHp, currentHealth + (maxHp * AscensionManager.Instance.GetEnemyRegenPercent() * Time.deltaTime));
                }
            }

            Vector3 targetPosition = pathPoints[currentWaypointIndex];
            float speed = data.moveSpeed * slowMultiplier;
            if (AscensionManager.Instance != null)
            {
                speed *= AscensionManager.Instance.GetEnemySpeedMultiplier();
            }

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

        private bool TickBlockingDefense()
        {
            BattlefieldDefenseRuntime active = BattlefieldDefenseManager.Instance != null
                ? BattlefieldDefenseManager.Instance.ActiveDefense
                : null;
            if (active == null || !active.IsActive) { blockingDefense = null; return false; }
            blockingDefense = active;
            float distance = Vector3.Distance(transform.position, active.transform.position);
            if (distance > active.MeleeStopRange) return false;
            Vector3 direction = active.transform.position - transform.position; direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
            defenseAttackTimer -= Time.deltaTime;
            if (defenseAttackTimer <= 0f)
            {
                defenseAttackTimer = 1f;
                animator?.PlayAttack();
                active.TakeDamage(data != null ? data.castleDamage : 1f, this, activationId);
            }
            return true;
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

        private void TriggerVolatileExplosion()
        {
            if (data == null) return;
            float radius = data.explosionRadius > 0f ? data.explosionRadius : 3.5f;
            float dmg = data.explosionDamage > 0f ? data.explosionDamage : 40f;
            float radiusSqr = radius * radius;

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayExplosion(transform.position, true);
            }
            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnCustomText(transform.position, "💥 BOOM!", new Color(1f, 0.5f, 0.1f), 1.3f, true);
            }

            var all = EnemyManager.All;
            if (all != null)
            {
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    if (i >= all.Count) continue;
                    Enemy other = all[i];
                    if (other != null && other != this && !other.IsDead)
                    {
                        if ((other.transform.position - transform.position).sqrMagnitude <= radiusSqr)
                        {
                            other.TakeDamage(dmg, true);
                        }
                    }
                }
            }
        }
    }
}
