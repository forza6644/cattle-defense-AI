using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class Stage1BalanceBenchmarkTests
    {
        private StageData LoadStage1()
        {
#if UNITY_EDITOR
            var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stages/Stage1_CastleRoad.asset");
            if (stage != null) return stage;
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Game/ScriptableObjects/GameConfig.asset");
            if (config != null && config.stages != null && config.stages.Length > 0) return config.stages[0];
#endif
            return null;
        }

        [Test]
        public void Stage1_HasExactlyTenConfiguredWaves()
        {
            var stage = LoadStage1();
            Assert.IsNotNull(stage, "Stage 1 Castle Road asset must load successfully.");
            Assert.AreEqual("stage_1_castle_road", stage.stageId);
            Assert.IsNotNull(stage.waves, "Stage 1 waves array must not be null.");
            Assert.AreEqual(10, stage.waves.Length, "Stage 1 must contain exactly 10 waves.");

            for (int i = 0; i < 10; i++)
            {
                var wave = stage.waves[i];
                Assert.IsNotNull(wave, $"Wave {i + 1} must not be null.");
                Assert.IsNotNull(wave.spawns, $"Wave {i + 1} spawns must not be null.");
                Assert.Greater(wave.spawns.Length, 0, $"Wave {i + 1} must have at least one spawn entry.");
            }
        }

        [Test]
        public void Stage1_WavesIncorporateAllThreeLanes()
        {
            var stage = LoadStage1();
            Assert.IsNotNull(stage);

            bool hasExplicitLeft = false;
            bool hasExplicitCenter = false;
            bool hasExplicitRight = false;
            bool hasAuto = false;

            for (int w = 0; w < stage.waves.Length; w++)
            {
                var wave = stage.waves[w];
                foreach (var entry in wave.spawns)
                {
                    if (entry.laneAssignment == WaveLaneAssignment.Left) hasExplicitLeft = true;
                    if (entry.laneAssignment == WaveLaneAssignment.Center) hasExplicitCenter = true;
                    if (entry.laneAssignment == WaveLaneAssignment.Right) hasExplicitRight = true;
                    if (entry.laneAssignment == WaveLaneAssignment.Auto) hasAuto = true;
                }
            }

            Assert.IsTrue(hasExplicitLeft, "Stage 1 waves must feature explicit Left lane assignments.");
            Assert.IsTrue(hasExplicitCenter, "Stage 1 waves must feature explicit Center lane assignments.");
            Assert.IsTrue(hasExplicitRight, "Stage 1 waves must feature explicit Right lane assignments.");
            Assert.IsTrue(hasAuto, "Stage 1 waves must feature Auto lane distribution for broad multi-lane pressure.");
        }

        [Test]
        public void Stage1_EnemyStatsAndRewards_AreValidAndTuned()
        {
#if UNITY_EDITOR
            var grunt = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/GruntData.asset");
            var runner = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/RunnerData.asset");
            var armored = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/ArmoredData.asset");
            var brute = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BruteData.asset");
            var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/BossData.asset");

            Assert.IsNotNull(grunt, "GruntData must exist");
            Assert.GreaterOrEqual(grunt.health, 30f, "Grunt HP must be calibrated >= 30 for tactical presence");
            Assert.Greater(grunt.moveSpeed, 2.5f, "Grunt move speed must be reasonable");
            Assert.Greater(grunt.goldReward, 0, "Grunt must give gold");
            Assert.Greater(grunt.xpValue, 0, "Grunt must give explicit XP");

            Assert.IsNotNull(runner, "RunnerData must exist");
            Assert.GreaterOrEqual(runner.health, 15f, "Runner HP must be calibrated >= 15");
            Assert.GreaterOrEqual(runner.moveSpeed, 4.0f, "Runner must be fast flanker");

            Assert.IsNotNull(armored, "ArmoredData must exist");
            Assert.GreaterOrEqual(armored.health, 60f, "Armored HP must be calibrated >= 60");
            Assert.GreaterOrEqual(armored.armor, 3f, "Armored must have armor >= 3");

            Assert.IsNotNull(brute, "BruteData must exist");
            Assert.GreaterOrEqual(brute.health, 200f, "Brute HP must be calibrated >= 200");
            Assert.GreaterOrEqual(brute.castleDamage, 2, "Brute must threaten castle with >= 2 damage");

            Assert.IsNotNull(boss, "BossData must exist");
            Assert.GreaterOrEqual(boss.health, 1000f, "Warlord Boss HP must be calibrated >= 1000 for >=25s fight");
            Assert.AreEqual(EnemyClassification.Boss, boss.classification, "Warlord Boss must be classified as Boss");
            Assert.GreaterOrEqual(boss.castleDamage, 5, "Boss must deal >= 5 castle damage");
#endif
        }

        [Test]
        public void Stage1_ProgressionXpCurve_AwardsMeasuredDraftCount()
        {
            GameObject go = new GameObject("TestProgression");
            var progression = go.AddComponent<RunProgressionManager>();

            try
            {
                Assert.AreEqual(100, progression.GetXpNeededForNextLevel(), "Level 1 -> 2 must require 100 XP");

                // Simulate earning ~2000 total XP over 10 waves
                int totalXpGained = 2033;
                progression.AddXp(totalXpGained);

                // Player should reach Level 6 (triggering 5 card drafts)
                Assert.GreaterOrEqual(progression.CurrentLevel, 5, "Stage 1 run should reach at least Level 5 (4 drafts)");
                Assert.LessOrEqual(progression.CurrentLevel, 7, "Stage 1 run should not exceed Level 7 (max 6 drafts)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Stage1_SpawnIntervalsAndCounts_AreWithinMobilePacingEnvelope()
        {
            var stage = LoadStage1();
            Assert.IsNotNull(stage);

            int totalEnemies = 0;
            for (int w = 0; w < stage.waves.Length; w++)
            {
                var wave = stage.waves[w];
                int waveEnemies = 0;

                foreach (var entry in wave.spawns)
                {
                    Assert.IsNotNull(entry.enemy, $"Spawn entry in Wave {w + 1} must reference an enemy asset.");
                    Assert.Greater(entry.count, 0, $"Spawn count in Wave {w + 1} must be > 0.");
                    Assert.GreaterOrEqual(entry.spawnInterval, 0.5f, $"Spawn interval in Wave {w + 1} must be >= 0.5s to prevent burst spam.");
                    Assert.LessOrEqual(entry.spawnInterval, 3.5f, $"Spawn interval in Wave {w + 1} must be <= 3.5s to prevent dead time.");
                    Assert.GreaterOrEqual(entry.startDelay, 0f, $"Start delay in Wave {w + 1} must be >= 0.");
                    waveEnemies += entry.count;
                }

                Assert.GreaterOrEqual(waveEnemies, 10, $"Wave {w + 1} must have at least 10 enemies.");
                Assert.LessOrEqual(waveEnemies, 50, $"Wave {w + 1} must have <= 50 enemies to prevent performance drops.");
                totalEnemies += waveEnemies;
            }

            Assert.GreaterOrEqual(totalEnemies, 200, "Stage 1 total enemies must be >= 200 for full 3-4 minute combat density.");
            Assert.LessOrEqual(totalEnemies, 350, "Stage 1 total enemies must be <= 350.");
        }

        [Test]
        public void Stage1_StageConfig_IsDeterministicAndValid()
        {
            var stage = LoadStage1();
            Assert.IsNotNull(stage);

            Assert.AreEqual(1.0f, stage.spawnIntervalMultiplier, 0.001f, "Stage 1 spawnIntervalMultiplier must be 1.0.");
            Assert.AreEqual(1, stage.enemyCountMultiplier, "Stage 1 enemyCountMultiplier must be 1.");
            Assert.IsTrue(stage.useExactWaveCounts, "Stage 1 must use exact wave counts.");
        }
    }
}
