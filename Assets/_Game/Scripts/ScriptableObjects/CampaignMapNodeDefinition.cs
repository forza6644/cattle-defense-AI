using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "NewCampaignMapNode", menuName = "Stonehold/Campaign Map Node")]
    public class CampaignMapNodeDefinition : ScriptableObject
    {
        public int stageIndex;
        public string stageName;
        public string biomeTheme;
        [TextArea(2, 4)]
        public string loreBriefing;
        public Vector2 mapCoordinates = new Vector2(0f, 0f);
        public int requiredTotalStarsToUnlock = 0;
        public string star1Condition = "Clear all waves";
        public string star2Condition = "Clear with 70%+ Castle HP";
        public string star3Condition = "Clear on Ascension Heat 2+";
        public string sceneName = "GameplayIntegration_V2";
        public Color themeColor = new Color(0.3f, 0.7f, 1f);
        public string biomeIcon = "🏰";
    }
}
