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

        [Header("Roster Info")]
        public EnemyData[] expectedEnemyTypes;
    }
}
