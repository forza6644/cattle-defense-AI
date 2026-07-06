using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tower combat + upgrades. Every stat (damage, range, fire rate, splash, slow,
    /// costs, max level) comes from the assigned TowerData asset — nothing hardcoded.
    /// Finds the nearest enemy in range and fires a projectile on a cooldown.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;

        private int level = 1;
        private float cooldownTimer;

        public TowerData Data => data;
        public int Level => level;

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
            }
        }

        /// <summary>Called when the player clicks this tower. Spends gold and upgrades.</summary>
        public bool TryUpgrade()
        {
            if (level >= data.maxLevel)
            {
                Debug.Log(data.towerName + " is already at max level (" + data.maxLevel + ").");
                return false;
            }

            int cost = UpgradeCost;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(cost))
            {
                Debug.Log("Not enough gold to upgrade (need " + cost + ").");
                return false;
            }

            level++;
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
