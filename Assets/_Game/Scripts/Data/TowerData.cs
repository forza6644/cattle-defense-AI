using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Configurable stats for one tower type. Designers create these assets under
    /// ScriptableObjects/Towers and tune values in the Inspector — no code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "Stonehold/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [Header("Identity")]
        public string towerName;
        public GameObject prefab;

        [Header("Combat Stats")]
        public float damage;
        public float range;
        [Tooltip("Shots per second (e.g. 1 = one shot every second, 0.4 = one shot every 2.5s).")]
        public float fireRate;

        [Header("Projectile")]
        public GameObject projectilePrefab;

        [Header("Splash (Cannon)")]
        [Tooltip("0 = single target. > 0 damages every enemy within this radius of the impact point.")]
        public float splashRadius;

        [Header("Slow (Frost)")]
        [Tooltip("Speed multiplier applied to enemies hit. 1 = no slow, 0.6 = 40% slower.")]
        public float slowMultiplier = 1f;
        [Tooltip("Seconds the slow lasts. Re-applying refreshes the timer (no stacking).")]
        public float slowDuration;

        [Header("Economy")]
        public int cost;

        [Header("Upgrade")]
        public int maxLevel;
        public float damageMultiplierPerLevel;
        public float rangeMultiplierPerLevel;
    }
}
