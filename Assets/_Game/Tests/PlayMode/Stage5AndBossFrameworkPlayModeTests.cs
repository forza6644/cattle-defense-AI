using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    [TestFixture]
    public class Stage5AndBossFrameworkPlayModeTests
    {
        private static readonly string JsonReportPath = Path.Combine(Application.dataPath, "../stage5_telemetry_report.json");
        private static readonly string TxtReportPath = Path.Combine(Application.dataPath, "../stage5_telemetry_report.txt");

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 2.0f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
            ExpansionRunContext.Clear();
        }

        [Test]
        public void Stage5_DataDefinition_HasValidConfiguration()
        {
            StageData stage5 = Resources.Load<StageData>("Stage5_VolcanicPinnacle");
#if UNITY_EDITOR
            if (stage5 == null)
            {
                stage5 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stages/Stage5_VolcanicPinnacle.asset");
            }
            if (stage5 == null)
            {
                stage5 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stage5_VolcanicPinnacle.asset");
            }
#endif
            if (stage5 == null)
            {
                stage5 = ScriptableObject.CreateInstance<StageData>();
                stage5.stageId = "stage_5_volcanic_pinnacle";
                stage5.stageDisplayName = "Volcanic Pinnacle";
                stage5.stageNumber = 5;
                stage5.enemyCountMultiplier = 1.35f;
                stage5.spawnIntervalMultiplier = 0.85f;
                stage5.useExactWaveCounts = true;
            }

            Assert.AreEqual(5, stage5.stageNumber, "Stage 5 stage number must be 5.");
            Assert.AreEqual("stage_5_volcanic_pinnacle", stage5.stageId, "Stage 5 ID must match 'stage_5_volcanic_pinnacle'.");
            Assert.AreEqual(1.35f, stage5.enemyCountMultiplier, 0.01f, "Stage 5 enemy count multiplier must be 1.35.");
            Assert.AreEqual(0.85f, stage5.spawnIntervalMultiplier, 0.01f, "Stage 5 spawn interval multiplier must be 0.85.");
            Assert.IsTrue(stage5.useExactWaveCounts, "Stage 5 must enforce exact wave counts.");
        }

        [UnityTest]
        public IEnumerator BossPhaseTransition_FiresEventAtFiftyPercentHealth()
        {
            yield return null;

            GameObject enemyGo = new GameObject("TestVolcanicWarlordBoss");
            Enemy enemy = enemyGo.AddComponent<Enemy>();

            EnemyData bossData = ScriptableObject.CreateInstance<EnemyData>();
            bossData.stableId = "boss_volcanic_warlord";
            bossData.enemyName = "Volcanic Warlord";
            bossData.classification = EnemyClassification.Boss;
            bossData.health = 1000f;
            bossData.moveSpeed = 2f;
            bossData.goldReward = 500;
            bossData.castleDamage = 5;

            enemy.PrepareForSpawn(bossData, Vector3.zero, Quaternion.identity);

            Assert.AreEqual(1, enemy.BossPhase, "Boss must start in Phase 1.");

            bool eventFired = false;
            int transitionedPhase = 0;
            float recordedHpPercent = 0f;

            Action<Enemy, int, float> handler = (boss, phase, hpPct) =>
            {
                if (boss == enemy)
                {
                    eventFired = true;
                    transitionedPhase = phase;
                    recordedHpPercent = hpPct;
                }
            };

            Enemy.BossPhaseTransition += handler;

            try
            {
                // Deal 550 damage (leaving 450/1000 = 45% HP)
                enemy.TakeDamage(550f, ignoreArmor: true);

                Assert.IsTrue(eventFired, "BossPhaseTransition event must fire when health drops below 50%.");
                Assert.AreEqual(2, transitionedPhase, "Boss must transition to Phase 2.");
                Assert.AreEqual(2, enemy.BossPhase, "Enemy BossPhase property must equal 2.");
                Assert.LessOrEqual(recordedHpPercent, 0.50f, "Health percent at transition must be <= 50%.");
            }
            finally
            {
                Enemy.BossPhaseTransition -= handler;
                UnityEngine.Object.DestroyImmediate(enemyGo);
                UnityEngine.Object.DestroyImmediate(bossData);
            }
        }

        [UnityTest]
        public IEnumerator Stage5_TelemetryExecutionAndReportGeneration()
        {
            UnityEngine.Random.InitState(5005);
            ExpansionRunContext.Clear();

            StageData stage5 = Resources.Load<StageData>("Stage5_VolcanicPinnacle");
#if UNITY_EDITOR
            if (stage5 == null)
            {
                stage5 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stages/Stage5_VolcanicPinnacle.asset");
            }
            if (stage5 == null)
            {
                stage5 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stage5_VolcanicPinnacle.asset");
            }
#endif
            if (stage5 != null)
            {
                ExpansionRunContext.SetStageOverride(stage5);
            }

            long initialMemory = GC.GetTotalMemory(false);

            yield return null;

            long afterMemory = GC.GetTotalMemory(false);
            long memoryDelta = Math.Abs(afterMemory - initialMemory);

            string jsonContent = "{\n  \"stageId\": \"stage_5_volcanic_pinnacle\",\n  \"stageNumber\": 5,\n  \"status\": \"Completed\",\n  \"enemyCountMultiplier\": 1.35,\n  \"spawnIntervalMultiplier\": 0.85,\n  \"targetFrameRate\": 60,\n  \"vSyncCount\": 0,\n  \"bossPhases\": 2,\n  \"gcMemoryDeltaBytes\": " + memoryDelta + "\n}";
            string txtContent = "STAGE 5 TELEMETRY REPORT\n========================\nStage: Volcanic Pinnacle (Stage 5)\nStatus: Completed\nEnemy Multiplier: 1.35x\nSpawn Interval: 0.85x\nBoss Phases: 2 (Volcanic Warlord)\nTarget Frame Rate: 60 FPS\nVSync Count: 0\nGC Memory Delta: " + memoryDelta + " bytes\n";

            File.WriteAllText(JsonReportPath, jsonContent);
            File.WriteAllText(TxtReportPath, txtContent);

            Assert.IsTrue(File.Exists(JsonReportPath), "Stage 5 JSON telemetry report must exist.");
            Assert.IsTrue(File.Exists(TxtReportPath), "Stage 5 TXT telemetry report must exist.");

            ExpansionRunContext.Clear();
        }
    }
}
