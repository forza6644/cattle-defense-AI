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
            StageData stage3 = null;
#if UNITY_EDITOR
            stage3 = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Game/ScriptableObjects/Stages/Stage3_FrozenFrontier.asset");
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
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0), "Run must start with 0 owned heroes.");

            // Authentic fresh campaign starting roster: 1 starting defender (Archer)
            roster.RecruitHero("archer");

            RealCombatTelemetryLogger logger = new RealCombatTelemetryLogger();
            logger.StartRun();
            logger.Report.reportTitle = "STAGE 3 (FROZEN FRONTIER) REAL-TIME COMBAT TELEMETRY REPORT";
            logger.Report.stageId = "stage_3_frozen_frontier";

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
                TestContext.WriteLine("STAGE 3 (FROZEN FRONTIER) AUTHENTIC FRESH-CAMPAIGN COMBAT TELEMETRY");
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

            Assert.That(File.Exists(JsonReportPath), Is.True, "Stage 3 JSON telemetry report must exist.");
            Assert.That(File.Exists(TxtReportPath), Is.True, "Stage 3 TXT telemetry report must exist.");
            Assert.That(logger.Report.totalWavesCleared, Is.EqualTo(10), "Stage 3 telemetry run must clear all 10 waves.");
            Assert.That(logger.Report.damageBreakdown, Is.Not.Null, "Damage breakdown list must not be null.");
            Assert.That(logger.Report.damageBreakdown.Count, Is.GreaterThan(0), "Damage breakdown must contain recorded damage sources.");
            Assert.That(logger.Report.damageBreakdown.Exists(d => d.sourceId == "archer" || d.sourceId.StartsWith("crystal")), Is.True,
                "Damage breakdown must contain the starting defender or active crystal.");
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "Stage 3 real combat run must reach Victory.");
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
