using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class Stage3RealTelemetryPlayModeTests
    {
        private static readonly string JsonReportPath = Path.Combine(Application.dataPath, "../stage3_telemetry_report.json");
        private static readonly string TxtReportPath = Path.Combine(Application.dataPath, "../stage3_telemetry_report.txt");

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 2.0f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
        }

        [UnityTest, Timeout(420000)]
        public IEnumerator Stage3_RealTimeCombat_TelemetryExecution()
        {
            UnityEngine.Random.InitState(3003);
            ExpansionRunContext.Clear();

            // Set Selected Stage to Stage 3 (Index 2) and set stage override
            SaveManager.SetSelectedStage(2);
            StageData stage3 = Resources.Load<StageData>("Stage3_FrozenFrontier");
#if UNITY_EDITOR
            if (stage3 == null)
            {
                stage3 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stage3_FrozenFrontier.asset");
            }
#endif
            if (stage3 != null)
            {
                ExpansionRunContext.SetStageOverride(stage3);
            }

            var loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameplayIntegration_V2");
            while (!loadOp.isDone) yield return null;

            float waitInitDeadline = Time.realtimeSinceStartup + 25f;
            while ((GameManager.Instance == null || HeroRosterManager.Instance == null || UnityEngine.Object.FindFirstObjectByType<WaveManager>() == null
                    || RunProgressionManager.Instance == null || UIManager.Instance == null)
                   && Time.realtimeSinceStartup < waitInitDeadline)
            {
                yield return null;
            }

            GameManager game = GameManager.Instance;
            HeroRosterManager roster = HeroRosterManager.Instance;
            WaveManager waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            Castle castle = UnityEngine.Object.FindFirstObjectByType<Castle>();
            RunProgressionManager progression = RunProgressionManager.Instance;

            Assert.That(game, Is.Not.Null, "GameManager must exist.");
            Assert.That(roster, Is.Not.Null, "HeroRosterManager must exist.");
            Assert.That(waves, Is.Not.Null, "WaveManager must exist.");
            Assert.That(castle, Is.Not.Null, "Castle must exist.");
            Assert.That(progression, Is.Not.Null, "RunProgressionManager must exist.");

            roster.InitializeRunRoster();

            // Recruit full hero roster for Stage 3 defense
            string[] heroIds = { "archer", "bombardier", "frost_mage", "fire_mage", "electric_engineer", "sniper" };
            foreach (string heroId in heroIds)
            {
                roster.RecruitHero(heroId);
            }

            RealCombatTelemetryLogger logger = new RealCombatTelemetryLogger();
            logger.StartRun();
            logger.Report.reportTitle = "STAGE 3 (FROZEN FRONTIER) REAL-TIME COMBAT TELEMETRY REPORT";
            logger.Report.stageId = "stage_3_frozen_frontier";

            int currentWaveTracked = 0;
            int lastKnownAliveCount = 0;
            bool warlordBossSpawned = false;
            bool warlordBossDefeated = false;

            System.Action<Enemy, int> onEnemyKilled = (enemy, gold) =>
            {
                logger.OnEnemyDefeated(1);
                if (enemy != null && enemy.Data != null && enemy.Data.classification == EnemyClassification.Boss)
                {
                    warlordBossDefeated = true;
                }
            };
            Enemy.AnyKilled += onEnemyKilled;

            string[] currentOfferedTitles = null;
            System.Action<RunProgressionManager.CardChoice[]> onShowDraft = (choices) =>
            {
                if (choices != null)
                {
                    List<string> titles = new List<string>();
                    foreach (var c in choices)
                    {
                        titles.Add($"{c.title} [{c.rarity} {c.cardType}]");
                    }
                    currentOfferedTitles = titles.ToArray();
                }
            };
            progression.ShowLevelUpDraft += onShowDraft;

            game.SetGameSpeed(2.5f); // Fast real-time simulation for high density 161 enemies

            float deadline = Time.realtimeSinceStartup + 360f;
            bool handledCurrentDraft = false;

            try
            {
                while (game.State != GameState.Victory && game.State != GameState.Defeat && Time.realtimeSinceStartup < deadline)
                {
                    if (waves.IsWaitingForWave)
                    {
                        if (currentWaveTracked > 0)
                        {
                            logger.OnWaveClear(currentWaveTracked, castle, progression);
                        }
                        currentWaveTracked = waves.CurrentWave + 1;
                        if (currentWaveTracked <= 10)
                        {
                            logger.OnWaveStart(currentWaveTracked);
                        }
                        waves.StartNextWaveNow();
                        lastKnownAliveCount = 0;
                    }

                    int activeEnemies = EnemyManager.AliveCount;
                    if (activeEnemies > lastKnownAliveCount)
                    {
                        int spawnedNew = activeEnemies - lastKnownAliveCount;
                        logger.OnEnemySpawned(spawnedNew);
                    }
                    lastKnownAliveCount = activeEnemies;

                    // Track Warlord Boss spawn
                    if (currentWaveTracked == 10 && !warlordBossSpawned)
                    {
                        var alive = EnemyManager.All;
                        foreach (var e in alive)
                        {
                            if (e != null && e.Data != null && e.Data.classification == EnemyClassification.Boss)
                            {
                                warlordBossSpawned = true;
                                break;
                            }
                        }
                    }

                    // Process Card Drafts automatically
                    if (CardDraftManager.Instance != null && CardDraftManager.Instance.IsDraftActive)
                    {
                        if (!handledCurrentDraft)
                        {
                            string[] offeredTitles = GetOfferedDraftTitles();
                            if (offeredTitles == null || offeredTitles.Length == 0)
                            {
                                offeredTitles = currentOfferedTitles;
                            }
                            string selectedTitle = (offeredTitles != null && offeredTitles.Length > 0)
                                ? offeredTitles[0]
                                : "First Choice";
                            logger.OnDraftTriggered(progression.CurrentLevel, offeredTitles, selectedTitle);
                            ClickFirstDraftCard();
                            handledCurrentDraft = true;
                        }
                    }
                    else
                    {
                        handledCurrentDraft = false;
                    }

                    yield return null;
                }

                if (currentWaveTracked > 0 && logger.Report.waveLogs.Find(w => w.waveNumber == currentWaveTracked) == null)
                {
                    logger.OnWaveClear(currentWaveTracked, castle, progression);
                }

                logger.CompleteRun(game.State, castle, progression);

                // Write Stage 3 report files
                SaveStage3Report(logger.Report, warlordBossSpawned, warlordBossDefeated, JsonReportPath, TxtReportPath);
            }
            finally
            {
                Enemy.AnyKilled -= onEnemyKilled;
                progression.ShowLevelUpDraft -= onShowDraft;
                ExpansionRunContext.Clear();
            }

            Assert.That(File.Exists(JsonReportPath), Is.True, "Stage 3 JSON telemetry report must exist.");
            Assert.That(File.Exists(TxtReportPath), Is.True, "Stage 3 TXT telemetry report must exist.");
            Assert.That(logger.Report.totalWavesCleared, Is.EqualTo(10), "Stage 3 telemetry run must clear all 10 waves.");
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "Stage 3 real combat run must reach Victory.");
        }

        private static void SaveStage3Report(Stage1TelemetryReportData report, bool bossSpawned, bool bossDefeated, string jsonPath, string txtPath)
        {
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(jsonPath, json);

            using (StreamWriter writer = new StreamWriter(txtPath, false))
            {
                writer.WriteLine("================================================================================");
                writer.WriteLine($" {report.reportTitle}");
                writer.WriteLine("================================================================================");
                writer.WriteLine($"Timestamp:                  {report.generatedAtTimestamp}");
                writer.WriteLine($"Stage ID:                   {report.stageId}");
                writer.WriteLine($"Final Game State:           {report.finalGameState}");
                writer.WriteLine($"Total Run Duration:         {report.totalRunDurationSeconds:F2} seconds");
                writer.WriteLine($"Total Waves Cleared:        {report.totalWavesCleared} / 10");
                writer.WriteLine($"Total Enemies Spawned:      {report.totalEnemiesSpawned}");
                writer.WriteLine($"Total Enemies Defeated:     {report.totalEnemiesDefeated}");
                writer.WriteLine($"Final Castle HP:            {report.finalCastleHp} / {report.finalCastleMaxHp}");
                writer.WriteLine($"Final Player Level:         {report.finalPlayerLevel}");
                writer.WriteLine($"Final Player XP:            {report.finalPlayerXp}");
                writer.WriteLine($"Total Draft Triggers:       {report.totalDraftTriggersFired}");
                writer.WriteLine($"Warlord Boss Spawned:       {(bossSpawned ? "YES" : "NO")}");
                writer.WriteLine($"Warlord Boss Defeated:      {(bossDefeated ? "YES" : "NO")}");
                writer.WriteLine("================================================================================");
                writer.WriteLine(" WAVE BREAKDOWN:");
                writer.WriteLine("--------------------------------------------------------------------------------");
                writer.WriteLine(" Wave | Start (s) | End (s) | Duration (s) | Spawned | Defeated | Castle HP | XP   | Level");
                writer.WriteLine("--------------------------------------------------------------------------------");
                foreach (var w in report.waveLogs)
                {
                    writer.WriteLine($" {w.waveNumber,4} | {w.realStartTimeSeconds,9:F1} | {w.realEndTimeSeconds,7:F1} | {w.durationSeconds,12:F1} | {w.enemiesSpawned,7} | {w.enemiesDefeated,8} | {w.castleHpRemaining,4}/{w.castleMaxHp,-4} | {w.playerXpAtWaveEnd,4} | {w.playerLevelAtWaveEnd,5}");
                }
                writer.WriteLine("================================================================================");
                writer.WriteLine(" DRAFT SELECTION LOG:");
                writer.WriteLine("--------------------------------------------------------------------------------");
                foreach (var d in report.draftLogs)
                {
                    writer.WriteLine($" Draft #{d.draftIndex} @ {d.timestampSeconds:F1}s (Level {d.playerLevel})");
                    writer.WriteLine($"   Offered Choices:  {string.Join(" | ", d.offeredCardTitles)}");
                    writer.WriteLine($"   Selected Card:    {d.selectedCardTitle}");
                    writer.WriteLine("--------------------------------------------------------------------------------");
                }
            }
        }

        private static string[] GetOfferedDraftTitles()
        {
            UIManager ui = UIManager.Instance;
            if (ui == null) return new string[0];
            var field = typeof(UIManager).GetField("cardTitleTexts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var textsObj = field?.GetValue(ui) as System.Array;
            if (textsObj != null)
            {
                List<string> res = new List<string>();
                foreach (var t in textsObj)
                {
                    if (t == null) continue;
                    var prop = t.GetType().GetProperty("text");
                    string val = prop?.GetValue(t) as string;
                    if (!string.IsNullOrEmpty(val)) res.Add(val);
                }
                return res.ToArray();
            }
            return new string[0];
        }

        private static bool ClickFirstDraftCard()
        {
            UIManager ui = UIManager.Instance;
            if (ui == null) return false;
            var field = typeof(UIManager).GetField("cardButtons", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var buttons = field?.GetValue(ui) as UnityEngine.UI.Button[];
            if (buttons == null || buttons.Length == 0 || buttons[0] == null) return false;
            buttons[0].onClick.Invoke();
            return true;
        }
    }
}
