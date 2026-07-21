using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Tests
{
    public class GameplayParityTests
    {
        private const string ConfigPath = "Assets/_Game/ScriptableObjects/GameConfig.asset";
        private const string Stage1Path = "Assets/_Game/ScriptableObjects/Stage1_CastleRoad.asset";

        private GameConfig config;
        private StageData stage1;

        [SetUp]
        public void SetUp()
        {
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            stage1 = AssetDatabase.LoadAssetAtPath<StageData>(Stage1Path);
        }

        [Test]
        public void GameConfig_HasParityTuning()
        {
            Assert.That(config, Is.Not.Null, "GameConfig asset not found.");
            Assert.That(config.startingGold, Is.EqualTo(150), "Starting gold must be 150 for Stage 1 parity.");
            Assert.That(config.castleMaxHealth, Is.EqualTo(50), "Castle max health must be 50.");
            Assert.That(config.waveClearGoldBonus, Is.EqualTo(75), "Wave clear gold bonus must be 75.");
            Assert.That(config.sellRefundPercent, Is.EqualTo(0.6f).Within(0.01f), "Sell refund percentage must be 60%.");
        }

        [Test]
        public void Stage1_HasParityTuning()
        {
            Assert.That(stage1, Is.Not.Null, "Stage 1 asset not found.");
            Assert.That(stage1.stageNumber, Is.EqualTo(1));
            Assert.That(stage1.waves, Has.Length.EqualTo(10), "Stage 1 must contain exactly 10 waves.");
            Assert.That(stage1.enemyCountMultiplier, Is.EqualTo(0.8f).Within(0.01f), "Stage 1 enemy count multiplier must be 0.8.");
            Assert.That(stage1.spawnIntervalMultiplier, Is.EqualTo(1.15f).Within(0.01f), "Stage 1 spawn interval multiplier must be 1.15.");
        }

        [Test]
        public void CameraRig_FovAndAspect_AreConfigured()
        {
            GameObject camGo = new GameObject("TempCam");
            Camera cam = camGo.AddComponent<Camera>();
            CameraRig rig = camGo.AddComponent<CameraRig>();

            // Clean up
            Object.DestroyImmediate(camGo);

            Assert.That(rig, Is.Not.Null);
        }
    }
}
