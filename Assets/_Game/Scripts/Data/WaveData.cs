using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Optional explicit lane for a spawn entry. Auto (0) is the serialized default
    /// for existing wave assets and distributes enemies across Left/Center/Right.
    /// </summary>
    public enum WaveLaneAssignment
    {
        Auto = 0,
        Left = 1,
        Center = 2,
        Right = 3
    }

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
            [Min(0f)] public float startDelay;
            [Tooltip("Auto distributes across the three lanes. Left/Center/Right force single-lane pressure.")]
            public WaveLaneAssignment laneAssignment;
        }
    }
}
