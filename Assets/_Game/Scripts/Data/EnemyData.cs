using UnityEngine;

namespace Stonehold
{
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
    }
}
