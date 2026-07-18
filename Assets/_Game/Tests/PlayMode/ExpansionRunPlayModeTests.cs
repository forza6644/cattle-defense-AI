using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class ExpansionRunPlayModeTests
    {
        private const string Root = "Assets/_Game/ScriptableObjects/ExpansionRunQualification";
        private static readonly string[] ProgressKeys =
        {
            "save_version", "stats_best_wave", "stats_total_wins", "stats_total_losses", "stats_total_runs",
            "lobby_selected_stage", "stats_highest_stage_unlocked", "stats_stage_1_completed",
            "stats_meta_gold", "stats_account_xp", "stats_core_materials",
            "meta_level_archer", "meta_level_bombardier", "meta_level_frost_mage", "meta_level_fire_mage",
            "meta_level_electric_engineer", "meta_level_sniper", "meta_upgrade_castle_hp", "meta_upgrade_castle_regen",
            "meta_upgrade_damage", "meta_upgrade_fire_rate", "meta_upgrade_range"
        };
        private readonly Dictionary<string, int> savedProgress = new Dictionary<string, int>();
        private readonly HashSet<string> missingProgress = new HashSet<string>();
        private bool hadStartingDefender;
        private string savedStartingDefender;
        private readonly List<Object> owned = new List<Object>();
        private StageData stage;
        private BattlefieldAnchorManager anchors;
        private TrapRuntimeManager traps;
        private BattlefieldDefenseManager defenses;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CapturePlayerProgress();
            stage = AssetDatabase.LoadAssetAtPath<StageData>(Root + "/StoneholdExpansionTrial.asset");
            Assert.That(stage, Is.Not.Null);
            ExpansionRunContext.Clear();
            traps = TrapRuntimeManager.Instance ?? Own(new GameObject("T13G Trap Manager")).AddComponent<TrapRuntimeManager>();
            defenses = BattlefieldDefenseManager.Instance ?? Own(new GameObject("T13G Defense Manager")).AddComponent<BattlefieldDefenseManager>();
            anchors = BattlefieldAnchorManager.Instance ?? Own(new GameObject("T13G Anchor Manager")).AddComponent<BattlefieldAnchorManager>();
            traps.ResetForRun();
            defenses.ResetForRun();
            anchors.ResetForRun();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            ExpansionRunContext.Clear();
            traps?.ResetForRun();
            defenses?.ResetForRun();
            anchors?.ResetForRun();
            foreach (TrapRuntimeZone item in Object.FindObjectsByType<TrapRuntimeZone>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(item.gameObject);
            foreach (BattlefieldDefenseRuntime item in Object.FindObjectsByType<BattlefieldDefenseRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(item.gameObject);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            RestorePlayerProgress();
            yield return null;
        }

        [Test] public void ExpansionContext_SetAndClearAreDeterministic()
        {
            ExpansionRunContext.SetStageOverride(stage);
            Assert.That(ExpansionRunContext.StageOverride, Is.SameAs(stage));
            ExpansionRunContext.Clear();
            Assert.That(ExpansionRunContext.StageOverride, Is.Null);
        }

        [Test] public void Fixture_RegistersTwoTrapAndOneDefenseAnchor()
        {
            InstantiateFixture();
            Assert.That(anchors.Anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Trap), Is.EqualTo(2));
            Assert.That(anchors.Anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Defense), Is.EqualTo(1));
        }

        [Test] public void Fixture_MakesBothDeploymentTypesAvailable()
        {
            InstantiateFixture();
            Assert.That(anchors.HasAvailableAnchor(BattlefieldAnchorType.Trap), Is.True);
            Assert.That(anchors.HasAvailableAnchor(BattlefieldAnchorType.Defense), Is.True);
        }

        [Test] public void ExpansionBattlefieldCards_DeploySuccessfully()
        {
            InstantiateFixture();
            Assert.That(traps.TryDeploy(Caltrops, out TrapRuntimeZone first), Is.True);
            Assert.That(traps.TryDeploy(Oil, out TrapRuntimeZone second), Is.True);
            Assert.That(defenses.TryDeploy(Barricade, out BattlefieldDefenseRuntime wall), Is.True);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
        }

        [Test] public void Reset_ClearsExpansionBattlefieldState()
        {
            InstantiateFixture();
            traps.TryDeploy(Caltrops, out _);
            traps.TryDeploy(Oil, out _);
            defenses.TryDeploy(Barricade, out _);
            traps.ResetForRun();
            defenses.ResetForRun();
            anchors.ResetForRun();
            Assert.That(traps.ActiveTraps, Is.Empty);
            Assert.That(defenses.ActiveDefense, Is.Null);
            Assert.That(anchors.OccupiedCount, Is.Zero);
            Assert.That(traps.StaleTickCount, Is.Zero);
            Assert.That(defenses.StaleTargetCount, Is.Zero);
        }

        [Test] public void DraftEligibility_FollowsLiveAnchorAvailability()
        {
            CardDefinition trapCard = stage.cardPoolOverride.cards.Single(x => x.card.id == "deploy_caltrops").card;
            CardDefinition defenseCard = stage.cardPoolOverride.cards.Single(x => x.card.id == "deploy_wooden_barricade").card;
            DraftSelectionState unavailable = State(false, false);
            Assert.That(CardDraftSelector.IsEligible(trapCard, unavailable), Is.False);
            Assert.That(CardDraftSelector.IsEligible(defenseCard, unavailable), Is.False);
            DraftSelectionState available = State(true, true);
            Assert.That(CardDraftSelector.IsEligible(trapCard, available), Is.True);
            Assert.That(CardDraftSelector.IsEligible(defenseCard, available), Is.True);
        }

        [UnityTest] public IEnumerator DuplicateManagers_DoNotReplaceExpansionSingletons()
        {
            BattlefieldAnchorManager originalAnchors = anchors;
            TrapRuntimeManager originalTraps = traps;
            BattlefieldDefenseManager originalDefenses = defenses;
            GameObject duplicate = Own(new GameObject("T13G Duplicate Managers"));
            duplicate.AddComponent<BattlefieldAnchorManager>();
            duplicate.AddComponent<TrapRuntimeManager>();
            duplicate.AddComponent<BattlefieldDefenseManager>();
            yield return null;
            Assert.That(BattlefieldAnchorManager.Instance, Is.SameAs(originalAnchors));
            Assert.That(TrapRuntimeManager.Instance, Is.SameAs(originalTraps));
            Assert.That(BattlefieldDefenseManager.Instance, Is.SameAs(originalDefenses));
        }

        [UnityTest] public IEnumerator WaveNine_ProfileAtOneX()
        {
            yield return ProfileWaveNine(1f);
        }

        [UnityTest] public IEnumerator WaveNine_ProfileAtTwoX()
        {
            yield return ProfileWaveNine(2f);
        }

        [UnityTest] public IEnumerator RepeatedFixtureRestart_HasNoStaleOccupancy()
        {
            for (int cycle = 0; cycle < 20; cycle++)
            {
                GameObject fixture = InstantiateFixture();
                Assert.That(traps.TryDeploy(Caltrops, out _), Is.True);
                Assert.That(defenses.TryDeploy(Barricade, out _), Is.True);
                traps.ResetForRun();
                defenses.ResetForRun();
                anchors.ResetForRun();
                Object.DestroyImmediate(fixture);
                Assert.That(anchors.OccupiedCount, Is.Zero);
                yield return null;
            }
            Assert.That(traps.StaleTickCount, Is.Zero);
            Assert.That(defenses.StaleTargetCount, Is.Zero);
        }

        [UnityTest, Timeout(420000)]
        public IEnumerator FullExpansionRun_ReachesVictoryAndRestartsCleanly()
        {
            UnityEngine.Random.InitState(13007);
            ExpansionRunContext.SetStageOverride(stage);
            SaveManager.SetSelectedStartingDefender("archer");

            AsyncOperation load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            while (!load.isDone) yield return null;

            float initializationDeadline = Time.realtimeSinceStartup + 20f;
            while ((GameManager.Instance == null || HeroRosterManager.Instance == null || CardDraftManager.Instance == null
                    || UIManager.Instance == null || Object.FindFirstObjectByType<WaveManager>() == null)
                   && Time.realtimeSinceStartup < initializationDeadline) yield return null;

            GameManager game = GameManager.Instance;
            HeroRosterManager roster = HeroRosterManager.Instance;
            WaveManager waves = Object.FindFirstObjectByType<WaveManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(roster, Is.Not.Null);
            Assert.That(waves, Is.Not.Null);
            Assert.That(waves.ActiveStage, Is.SameAs(stage));

            roster.InitializeRunRoster();
            Assert.That(roster.RecruitHero("bombardier"), Is.True);
            Assert.That(roster.RecruitHero("frost_mage"), Is.True);
            Assert.That(roster.RecruitHero("electric_engineer"), Is.True);
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(4));

            yield return null;
            Assert.That(BattlefieldAnchorManager.Instance, Is.Not.Null);
            Assert.That(BattlefieldAnchorManager.Instance.Anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Trap), Is.EqualTo(2));
            Assert.That(BattlefieldAnchorManager.Instance.Anchors.Count(x => x.AnchorType == BattlefieldAnchorType.Defense), Is.EqualTo(1));
            Assert.That(TrapRuntimeManager.Instance.TryDeploy(Caltrops, out _), Is.True);
            Assert.That(TrapRuntimeManager.Instance.TryDeploy(Oil, out _), Is.True);
            Assert.That(BattlefieldDefenseManager.Instance.TryDeploy(Barricade, out _), Is.True);

            game.SetGameSpeed(2f);
            int drafts = 0;
            bool handledDraft = false;
            int peakEnemies = 0;
            float started = Time.realtimeSinceStartup;
            float deadline = started + 330f;
            while (game.State != GameState.Victory && game.State != GameState.Defeat && Time.realtimeSinceStartup < deadline)
            {
                if (waves.IsWaitingForWave) waves.StartNextWaveNow();
                if (CardDraftManager.Instance != null && CardDraftManager.Instance.IsDraftActive)
                {
                    if (!handledDraft && ClickFirstDraftCard()) { drafts++; handledDraft = true; }
                }
                else handledDraft = false;
                peakEnemies = Mathf.Max(peakEnemies, EnemyManager.AliveCount);
                yield return null;
            }

            float elapsed = Time.realtimeSinceStartup - started;
            TestContext.WriteLine($"ControlledRun state={game.State} wave={waves.CurrentWave} drafts={drafts} peakEnemies={peakEnemies} elapsedRealtime={elapsed:F1}s");
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "Expansion run did not reach Victory.");
            Assert.That(waves.CurrentWave, Is.EqualTo(10));
            Assert.That(drafts, Is.GreaterThan(0));
            Assert.That(peakEnemies, Is.GreaterThanOrEqualTo(20));

            string[] poolKeys = { "grunt", "runner", "brute", "armored", "crossbow_raider", "elite_war_shaman", "warlord_boss" };
            foreach (string key in poolKeys)
            {
                Assert.That(EnemyPoolManager.Instance.TryGetDiagnostics(key, out EnemyPoolManager.PoolDiagnostics diagnostics), Is.True);
                Assert.That(diagnostics.InvalidReturns, Is.Zero, key + " had invalid pool returns.");
                Assert.That(diagnostics.Active, Is.Zero, key + " remained active at Victory.");
            }

            GameManager oldGame = game;
            oldGame.Restart();
            float restartDeadline = Time.realtimeSinceStartup + 25f;
            while ((GameManager.Instance == null || GameManager.Instance == oldGame) && Time.realtimeSinceStartup < restartDeadline) yield return null;
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance, Is.Not.SameAs(oldGame));
            Assert.That(Object.FindObjectsByType<GameManager>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CardDraftManager>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<RunModifierManager>().Length, Is.EqualTo(1));
            Assert.That(TrapRuntimeManager.Instance.ActiveTraps, Is.Empty);
            Assert.That(BattlefieldDefenseManager.Instance.ActiveDefense, Is.Null);
        }

        private static bool ClickFirstDraftCard()
        {
            UIManager ui = UIManager.Instance;
            FieldInfo field = typeof(UIManager).GetField("cardButtons", BindingFlags.Instance | BindingFlags.NonPublic);
            UnityEngine.UI.Button[] buttons = field?.GetValue(ui) as UnityEngine.UI.Button[];
            if (buttons == null || buttons.Length == 0 || buttons[0] == null) return false;
            buttons[0].onClick.Invoke();
            return true;
        }

        private void CapturePlayerProgress()
        {
            savedProgress.Clear();
            missingProgress.Clear();
            foreach (string key in ProgressKeys)
            {
                if (PlayerPrefs.HasKey(key)) savedProgress[key] = PlayerPrefs.GetInt(key);
                else missingProgress.Add(key);
            }
            const string defenderKey = "lobby_selected_starting_defender";
            hadStartingDefender = PlayerPrefs.HasKey(defenderKey);
            savedStartingDefender = PlayerPrefs.GetString(defenderKey, "archer");
        }

        private void RestorePlayerProgress()
        {
            foreach (string key in ProgressKeys)
            {
                if (missingProgress.Contains(key)) PlayerPrefs.DeleteKey(key);
                else if (savedProgress.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value);
            }
            const string defenderKey = "lobby_selected_starting_defender";
            if (hadStartingDefender) PlayerPrefs.SetString(defenderKey, savedStartingDefender);
            else PlayerPrefs.DeleteKey(defenderKey);
            PlayerPrefs.Save();
            SaveManager.LoadProgress();
        }

        private IEnumerator ProfileWaveNine(float speed)
        {
            WaveData wave = stage.waves[8];
            var enemies = new List<GameObject>();
            FieldInfo dataField = typeof(Enemy).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dataField, Is.Not.Null);
            Time.timeScale = speed;
            using (ProfilerRecorder gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                foreach (WaveData.SpawnEntry entry in wave.spawns)
                {
                    for (int i = 0; i < entry.count; i++)
                    {
                        GameObject go = Own(new GameObject($"T13G {entry.enemy.stableId} {i}"));
                        go.SetActive(false);
                        Enemy enemy = go.AddComponent<Enemy>();
                        dataField.SetValue(enemy, entry.enemy);
                        go.transform.position = new Vector3((i % 10) - 5f, 0f, 10f + i / 10f);
                        go.SetActive(true);
                        enemies.Add(go);
                    }
                }

                long peakGc = 0;
                for (int frame = 0; frame < 12; frame++)
                {
                    yield return null;
                    if (gc.Valid) peakGc = System.Math.Max(peakGc, gc.LastValue);
                }
                TestContext.WriteLine($"Wave9 speed={speed:0.0}x enemies={enemies.Count} alive={EnemyManager.AliveCount} peakGcAllocatedBytes={peakGc}");
                Assert.That(enemies, Has.Count.EqualTo(70));
                Assert.That(EnemyManager.AliveCount, Is.GreaterThanOrEqualTo(70));
                Assert.That(peakGc, Is.GreaterThanOrEqualTo(0));
            }

            foreach (GameObject enemy in enemies) if (enemy != null) Object.DestroyImmediate(enemy);
            yield return null;
            Assert.That(EnemyManager.AliveCount, Is.Zero);
        }

        private GameObject InstantiateFixture()
        {
            GameObject fixture = Own(Object.Instantiate(stage.battlefieldFixturePrefab));
            return fixture;
        }

        private DraftSelectionState State(bool trap, bool defense) =>
            new DraftSelectionState(new[] { "archer" }, new[] { AttackType.SingleTarget }, 3, new Dictionary<string, int>(), trap, defense);

        private TrapDefinition Caltrops => AssetDatabase.LoadAssetAtPath<TrapDefinition>("Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification/Caltrops.asset");
        private TrapDefinition Oil => AssetDatabase.LoadAssetAtPath<TrapDefinition>("Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification/BurningOil.asset");
        private BattlefieldDefenseDefinition Barricade => AssetDatabase.LoadAssetAtPath<BattlefieldDefenseDefinition>("Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification/WoodenBarricade.asset");
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    }
}