using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Manages the 3-star campaign map progression across all 6 biomes,
    /// tracking star ratings, highest Ascension Heat cleared, high scores, and map unlocks.
    /// </summary>
    public class CampaignProgressionManager : MonoBehaviour
    {
        public static CampaignProgressionManager Instance { get; private set; }

        public static event Action OnCampaignProgressUpdated;

        private readonly List<CampaignMapNodeDefinition> allNodes = new List<CampaignMapNodeDefinition>();
        public IReadOnlyList<CampaignMapNodeDefinition> AllNodes
        {
            get
            {
                EnsureLoaded();
                return allNodes;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnCampaignProgressUpdated = null;
        }

        public static void ResetForTesting()
        {
            Instance = null;
            OnCampaignProgressUpdated = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }
            Instance = this;
            LoadCampaignNodes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureLoaded()
        {
            if (allNodes == null || allNodes.Count == 0)
            {
                LoadCampaignNodes();
            }
        }

        public void LoadCampaignNodes()
        {
            allNodes.Clear();
            var loaded = Resources.LoadAll<CampaignMapNodeDefinition>("CampaignNodes");
            if (loaded != null && loaded.Length > 0)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    allNodes.Add(loaded[i]);
                }
            }

            if (allNodes.Count == 0)
            {
                CreateDefaultNodes();
            }

            allNodes.Sort((a, b) => a.stageIndex.CompareTo(b.stageIndex));
        }

        public CampaignMapNodeDefinition GetNode(int stageIndex)
        {
            EnsureLoaded();
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i].stageIndex == stageIndex) return allNodes[i];
            }
            return null;
        }

        public int GetStarsForStage(int stageIndex)
        {
            return PlayerPrefs.GetInt("campaign_stars_stage_" + stageIndex, 0);
        }

        public int GetHighestHeatCleared(int stageIndex)
        {
            return PlayerPrefs.GetInt("campaign_heat_stage_" + stageIndex, 0);
        }

        public int GetBestScore(int stageIndex)
        {
            return PlayerPrefs.GetInt("campaign_score_stage_" + stageIndex, 0);
        }

        public int GetTotalStarsEarned()
        {
            EnsureLoaded();
            int total = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                total += GetStarsForStage(allNodes[i].stageIndex);
            }
            return total;
        }

        public bool IsStageUnlocked(int stageIndex)
        {
            EnsureLoaded();
            if (stageIndex == 0) return true;
            var node = GetNode(stageIndex);
            if (node == null) return false;

            return GetTotalStarsEarned() >= node.requiredTotalStarsToUnlock;
        }

        public int RecordStageResult(int stageIndex, bool victory, float castleHealthPercent, int heatLevel, int score)
        {
            int earnedStars = 0;
            if (victory)
            {
                earnedStars++; // Star 1: Victory
                if (castleHealthPercent >= 0.70f)
                {
                    earnedStars++; // Star 2: Castle Guardian (70%+ HP)
                }
                if (heatLevel >= 2)
                {
                    earnedStars++; // Star 3: Infernal Trial (Ascension Heat 2+)
                }
            }

            int currentStars = GetStarsForStage(stageIndex);
            if (earnedStars > currentStars)
            {
                PlayerPrefs.SetInt("campaign_stars_stage_" + stageIndex, earnedStars);
            }

            int bestScore = GetBestScore(stageIndex);
            if (score > bestScore)
            {
                PlayerPrefs.SetInt("campaign_score_stage_" + stageIndex, score);
            }

            int highestHeat = GetHighestHeatCleared(stageIndex);
            if (victory && heatLevel > highestHeat)
            {
                PlayerPrefs.SetInt("campaign_heat_stage_" + stageIndex, heatLevel);
            }

            PlayerPrefs.Save();

            if (victory)
            {
                SaveManager.UnlockStage(stageIndex + 2); // Unlock next stage in standard save
            }

            OnCampaignProgressUpdated?.Invoke();
            return earnedStars;
        }

        public void ResetCampaignProgress()
        {
            for (int i = 0; i < 10; i++)
            {
                PlayerPrefs.DeleteKey("campaign_stars_stage_" + i);
                PlayerPrefs.DeleteKey("campaign_score_stage_" + i);
                PlayerPrefs.DeleteKey("campaign_heat_stage_" + i);
            }
            PlayerPrefs.Save();
            OnCampaignProgressUpdated?.Invoke();
        }

        private void CreateDefaultNodes()
        {
            allNodes.Add(CreateDefaultNode(0, "Castle Road", "Grassy Plains", "The primary approach to Stonehold Citadel. Repel initial goblin raiders.", 0, new Vector2(-320f, -120f), "🏰", new Color(0.4f, 0.8f, 0.4f)));
            allNodes.Add(CreateDefaultNode(1, "Highlands Fortress", "Rocky Foothills", "Rugged rocky slopes swarming with armored orc battalions.", 2, new Vector2(-190f, 20f), "🏔️", new Color(0.8f, 0.6f, 0.3f)));
            allNodes.Add(CreateDefaultNode(2, "Frozen Pass", "Glacial Peaks", "Sub-zero mountain blizzards crawling with frost-bitten siege fiends.", 4, new Vector2(-60f, -80f), "❄️", new Color(0.4f, 0.85f, 1f)));
            allNodes.Add(CreateDefaultNode(3, "Volcanic Caldera", "Infernal Magma", "Scorched volcanic badlands flowing with living molten rock.", 6, new Vector2(70f, 50f), "🌋", new Color(1f, 0.45f, 0.2f)));
            allNodes.Add(CreateDefaultNode(4, "Toxic Mire", "Rotting Swamp", "Noxious poisonous wetlands hiding caustic abominations.", 8, new Vector2(200f, -60f), "☣️", new Color(0.5f, 0.9f, 0.3f)));
            allNodes.Add(CreateDefaultNode(5, "The Void Rift", "Abyssal Dimension", "A cosmic singularity tearing the fabric of reality itself.", 10, new Vector2(330f, 40f), "🌌", new Color(0.8f, 0.3f, 0.95f)));
        }

        private CampaignMapNodeDefinition CreateDefaultNode(int index, string name, string biome, string lore, int reqStars, Vector2 coords, string icon, Color color)
        {
            var node = ScriptableObject.CreateInstance<CampaignMapNodeDefinition>();
            node.stageIndex = index;
            node.stageName = name;
            node.biomeTheme = biome;
            node.loreBriefing = lore;
            node.requiredTotalStarsToUnlock = reqStars;
            node.mapCoordinates = coords;
            node.biomeIcon = icon;
            node.themeColor = color;
            return node;
        }
    }
}
