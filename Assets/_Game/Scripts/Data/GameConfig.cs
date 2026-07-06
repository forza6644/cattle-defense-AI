using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Global run configuration: starting gold, castle HP, and the ordered wave list.
    /// Single source of truth for prototype-wide tuning. One asset under ScriptableObjects.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Stonehold/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        public int startingGold;

        [Header("Castle")]
        public int castleMaxHealth;

        [Header("Waves (in order)")]
        public WaveData[] waves;
    }
}
