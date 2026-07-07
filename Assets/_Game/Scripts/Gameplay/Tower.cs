using UnityEngine;

namespace Stonehold
{
    /// <summary>
        /// Tower combat + upgrades. Every stat (damage, range, fire rate, splash, slow,
        /// costs, max level) comes from the assigned TowerData asset. Target acquisition
        /// prioritizes enemies closest to the castle through the EnemyManager registry.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;

        private int level = 1;
        private float cooldownTimer;
        private ProceduralAnimator animator;
        private TargetingMode currentTargetingMode;

        public TowerData Data => data;
        public int Level => level;
        public bool IsMaxLevel => data != null && level >= data.maxLevel;
        public TargetingMode CurrentTargetingMode
        {
            get => currentTargetingMode;
            set => currentTargetingMode = value;
        }

        /// <summary>The slot this tower stands on (null for pre-placed towers).</summary>
        public TowerSlot Slot { get; set; }

        /// <summary>Gold spent on this tower so far (placement + upgrades).</summary>
        public int TotalInvested { get; private set; }

        /// <summary>Current damage: base scaled by the per-level multiplier and run-wide progression multiplier.</summary>
        public float Damage
        {
            get
            {
                float dmg = data.damage * Mathf.Pow(data.damageMultiplierPerLevel, level - 1);
                if (RunProgressionManager.Instance != null)
                {
                    dmg *= RunProgressionManager.Instance.RunDamageMultiplier;
                }
                return dmg;
            }
        }

        /// <summary>Current range: base scaled by the per-level multiplier and run-wide progression multiplier.</summary>
        public float Range
        {
            get
            {
                float baseRange = data.range * Mathf.Pow(data.rangeMultiplierPerLevel, level - 1);
                if (RunProgressionManager.Instance != null)
                {
                    baseRange *= RunProgressionManager.Instance.RunRangeMultiplier;
                }
                return baseRange;
            }
        }

        /// <summary>Cost to upgrade from the current level: base cost x current level.</summary>
        public int UpgradeCost => data.cost * level;

        private void Awake()
        {
            if (data == null)
            {
                Debug.LogWarning(name + ": TowerData not assigned.");
                enabled = false;
                return;
            }

            TotalInvested = data.cost;
            animator = GetComponent<ProceduralAnimator>();
            currentTargetingMode = data.defaultTargetingMode;
        }

        /// <summary>Spends gold and upgrades this tower one level.</summary>
        public bool TryUpgrade()
        {
            if (IsMaxLevel)
            {
                return false;
            }

            int cost = UpgradeCost;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(cost))
            {
                return false;
            }

            level++;
            TotalInvested += cost;
            Debug.Log(data.towerName + " upgraded to level " + level + " for " + cost + " gold (damage now " + Damage + ").");
            return true;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            Enemy target = EnemyManager.FindTarget(transform.position, Range, currentTargetingMode);
            if (target != null && cooldownTimer <= 0f)
            {
                Fire(target);
                float rate = data.fireRate;
                if (RunProgressionManager.Instance != null)
                {
                    rate *= RunProgressionManager.Instance.RunFireRateMultiplier;
                }
                cooldownTimer = rate > 0f ? 1f / rate : 1f;
            }
        }

        private void Fire(Enemy target)
        {
            if (data.projectilePrefab == null)
            {
                return;
            }

            if (animator != null)
            {
                animator.PlayAttack();
            }

            if (data.attackSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(data.attackSound, 0.7f);
            }

            Projectile projectile = Projectile.Spawn(data.projectilePrefab, transform.position);
            if (projectile != null)
            {
                projectile.Init(target, Damage, data.splashRadius, data.slowMultiplier, data.slowDuration, data.projectileTrailColor);
            }
        }
    }
}
