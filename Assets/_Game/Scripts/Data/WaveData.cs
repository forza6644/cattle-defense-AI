using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Defines one wave: a label (Learn/Challenge/Win) and the enemies to spawn.
    /// Designers create these assets under ScriptableObjects/Waves.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveData", menuName = "Stonehold/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Header("Identity")]
        public string waveLabel;

        [Header("Spawns")]
        public SpawnEntry[] spawns;

        [System.Serializable]
        public struct SpawnEntry
        {
            public EnemyData enemy;
            public int count;
            public float spawnInterval;
        }
    }
}
