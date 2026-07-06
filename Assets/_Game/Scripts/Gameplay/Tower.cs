using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tower combat + upgrades. Every stat (damage, range, fire rate, splash, slow,
    /// costs, max level) comes from the assigned TowerData asset. Tracks the total
    /// gold invested (placement + upgrades) so selling can refund a fraction of it.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;

        private int level = 1;
        private float cooldownTimer;

        public TowerData Data => data;
        public int Level => level;
        public bool IsMaxLevel => data != null && level >= data.maxLevel;

        /// <summary>The slot this tower stands on (null for pre-placed towers).</summary>
        public TowerSlot Slot { get; set; }

        /// <summary>Gold spent on this tower so far (placement + upgrades).</summary>
        public int TotalInvested { get; private set; }

        /// <summary>Current damage: base scaled by the per-level multiplier.</summary>
        public float Damage => data.damage * Mathf.Pow(data.damageMultiplierPerLevel, level - 1);

        /// <summary>Current range: base scaled by the per-level multiplier.</summary>
        public float Range => data.range * Mathf.Pow(data.rangeMultiplierPerLevel, level - 1);

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

            Enemy target = FindNearestEnemyInRange();
            if (target != null && cooldownTimer <= 0f)
            {
                Fire(target);
                cooldownTimer = data.fireRate > 0f ? 1f / data.fireRate : 1f;
            }
        }

        private Enemy FindNearestEnemyInRange()
        {
            Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            Enemy nearest = null;
            float bestDistance = Range;

            foreach (Enemy enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void Fire(Enemy target)
        {
            if (data.projectilePrefab == null)
            {
                return;
            }

            GameObject shot = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);

            Projectile projectile = shot.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Init(target, Damage, data.splashRadius, data.slowMultiplier, data.slowDuration);
            }
        }
    }
}
