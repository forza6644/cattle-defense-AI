using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Tower combat + upgrades. Finds the nearest enemy in range and fires a
    /// projectile on a cooldown. Can be upgraded up to level 3; each upgrade
    /// increases damage by 50%.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [SerializeField] private float range = 8f;
        [SerializeField] private float fireCooldown = 1f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float damage = 10f;

        private const int MaxLevel = 3;
        private int level = 1;
        private float cooldownTimer;

        public int Level => level;
        public float Damage => damage;

        /// <summary>Cost to upgrade from the current level. 1->2 = 50, 2->3 = 100.</summary>
        public int UpgradeCost => level == 1 ? 50 : 100;

        /// <summary>Called when the player clicks this tower. Spends gold and upgrades.</summary>
        public bool TryUpgrade()
        {
            if (level >= MaxLevel)
            {
                Debug.Log("Tower is already at max level (" + MaxLevel + ").");
                return false;
            }

            int cost = UpgradeCost;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(cost))
            {
                Debug.Log("Not enough gold to upgrade (need " + cost + ").");
                return false;
            }

            level++;
            damage *= 1.5f;
            Debug.Log("Tower upgraded to level " + level + " for " + cost + " gold (damage now " + damage + ").");
            return true;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            Enemy target = FindNearestEnemyInRange();
            if (target != null && cooldownTimer <= 0f)
            {
                Fire(target);
                cooldownTimer = fireCooldown;
            }
        }

        private Enemy FindNearestEnemyInRange()
        {
            Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            Enemy nearest = null;
            float bestDistance = range;

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
            if (projectilePrefab == null)
            {
                return;
            }

            GameObject shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            Projectile projectile = shot.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetTarget(target);
            }
        }
    }
}
