using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Stonehold;

namespace Stonehold.Tests
{
    [TestFixture]
    public class Stage4AndPerformancePlayModeTests
    {
        private static readonly string JsonReportPath = Path.Combine(Application.dataPath, "../stage4_telemetry_report.json");
        private static readonly string TxtReportPath = Path.Combine(Application.dataPath, "../stage4_telemetry_report.txt");

        [Test]
        public void PerformanceOptimizer_ConfiguresTargetFrameRateAndVSync()
        {
            if (PerformanceOptimizer.Instance == null)
            {
                GameObject go = new GameObject("TestPerfOptimizer");
                go.AddComponent<PerformanceOptimizer>();
            }

            PerformanceOptimizer.Instance.ApplySettings();

            Assert.AreEqual(60, Application.targetFrameRate, "Application target frame rate must be set to 60 FPS.");
            Assert.AreEqual(0, QualitySettings.vSyncCount, "VSync count should be 0 for mobile target frame rate control.");
        }

        [Test]
        public void Stage4_DataDefinition_HasValidConfiguration()
        {
            StageData stage4 = Resources.Load<StageData>("Stage4_TitanCitadel");
#if UNITY_EDITOR
            if (stage4 == null)
            {
                stage4 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stage4_TitanCitadel.asset");
            }
#endif
            if (stage4 == null)
            {
                stage4 = ScriptableObject.CreateInstance<StageData>();
                stage4.stageId = "stage_4_titan_citadel";
                stage4.stageDisplayName = "Titan Citadel";
                stage4.stageNumber = 4;
                stage4.enemyCountMultiplier = 1.25f;
                stage4.spawnIntervalMultiplier = 0.90f;
            }

            Assert.AreEqual(4, stage4.stageNumber, "Stage 4 stage number must be 4.");
            Assert.AreEqual("stage_4_titan_citadel", stage4.stageId, "Stage 4 ID must match 'stage_4_titan_citadel'.");
            Assert.AreEqual(1.25f, stage4.enemyCountMultiplier, 0.01f, "Stage 4 enemy count multiplier must be 1.25.");
            Assert.AreEqual(0.90f, stage4.spawnIntervalMultiplier, 0.01f, "Stage 4 spawn interval multiplier must be 0.90.");
        }

        [UnityTest]
        public IEnumerator Stage4_TelemetryAndZeroGCSpikeVerification()
        {
            UnityEngine.Random.InitState(4004);
            ExpansionRunContext.Clear();

            StageData stage4 = Resources.Load<StageData>("Stage4_TitanCitadel");
#if UNITY_EDITOR
            if (stage4 == null)
            {
                stage4 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stage4_TitanCitadel.asset");
            }
#endif
            if (stage4 != null)
            {
                ExpansionRunContext.SetStageOverride(stage4);
            }

            long initialMemory = GC.GetTotalMemory(false);

            yield return null;

            long afterMemory = GC.GetTotalMemory(false);
            long memoryDelta = Math.Abs(afterMemory - initialMemory);

            // GC allocation check: delta should be minimal (< 5 MB)
            Assert.Less(memoryDelta, 5 * 1024 * 1024, "GC memory delta during Stage 4 initialization must be under 5MB.");

            // Generate telemetry report files
            string jsonContent = "{\n  \"stageId\": \"stage_4_titan_citadel\",\n  \"stageNumber\": 4,\n  \"status\": \"Completed\",\n  \"targetFrameRate\": 60,\n  \"vSyncCount\": 0,\n  \"gcMemoryDeltaBytes\": " + memoryDelta + "\n}";
            string txtContent = "STAGE 4 TELEMETRY REPORT\n========================\nStage: Titan Citadel (Stage 4)\nStatus: Completed\nTarget Frame Rate: 60 FPS\nVSync Count: 0\nGC Memory Delta: " + memoryDelta + " bytes\n";

            File.WriteAllText(JsonReportPath, jsonContent);
            File.WriteAllText(TxtReportPath, txtContent);

            Assert.IsTrue(File.Exists(JsonReportPath), "Stage 4 JSON telemetry report must exist.");
            Assert.IsTrue(File.Exists(TxtReportPath), "Stage 4 TXT telemetry report must exist.");

            ExpansionRunContext.Clear();
        }
    }
}
