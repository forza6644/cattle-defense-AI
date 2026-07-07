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

        [Header("Defender Identity")]
        public string defenderId;
        public string displayNameOverride;
        [TextArea(3, 5)]
        public string defenderDescription;
        public DefenderRole role;
        public DefenderRarity rarity;

        [Header("Combat Stats")]
        public float damage;
        public float range;
        [Tooltip("Shots per second (e.g. 1 = one shot every second, 0.4 = one shot every 2.5s).")]
        public float fireRate;

        [Header("Targeting")]
        [Tooltip("Default enemy selection mode when this tower is placed.")]
        public TargetingMode defaultTargetingMode = TargetingMode.ClosestToGoal;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        [Tooltip("Colour of the projectile's trail (Arrow white, Cannon orange, Frost cyan).")]
        public Color projectileTrailColor = Color.white;
        [Tooltip("Sound played when this tower fires. Leave empty to rely on the impact sound instead.")]
        public AudioClip attackSound;

        [Header("Splash (Cannon)")]
        [Tooltip("0 = single target. > 0 damages every enemy within this radius of the impact point.")]
        public float splashRadius;

        [Header("Slow (Frost)")]
        [Tooltip("Speed multiplier applied to enemies hit. 1 = no slow, 0.6 = 40% slower.")]
        public float slowMultiplier = 1f;
        [Tooltip("Seconds the slow lasts. Re-applying refreshes the timer (no stacking).")]
        public float slowDuration;

        [Header("Defender Effects (Future Use)")]
        public float dotDamage;
        public float dotDuration;
        public int chainCount;
        public float chainRange;

        [Header("Defender Scaling (Future Use)")]
        public float[] runLevelMultipliers = { 1f };
        public float[] metaLevelMultipliers = { 1f };

        [Header("Economy")]
        public int cost;

        [Header("Upgrade")]
        public int maxLevel;
        public float damageMultiplierPerLevel;
        public float rangeMultiplierPerLevel;
    }
}
