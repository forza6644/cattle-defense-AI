using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Configurable stats for one tower type. Designers create these assets under
    /// ScriptableObjects/Towers and tune values in the Inspector — no code changes.
    /// Fields define the schema only; balance values are set per-asset.
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
        public float fireRate;

        [Header("Economy")]
        public int cost;

        [Header("Upgrade")]
        public int maxLevel;
        public float damageMultiplierPerLevel;
        public float rangeMultiplierPerLevel;
    }
}
