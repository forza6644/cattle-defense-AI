using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class HapticFeedbackManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("settings_haptics_enabled");
            HapticFeedbackManager.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("settings_haptics_enabled");
            HapticFeedbackManager.ResetForTesting();
        }

        [Test]
        public void Haptics_DefaultsToEnabled()
        {
            Assert.IsTrue(HapticFeedbackManager.IsHapticsEnabled, "Haptics should default to enabled.");
        }

        [Test]
        public void Haptics_Toggle_InvertsStateAndPersists()
        {
            Assert.IsTrue(HapticFeedbackManager.IsHapticsEnabled);
            HapticFeedbackManager.ToggleHaptics();
            Assert.IsFalse(HapticFeedbackManager.IsHapticsEnabled, "Haptics should be disabled after toggle.");

            HapticFeedbackManager.ResetForTesting();
            Assert.IsFalse(HapticFeedbackManager.IsHapticsEnabled, "Disabled state should persist across reset.");

            HapticFeedbackManager.ToggleHaptics();
            Assert.IsTrue(HapticFeedbackManager.IsHapticsEnabled, "Haptics should be enabled again after second toggle.");
        }

        [Test]
        public void Haptics_TriggerMethods_DoNotThrowExceptions()
        {
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerLight());
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerMedium());
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerHeavy());
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerImpact(HapticImpactType.Light));
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerImpact(HapticImpactType.Medium));
            Assert.DoesNotThrow(() => HapticFeedbackManager.TriggerImpact(HapticImpactType.Heavy));
        }
    }
}
