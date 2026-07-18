using UnityEngine;

namespace Stonehold
{
    public enum EnemySpecialRole
    {
        None = 0,
        RangedCastleAttacker = 1,
        HealingElite = 2
    }

    [System.Serializable]
    public sealed class EnemyRangedAttackSettings
    {
        [Min(0f)] public float standOffRange;
        [Min(0f)] public float windUpSeconds;
        [Min(0f)] public float cooldownSeconds;
        [Min(0f)] public float projectileSpeed;
        public GameObject projectilePrefab;
    }

    [System.Serializable]
    public sealed class EnemyHealingPulseSettings
    {
        [Min(0f)] public float intervalSeconds;
        [Min(0f)] public float castSeconds;
        [Min(0f)] public float radius;
        [Range(0f, 1f)] public float maxHealthFraction;
        [Range(0f, 1f)] public float selfHealMultiplier;
        [Min(1)] public int targetCap = 1;
        public bool excludeBoss = true;
    }

    /// <summary>
    /// Configurable stats for one enemy type. Designers create these assets under
    /// ScriptableObjects/Enemies. Fields define the schema only; values set per-asset.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Stonehold/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string stableId;
        public string enemyName;
        public GameObject prefab;
        public EnemyClassification classification = EnemyClassification.Normal;

        [Header("Stats")]
        public float health;
        public float moveSpeed;
        [Tooltip("Direct damage reduction (minimum 1 damage taken).")]
        public float armor;

        [Header("Future Defense Contracts")]
        [Min(0f)] public float shieldCapacity;
        [Range(0f, 1f)] public float dodgeChance;
        public ElementalResistanceProfile elementalResistances;
        [Range(0f, 1f)] public float crowdControlResistance;

        [Header("Impact")]
        public int goldReward;
        public int xpValue;
        public int castleDamage;

        [Header("Expansion Role")]
        public EnemySpecialRole specialRole;
        public EnemyRangedAttackSettings rangedAttack = new EnemyRangedAttackSettings();
        public EnemyHealingPulseSettings healingPulse = new EnemyHealingPulseSettings();
    }
}
