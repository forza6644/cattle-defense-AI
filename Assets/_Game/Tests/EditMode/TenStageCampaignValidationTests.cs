using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class TenStageCampaignValidationTests
    {
        [Test]
        public void GameConfig_ContainsAllTenConfiguredStages()
        {
#if UNITY_EDITOR
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Game/ScriptableObjects/GameConfig.asset");
#else
            var config = Resources.Load<GameConfig>("GameConfig");
#endif

            Assert.IsNotNull(config, "GameConfig should be loaded");
            Assert.IsNotNull(config.stages, "GameConfig stages array should not be null");
            Assert.AreEqual(10, config.stages.Length, "GameConfig should contain exactly 10 campaign stages");

            for (int i = 0; i < 10; i++)
            {
                var stage = config.stages[i];
                Assert.IsNotNull(stage, $"Stage at index {i} (Stage {i + 1}) should not be null");
                Assert.AreEqual(i + 1, stage.stageNumber, $"Stage at index {i} should have stageNumber {i + 1}");
                Assert.IsFalse(string.IsNullOrEmpty(stage.stageId), $"Stage {i + 1} stageId should not be empty");
                Assert.IsFalse(string.IsNullOrEmpty(stage.stageDisplayName), $"Stage {i + 1} displayName should not be empty");
                Assert.Greater(stage.enemyCountMultiplier, 0.5f, $"Stage {i + 1} enemy count multiplier should be valid");
                Assert.Greater(stage.spawnIntervalMultiplier, 0.2f, $"Stage {i + 1} spawn interval multiplier should be valid");
            }
        }

        [Test]
        public void AllTenStages_HaveTenValidEscalatingWavesEach()
        {
#if UNITY_EDITOR
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Game/ScriptableObjects/GameConfig.asset");
#else
            var config = Resources.Load<GameConfig>("GameConfig");
#endif

            Assert.IsNotNull(config);
            Assert.AreEqual(10, config.stages.Length);

            int totalWaves = 0;
            for (int s = 0; s < 10; s++)
            {
                var stage = config.stages[s];
                Assert.IsNotNull(stage.waves, $"Stage {s + 1} waves array should not be null");
                Assert.AreEqual(10, stage.waves.Length, $"Stage {s + 1} should have exactly 10 waves");

                for (int w = 0; w < 10; w++)
                {
                    var wave = stage.waves[w];
                    Assert.IsNotNull(wave, $"Wave {w + 1} in Stage {s + 1} should not be null");
                    Assert.IsNotNull(wave.spawns, $"Spawns in Stage {s + 1} Wave {w + 1} should not be null");
                    Assert.Greater(wave.spawns.Length, 0, $"Stage {s + 1} Wave {w + 1} should have at least 1 spawn entry");

                    for (int e = 0; e < wave.spawns.Length; e++)
                    {
                        var entry = wave.spawns[e];
                        Assert.IsNotNull(entry.enemy, $"EnemyData in Stage {s + 1} Wave {w + 1} spawn entry {e} must not be null");
                        Assert.Greater(entry.count, 0, $"Spawn count in Stage {s + 1} Wave {w + 1} entry {e} must be positive");
                        Assert.Greater(entry.spawnInterval, 0f, $"Spawn interval in Stage {s + 1} Wave {w + 1} entry {e} must be positive");
                    }
                    totalWaves++;
                }
            }

            Assert.AreEqual(100, totalWaves, "Total campaign waves across all 10 stages should be exactly 100");
        }

        [Test]
        public void CampaignProgressionManager_GeneratesTenMapNodesWithCorrectStarThresholds()
        {
            GameObject go = new GameObject("TestCampaignManager");
            var manager = go.AddComponent<CampaignProgressionManager>();

            try
            {
                manager.ResetCampaignProgress();
                var nodes = manager.AllNodes;

                Assert.IsNotNull(nodes, "Campaign nodes list should not be null");
                Assert.AreEqual(10, nodes.Count, "CampaignProgressionManager should register 10 nodes");

                int[] expectedStarRequirements = { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 };
                for (int i = 0; i < 10; i++)
                {
                    var node = nodes[i];
                    Assert.AreEqual(i, node.stageIndex, $"Node {i} should have stageIndex {i}");
                    Assert.AreEqual(expectedStarRequirements[i], node.requiredTotalStarsToUnlock, $"Node {i} required stars mismatch");
                    Assert.IsFalse(string.IsNullOrEmpty(node.stageName), $"Node {i} stageName should not be empty");
                    Assert.IsFalse(string.IsNullOrEmpty(node.biomeTheme), $"Node {i} biomeTheme should not be empty");
                    Assert.IsFalse(string.IsNullOrEmpty(node.biomeIcon), $"Node {i} biomeIcon should not be empty");
                }

                Assert.IsTrue(manager.IsStageUnlocked(0), "Stage 1 (index 0) should always be unlocked by default");
                Assert.IsFalse(manager.IsStageUnlocked(1), "Stage 2 (index 1) should be locked when stars = 0");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
