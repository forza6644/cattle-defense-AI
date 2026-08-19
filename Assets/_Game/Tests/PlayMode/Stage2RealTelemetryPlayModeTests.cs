using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class Stage2RealTelemetryPlayModeTests
    {
        private static readonly string JsonReportPath = Path.Combine(Application.dataPath, "../stage2_telemetry_report.json");
        private static readonly string TxtReportPath = Path.Combine(Application.dataPath, "../stage2_telemetry_report.txt");

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
        public IEnumerator Stage2_RealTimeCombat_TelemetryExecution()
        {
            UnityEngine.Random.InitState(2002);
            ExpansionRunContext.Clear();

            // Set Selected Stage to Stage 2 (Index 1) and set stage override
            SaveManager.SetSelectedStage(1);
            StageData stage2 = null;
#if UNITY_EDITOR
            stage2 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stages/Stage2_HighlandsFortress.asset");
#endif
            if (stage2 != null)
            {
                ExpansionRunContext.SetStageOverride(stage2);
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
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0), "Run must start with 0 owned heroes.");

            // Authentic fresh campaign starting roster: 1 starting defender (Archer)
            roster.RecruitHero("archer");

            RealCombatTelemetryLogger logger = new RealCombatTelemetryLogger();
            logger.StartRun();
            logger.Report.reportTitle = "STAGE 2 (HIGHLANDS FORTRESS) REAL-TIME COMBAT TELEMETRY REPORT";
            logger.Report.stageId = "stage_2_highlands";

            int currentWaveTracked = 0;
            int lastKnownAliveCount = 0;

            float closestEnemyZ = 999f;
            int enemiesInCastleApproach75 = 0;
            int enemiesInCastleGate95 = 0;
            int enemiesReachedCastle = 0;
            float bossSpawnTime = -1f;
            float bossDefeatTime = -1f;
            bool bossPhase2Triggered = false;

            System.Action<Enemy, int, float> onBossPhase = (boss, phase, hpPercent) =>
            {
                if (phase == 2) bossPhase2Triggered = true;
            };
            Enemy.BossPhaseTransition += onBossPhase;

            System.Action<Enemy, int> onEnemyKilled = (enemy, gold) =>
            {
                logger.OnEnemyDefeated(1);
                if (enemy != null && enemy.Data != null && enemy.Data.classification == EnemyClassification.Boss)
                {
                    bossDefeatTime = Time.realtimeSinceStartup;
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

            game.SetGameSpeed(2.0f); // 2x game speed for real-time simulation

            float deadline = Time.realtimeSinceStartup + 330f;
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

                    // Track active enemy positions and castle proximity
                    var allAlive = EnemyManager.All;
                    for (int i = 0; i < allAlive.Count; i++)
                    {
                        Enemy e = allAlive[i];
                        if (e == null || !e.IsActiveActivation || e.IsDead) continue;
                        float z = e.transform.position.z;
                        if (z < closestEnemyZ) closestEnemyZ = z;

                        if (e.Data != null && e.Data.classification == EnemyClassification.Boss && bossSpawnTime < 0f)
                        {
                            bossSpawnTime = Time.realtimeSinceStartup;
                        }

                        float depthPercent = Mathf.Clamp01((44.0f - z) / (44.0f - 0.4f));
                        if (depthPercent >= 0.75f) enemiesInCastleApproach75++;
                        if (depthPercent >= 0.95f || z <= 2.5f) enemiesInCastleGate95++;
                        if (e.IsAttackingCastle || z <= 2.2f) enemiesReachedCastle++;
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
                logger.SaveReport(JsonReportPath, TxtReportPath);

                float bossDurationReal = (bossDefeatTime > 0 && bossSpawnTime > 0) ? (bossDefeatTime - bossSpawnTime) : 0f;
                float bossDurationInGame = bossDurationReal * 2f;

                TestContext.WriteLine("================================================================================");
                TestContext.WriteLine("STAGE 2 (HIGHLANDS FORTRESS) AUTHENTIC FRESH-CAMPAIGN COMBAT TELEMETRY");
                TestContext.WriteLine("================================================================================");
                TestContext.WriteLine($"Total Run Duration (2x speed): {logger.Report.totalRunDurationSeconds:F2}s ({logger.Report.totalRunDurationSeconds * 2f:F2}s in-game)");
                TestContext.WriteLine($"Final Castle HP:               {castle.CurrentHealth} / {castle.MaxHealth}");
                TestContext.WriteLine($"Heroes Recruited:              {roster.OwnedHeroIds.Count} ({string.Join(", ", roster.OwnedHeroIds)})");
                TestContext.WriteLine($"Closest Enemy Z:               {closestEnemyZ:F2}m");
                TestContext.WriteLine($"Boss Duration (In-Game 1x):    {bossDurationInGame:F2}s (Real: {bossDurationReal:F2}s)");
                TestContext.WriteLine($"Boss Phase 2 Triggered:        {bossPhase2Triggered}");
                TestContext.WriteLine("================================================================================");
            }
            finally
            {
                Enemy.AnyKilled -= onEnemyKilled;
                Enemy.BossPhaseTransition -= onBossPhase;
                progression.ShowLevelUpDraft -= onShowDraft;
                ExpansionRunContext.Clear();
            }

            Assert.That(File.Exists(JsonReportPath), Is.True, "Stage 2 JSON telemetry report must exist.");
            Assert.That(File.Exists(TxtReportPath), Is.True, "Stage 2 TXT telemetry report must exist.");
            Assert.That(logger.Report.totalWavesCleared, Is.GreaterThanOrEqualTo(1), "Stage 2 telemetry run must clear waves.");
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "Stage 2 real combat run must reach Victory.");
        }

        private static void SaveStage2Report(Stage1TelemetryReportData report, int raiderAttacks, int shamanHeals, string jsonPath, string txtPath)
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
                writer.WriteLine($"Crossbow Raider Attacks:    {raiderAttacks}");
                writer.WriteLine($"War Shaman Heal Pulses:     {shamanHeals}");
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

            var titles = GetOfferedDraftTitles();
            int clickIndex = 0;
            if (HeroRosterManager.Instance != null && HeroRosterManager.Instance.OwnedHeroIds.Count < 4 && titles != null)
            {
                for (int i = 0; i < titles.Length && i < buttons.Length; i++)
                {
                    if (titles[i] != null && titles[i].StartsWith("Add ") && buttons[i] != null)
                    {
                        clickIndex = i;
                        break;
                    }
                }
            }

            buttons[clickIndex].onClick.Invoke();
            return true;
        }
    }
}
