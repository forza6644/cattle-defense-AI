using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class BatterySaverSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("settings_target_fps");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("settings_target_fps");
        }

        [Test]
        public void TargetFps_DefaultsTo60()
        {
            Assert.AreEqual(60, MainMenuUI.TargetFps);
        }

        [Test]
        public void TargetFps_Set30_PersistsAndAppliesFrameRate()
        {
            MainMenuUI.TargetFps = 30;
            Assert.AreEqual(30, MainMenuUI.TargetFps);
            Assert.AreEqual(30, Application.targetFrameRate);

            // Re-read directly from PlayerPrefs
            int pref = PlayerPrefs.GetInt("settings_target_fps", -1);
            Assert.AreEqual(30, pref);
        }

        [Test]
        public void TargetFps_Set60_PersistsAndAppliesFrameRate()
        {
            MainMenuUI.TargetFps = 30;
            MainMenuUI.TargetFps = 60;
            Assert.AreEqual(60, MainMenuUI.TargetFps);
            Assert.AreEqual(60, Application.targetFrameRate);
        }

        [Test]
        public void TargetFps_ClampsNonStandardValues()
        {
            MainMenuUI.TargetFps = 25;
            Assert.AreEqual(30, MainMenuUI.TargetFps, "Values <= 30 should clamp to 30 FPS.");

            MainMenuUI.TargetFps = 90;
            Assert.AreEqual(60, MainMenuUI.TargetFps, "Values > 30 should clamp to 60 FPS.");
        }
    }
}
