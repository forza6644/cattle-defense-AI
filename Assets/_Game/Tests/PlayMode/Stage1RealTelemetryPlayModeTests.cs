using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class Stage1RealTelemetryPlayModeTests
    {
        private static readonly string JsonReportPath = Path.Combine(Application.dataPath, "../stage1_telemetry_report.json");
        private static readonly string TxtReportPath = Path.Combine(Application.dataPath, "../stage1_telemetry_report.txt");

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
        public IEnumerator Stage1_RealTimeCombat_TelemetryExecution()
        {
            UnityEngine.Random.InitState(1001);
            ExpansionRunContext.Clear();
            SaveManager.SetSelectedStage(0); // Stage 1 Castle Road

            var loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameplayIntegration_V2");
            while (!loadOp.isDone) yield return null;

            float waitInitDeadline = Time.realtimeSinceStartup + 25f;
            while ((GameManager.Instance == null || HeroRosterManager.Instance == null || Object.FindFirstObjectByType<WaveManager>() == null
                    || RunProgressionManager.Instance == null || UIManager.Instance == null)
                   && Time.realtimeSinceStartup < waitInitDeadline)
            {
                yield return null;
            }

            GameManager game = GameManager.Instance;
            HeroRosterManager roster = HeroRosterManager.Instance;
            WaveManager waves = Object.FindFirstObjectByType<WaveManager>();
            Castle castle = Object.FindFirstObjectByType<Castle>();
            RunProgressionManager progression = RunProgressionManager.Instance;

            Assert.That(game, Is.Not.Null, "GameManager must exist.");
            Assert.That(roster, Is.Not.Null, "HeroRosterManager must exist.");
            Assert.That(waves, Is.Not.Null, "WaveManager must exist.");
            Assert.That(castle, Is.Not.Null, "Castle must exist.");
            Assert.That(progression, Is.Not.Null, "RunProgressionManager must exist.");

            roster.InitializeRunRoster();
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(0), "Run must start with 0 owned heroes.");

            // Recruit hero roster to defend the castle in Stage 1 real combat
            string[] heroIds = { "archer", "bombardier", "frost_mage", "fire_mage", "electric_engineer", "sniper" };
            foreach (string heroId in heroIds)
            {
                roster.RecruitHero(heroId);
            }

            RealCombatTelemetryLogger logger = new RealCombatTelemetryLogger();
            logger.StartRun();

            int currentWaveTracked = 0;
            int lastKnownAliveCount = 0;

            System.Action<Enemy, int> onEnemyKilled = (enemy, gold) =>
            {
                logger.OnEnemyDefeated(1);
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

            game.SetGameSpeed(2f); // Set to 2x game speed for real-time simulation

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
            }
            finally
            {
                Enemy.AnyKilled -= onEnemyKilled;
                progression.ShowLevelUpDraft -= onShowDraft;
            }

            Assert.That(File.Exists(JsonReportPath), Is.True, "JSON telemetry report file must be generated.");
            Assert.That(File.Exists(TxtReportPath), Is.True, "TXT telemetry report file must be generated.");
            Assert.That(logger.Report.totalWavesCleared, Is.GreaterThanOrEqualTo(1), "Telemetry run must clear waves.");
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "Stage 1 real combat run must reach Victory.");
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

        private static string GetFirstDraftTitle()
        {
            UIManager ui = UIManager.Instance;
            if (ui == null) return "First Choice";
            var field = typeof(UIManager).GetField("cardButtons", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var buttons = field?.GetValue(ui) as UnityEngine.UI.Button[];
            if (buttons != null && buttons.Length > 0 && buttons[0] != null)
            {
                var components = buttons[0].GetComponentsInChildren<Component>();
                foreach (var c in components)
                {
                    if (c == null) continue;
                    var prop = c.GetType().GetProperty("text");
                    if (prop != null)
                    {
                        string val = prop.GetValue(c) as string;
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
            }
            return "First Choice";
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
