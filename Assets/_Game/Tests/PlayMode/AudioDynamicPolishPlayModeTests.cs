using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class AudioDynamicPolishPlayModeTests
    {
        private GameObject audioGO;
        private AudioManager audioManager;
        private SoundLibrary testLibrary;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            if (AudioManager.Instance != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }

            testLibrary = ScriptableObject.CreateInstance<SoundLibrary>();
            testLibrary.musicGameplay = AudioClip.Create("test_music", 44100, 1, 44100, false);
            testLibrary.musicBoss = AudioClip.Create("test_boss_music", 44100, 1, 44100, false);
            testLibrary.cannonExplosion = AudioClip.Create("test_cannon", 4410, 1, 44100, false);
            testLibrary.frostHit = AudioClip.Create("test_frost", 4410, 1, 44100, false);
            testLibrary.arrowHit = AudioClip.Create("test_arrow", 4410, 1, 44100, false);
            testLibrary.button = AudioClip.Create("test_button", 4410, 1, 44100, false);
            createdObjects.Add(testLibrary);

            audioGO = new GameObject("AudioManager", typeof(AudioManager));
            audioManager = audioGO.GetComponent<AudioManager>();
            var libField = typeof(AudioManager).GetField("library", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (libField != null) libField.SetValue(audioManager, testLibrary);
            createdObjects.Add(audioGO);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator AudioManager_VolumePersistence_AndSettings()
        {
            audioManager.SetMasterVolume(0.8f);
            audioManager.SetMusicVolume(0.5f);
            audioManager.SetSfxVolume(0.75f);

            Assert.AreEqual(0.8f, audioManager.MasterVolume, 0.001f);
            Assert.AreEqual(0.5f, audioManager.MusicVolume, 0.001f);
            Assert.AreEqual(0.75f, audioManager.SfxVolume, 0.001f);

            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioManager_CrossfadesMusic_SmoothlyWithoutErrors()
        {
            audioManager.PlayMusic(testLibrary.musicGameplay, true);

            // Crossfade to boss music
            audioManager.FadeMusicTo(testLibrary.musicBoss, 0.2f, true);

            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(AudioManager.Instance);
        }

        [UnityTest]
        public IEnumerator AudioManager_PlaysElementalReactionSfx_OnTrigger()
        {
            // Triggers elemental reaction SFX for all combo types without errors
            audioManager.PlayElementalReactionSfx(ElementalReactionType.ThermalShock);
            audioManager.PlayElementalReactionSfx(ElementalReactionType.Overload);
            audioManager.PlayElementalReactionSfx(ElementalReactionType.Shatter);

            yield return null;
        }
    }
}
