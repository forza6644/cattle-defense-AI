using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Stonehold/Stage Data")]
    public class StageData : ScriptableObject
    {
        public string stageId;
        public string stageDisplayName;
        [TextArea(3, 5)]
        public string stageDescription;
        public int stageNumber;
        public StageMode stageMode = StageMode.CastleDefense;

        [Header("Waves")]
        public WaveData[] waves;

        [Header("Stage Pacing")]
        [Min(0.5f)] public float enemyCountMultiplier = 1f;
        [Range(0.5f, 1.5f)] public float spawnIntervalMultiplier = 1f;

        [Header("Roster Info")]
        public EnemyData[] expectedEnemyTypes;
    }
}
