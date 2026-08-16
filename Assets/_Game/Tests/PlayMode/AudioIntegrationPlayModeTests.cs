using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Stonehold;

namespace Stonehold.Tests
{
    [TestFixture]
    public class AudioIntegrationPlayModeTests
    {
        private GameObject audioObject;
        private AudioManager audioManager;

        [SetUp]
        public void SetUp()
        {
            if (AudioManager.Instance != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey("AudioMasterVol");
            audioObject = new GameObject("TestAudioManager");
            audioManager = audioObject.AddComponent<AudioManager>();
            audioManager.SetMasterVolume(1f);
        }

        [TearDown]
        public void TearDown()
        {
            if (audioObject != null)
            {
                Object.DestroyImmediate(audioObject);
            }
            if (AudioManager.Instance != null && AudioManager.Instance.gameObject != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator AudioManager_InstantiatesAndHandlesNullClipsGracefully()
        {
            Assert.IsNotNull(AudioManager.Instance, "AudioManager Instance must be registered.");
            Assert.AreEqual(1f, AudioManager.Instance.MasterVolume, "Default master volume should be 1.");

            // Test calling playback methods with null library / clips - must not throw exceptions!
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayButton());
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("archer"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("bombardier"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("frost_mage"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("fire_mage"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("electric_engineer"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroShot("sniper"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroImpact("archer", false));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayHeroImpact("fire_mage", true));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayAbilityCast("frost_mage"));
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayLevelUp());
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayPlace());
            Assert.DoesNotThrow(() => AudioManager.Instance.PlayUpgrade());

            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioManager_RespondsToVolumeControls()
        {
            yield return null;

            audioManager.SetMasterVolume(0.5f);
            audioManager.SetMusicVolume(0.4f);
            audioManager.SetSfxVolume(0.8f);

            Assert.AreEqual(0.5f, audioManager.MasterVolume, 0.01f);
            Assert.AreEqual(0.4f, audioManager.MusicVolume, 0.01f);
            Assert.AreEqual(0.8f, audioManager.SfxVolume, 0.01f);
        }

        [UnityTest]
        public IEnumerator StarterCrystal_TriggersAudioOnAttackExecution()
        {
            GameObject crystalGo = new GameObject("TestStarterCrystal");
            StarterCrystal crystal = crystalGo.AddComponent<StarterCrystal>();

            StarterCrystalDefinition def = ScriptableObject.CreateInstance<StarterCrystalDefinition>();
            def.crystalId = "test_crystal";
            def.element = CrystalElement.Fire;
            def.baseDamage = 50f;
            def.attacksPerSecond = 10f;
            def.attackRange = 20f;

            crystal.Configure(def);

            yield return null;

            Assert.DoesNotThrow(() => crystal.LoadSelectedCrystal(), "Loading crystal definition must not throw.");

            Object.DestroyImmediate(crystalGo);
            Object.DestroyImmediate(def);
        }
    }
}
