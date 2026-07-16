using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Stonehold.Tests
{
    public class BaselinePlayModeTests
    {
        private static readonly string[] IntPreferenceKeys =
        {
            "save_version",
            "stats_best_wave",
            "stats_total_wins",
            "stats_total_losses",
            "stats_total_runs",
            "lobby_selected_stage",
            "stats_highest_stage_unlocked",
            "stats_stage_1_completed",
            "stats_meta_gold",
            "stats_account_xp",
            "stats_core_materials",
            "meta_level_archer",
            "meta_level_bombardier",
            "meta_level_frost_mage",
            "meta_level_fire_mage",
            "meta_level_electric_engineer",
            "meta_level_sniper",
            "meta_upgrade_castle_hp",
            "meta_upgrade_castle_regen",
            "meta_upgrade_damage",
            "meta_upgrade_fire_rate",
            "meta_upgrade_range"
        };

        private readonly Dictionary<string, int> savedIntPreferences = new Dictionary<string, int>();
        private readonly HashSet<string> missingIntPreferences = new HashSet<string>();
        private bool hadStartingDefender;
        private string savedStartingDefender;

        [SetUp]
        public void SetUp()
        {
            savedIntPreferences.Clear();
            missingIntPreferences.Clear();
            for (int i = 0; i < IntPreferenceKeys.Length; i++)
            {
                string key = IntPreferenceKeys[i];
                if (PlayerPrefs.HasKey(key))
                {
                    savedIntPreferences[key] = PlayerPrefs.GetInt(key);
                }
                else
                {
                    missingIntPreferences.Add(key);
                }
            }

            const string defenderKey = "lobby_selected_starting_defender";
            hadStartingDefender = PlayerPrefs.HasKey(defenderKey);
            savedStartingDefender = PlayerPrefs.GetString(defenderKey, "archer");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (VfxManager manager in Object.FindObjectsByType<VfxManager>())
            {
                Object.DestroyImmediate(manager.gameObject);
            }
            foreach (AudioManager manager in Object.FindObjectsByType<AudioManager>())
            {
                Object.DestroyImmediate(manager.gameObject);
            }
            foreach (Castle castle in Object.FindObjectsByType<Castle>())
            {
                Object.DestroyImmediate(castle.gameObject);
            }

            RestorePlayerPreferences();
        }

        [UnityTest]
        public IEnumerator ResultEffect_PlaysAndReturnsWhileTimeScaleIsZero()
        {
            GameObject managerObject = new GameObject("VFX Test Manager");
            VfxManager manager = managerObject.AddComponent<VfxManager>();
            GameObject prefab = CreateParticlePrefab(0.08f, 0.08f);
            MethodInfo play = typeof(VfxManager).GetMethod(
                "Play",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(GameObject), typeof(Vector3), typeof(Color?), typeof(float), typeof(bool) },
                null);
            Assert.That(play, Is.Not.Null);

            Time.timeScale = 0f;
            play.Invoke(manager, new object[] { prefab, Vector3.zero, null, 1f, true });
            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(manager.transform.childCount, Is.EqualTo(1));
            ParticleSystem instance = manager.transform.GetChild(0).GetComponent<ParticleSystem>();
            Assert.That(instance.time, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(instance.gameObject.activeSelf, Is.False);
            Assert.That(instance.particleCount, Is.Zero);

            Object.Destroy(prefab);
            Object.Destroy(managerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioSceneHook_DoesNotDuplicateCastleDamageSubscription()
        {
            GameObject castleObject = new GameObject("Castle Hook Test");
            castleObject.SetActive(false);
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            config.castleMaxHealth = 50;
            Castle castle = castleObject.AddComponent<Castle>();
            SetPrivateField(castle, "config", config);
            castleObject.SetActive(true);

            GameObject audioObject = new GameObject("Audio Hook Test");
            AudioManager audio = audioObject.AddComponent<AudioManager>();
            MethodInfo hook = typeof(AudioManager).GetMethod("HookScene", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(hook, Is.Not.Null);
            hook.Invoke(audio, null);
            hook.Invoke(audio, null);

            FieldInfo damageField = typeof(Castle).GetField("DamageTaken", BindingFlags.Instance | BindingFlags.NonPublic);
            Delegate listeners = damageField?.GetValue(castle) as Delegate;
            Assert.That(listeners, Is.Not.Null);
            Assert.That(listeners.GetInvocationList().Length, Is.EqualTo(1));

            Object.Destroy(audioObject);
            Object.Destroy(castleObject);
            Object.Destroy(config);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VictoryAndDefeatSequences_CompleteAndReturnAtTimeScaleZero()
        {
            GameObject managerObject = new GameObject("Result VFX Test Manager");
            VfxManager manager = managerObject.AddComponent<VfxManager>();
            GameObject prefab = CreateParticlePrefab(0.08f, 0.08f);
            SetPrivateField(manager, "victoryPrefab", prefab);
            SetPrivateField(manager, "defeatPrefab", prefab);
            MethodInfo onStateChanged = typeof(VfxManager).GetMethod(
                "OnStateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onStateChanged, Is.Not.Null);

            Time.timeScale = 0f;
            onStateChanged.Invoke(manager, new object[] { GameState.Victory });
            yield return new WaitForSecondsRealtime(2.8f);
            AssertAllParticlesReturned(manager);

            onStateChanged.Invoke(manager, new object[] { GameState.Defeat });
            yield return new WaitForSecondsRealtime(1.4f);
            AssertAllParticlesReturned(manager);

            Object.Destroy(prefab);
            Object.Destroy(managerObject);
            yield return null;
        }

        [UnityTest, Timeout(420000)]
        public IEnumerator FullTenWaveRun_AllHeroesDraftSpeedResultsAndRestartRemainHealthy()
        {
            UnityEngine.Random.InitState(12012);
            SaveManager.SetSelectedStage(0);
            SaveManager.SetSelectedStartingDefender("archer");
            int startingGold = SaveManager.MetaGold;

            AsyncOperation load = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            float initializationDeadline = Time.realtimeSinceStartup + 15f;
            while ((GameManager.Instance == null
                    || HeroRosterManager.Instance == null
                    || CardDraftManager.Instance == null
                    || UIManager.Instance == null
                    || Object.FindFirstObjectByType<WaveManager>() == null)
                   && Time.realtimeSinceStartup < initializationDeadline)
            {
                yield return null;
            }

            GameManager game = GameManager.Instance;
            HeroRosterManager roster = HeroRosterManager.Instance;
            WaveManager waves = Object.FindFirstObjectByType<WaveManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(roster, Is.Not.Null);
            Assert.That(waves, Is.Not.Null);
            Assert.That(UIManager.Instance, Is.Not.Null);

            yield return null;
            roster.InitializeRunRoster();
            string[] heroIds =
            {
                "archer",
                "bombardier",
                "frost_mage",
                "fire_mage",
                "electric_engineer",
                "sniper"
            };
            for (int i = 1; i < heroIds.Length; i++)
            {
                Assert.That(roster.RecruitHero(heroIds[i]), Is.True, "Failed to recruit " + heroIds[i]);
            }
            Assert.That(roster.OwnedHeroIds.Count, Is.EqualTo(6));

            game.SetGameSpeed(1f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            game.SetGameSpeed(1.5f);
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
            game.SetGameSpeed(2f);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            game.TogglePause();
            Assert.That(game.State, Is.EqualTo(GameState.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            game.TogglePause();
            Assert.That(game.State, Is.EqualTo(GameState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(2f));

            int draftCount = 0;
            int peakEnemies = 0;
            int frames = 0;
            bool handledCurrentDraft = false;
            float runStart = Time.realtimeSinceStartup;
            float runDeadline = runStart + 330f;
            long peakGcAllocatedBytes = 0;
            ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                "GC Allocated In Frame",
                1);

            try
            {
                while (game.State != GameState.Victory && Time.realtimeSinceStartup < runDeadline)
                {
                    if (waves.IsWaitingForWave)
                    {
                        waves.StartNextWaveNow();
                    }

                    if (CardDraftManager.Instance != null && CardDraftManager.Instance.IsDraftActive)
                    {
                        if (!handledCurrentDraft && ClickFirstDraftCard())
                        {
                            draftCount++;
                            handledCurrentDraft = true;
                        }
                    }
                    else
                    {
                        handledCurrentDraft = false;
                    }

                    if (gcRecorder.Valid)
                    {
                        peakGcAllocatedBytes = Math.Max(peakGcAllocatedBytes, gcRecorder.LastValue);
                    }

                    peakEnemies = Mathf.Max(peakEnemies, EnemyManager.AliveCount);
                    frames++;
                    yield return null;
                }
            }
            finally
            {
                gcRecorder.Dispose();
            }

            float elapsed = Time.realtimeSinceStartup - runStart;
            Assert.That(game.State, Is.EqualTo(GameState.Victory), "The ten-wave run did not reach Victory.");
            Assert.That(waves.CurrentWave, Is.EqualTo(10));
            Assert.That(draftCount, Is.GreaterThan(0));
            Assert.That(DamageTracker.Instance, Is.Not.Null);
            for (int i = 0; i < heroIds.Length; i++)
            {
                Assert.That(
                    DamageTracker.Instance.DamageByHeroId.TryGetValue(heroIds[i], out float damage) && damage > 0f,
                    Is.True,
                    heroIds[i] + " did not record damage.");
            }

            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(3f);
            Assert.That(SaveManager.MetaGold, Is.GreaterThan(startingGold));
            Assert.That(SaveManager.TryClaimRunRewards(10, out _, out _, out _), Is.False);

            GameManager oldGame = game;
            oldGame.Restart();
            float restartDeadline = Time.realtimeSinceStartup + 20f;
            while ((GameManager.Instance == null || GameManager.Instance == oldGame)
                   && Time.realtimeSinceStartup < restartDeadline)
            {
                yield return null;
            }

            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance, Is.Not.SameAs(oldGame));
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
            Assert.That(Object.FindObjectsByType<GameManager>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CardDraftManager>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<RunModifierManager>().Length, Is.EqualTo(1));

            float approximateFps = elapsed > 0f ? frames / elapsed : 0f;
            Debug.Log(
                $"[BaselineQualification] waves=10 drafts={draftCount} peakEnemies={peakEnemies} " +
                $"elapsedRealtime={elapsed:0.0}s approximateBatchFps={approximateFps:0.0} " +
                $"peakGcAllocatedBytes={peakGcAllocatedBytes}");
        }

        private static GameObject CreateParticlePrefab(float duration, float lifetime)
        {
            GameObject prefab = new GameObject("Test Particle Prefab");
            ParticleSystem particle = prefab.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particle.main;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.loop = false;
            main.playOnAwake = false;
            return prefab;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing test field: " + fieldName);
            field.SetValue(target, value);
        }

        private static bool ClickFirstDraftCard()
        {
            UIManager ui = UIManager.Instance;
            if (ui == null)
            {
                return false;
            }

            FieldInfo buttonsField = typeof(UIManager).GetField(
                "cardButtons",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Button[] buttons = buttonsField?.GetValue(ui) as Button[];
            if (buttons == null || buttons.Length == 0 || buttons[0] == null)
            {
                return false;
            }

            buttons[0].onClick.Invoke();
            return true;
        }

        private static void AssertAllParticlesReturned(VfxManager manager)
        {
            ParticleSystem[] particles = manager.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(particles.Length, Is.GreaterThan(0));
            for (int i = 0; i < particles.Length; i++)
            {
                Assert.That(particles[i].gameObject.activeSelf, Is.False);
                Assert.That(particles[i].particleCount, Is.Zero);
            }
        }

        private void RestorePlayerPreferences()
        {
            for (int i = 0; i < IntPreferenceKeys.Length; i++)
            {
                string key = IntPreferenceKeys[i];
                if (missingIntPreferences.Contains(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
                else if (savedIntPreferences.TryGetValue(key, out int value))
                {
                    PlayerPrefs.SetInt(key, value);
                }
            }

            const string defenderKey = "lobby_selected_starting_defender";
            if (hadStartingDefender)
            {
                PlayerPrefs.SetString(defenderKey, savedStartingDefender);
            }
            else
            {
                PlayerPrefs.DeleteKey(defenderKey);
            }

            PlayerPrefs.Save();
            SaveManager.LoadProgress();
        }
    }
}
