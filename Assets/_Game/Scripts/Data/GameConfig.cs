using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Global run configuration: starting gold, castle HP, the ordered wave list and
    /// global pacing/economy rules. Single source of truth for run-wide tuning.
    /// One asset under ScriptableObjects.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Stonehold/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        public int startingGold;
        [Tooltip("Fraction of a tower's total invested gold refunded when sold.")]
        [Range(0f, 1f)]
        public float sellRefundPercent;

        [Header("Castle")]
        public int castleMaxHealth;

        [Header("Waves (in order)")]
        public WaveData[] waves;
        [Tooltip("Pause in seconds between one wave being cleared and the next starting.")]
        public float timeBetweenWaves;
    }
}
