using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Stonehold.Tests
{
    [TestFixture]
    public class MobileQualityTierTests
    {
        [SetUp]
        public void SetUp()
        {
            MobileQualityManager.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            MobileQualityManager.ResetForTesting();
        }

        [Test]
        public void QualitySettings_ConfiguredWith3MobileTiers()
        {
            string[] names = QualitySettings.names;
            Assert.IsNotNull(names);
            Assert.AreEqual(3, names.Length, "QualitySettings must contain exactly 3 tiers: Low, Medium, High.");
            Assert.AreEqual("Low", names[0]);
            Assert.AreEqual("Medium", names[1]);
            Assert.AreEqual("High", names[2]);
        }

        [Test]
        public void MobileQualityManager_DefaultTier_IsMedium()
        {
            Assert.AreEqual(MobileQualityTier.Medium, MobileQualityManager.CurrentTier);
            Assert.AreEqual("Medium (Balanced)", MobileQualityManager.GetTierDisplayName(MobileQualityTier.Medium));
        }

        [Test]
        public void MobileQualityManager_SetQualityTier_SwitchesAndPersists()
        {
            bool eventFired = false;
            MobileQualityTier receivedTier = MobileQualityTier.Medium;

            MobileQualityManager.OnQualityTierChanged += (tier) =>
            {
                eventFired = true;
                receivedTier = tier;
            };

            // Switch to Low (Battery Saver)
            MobileQualityManager.SetQualityTier(MobileQualityTier.Low);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(MobileQualityTier.Low, receivedTier);
            Assert.AreEqual(MobileQualityTier.Low, MobileQualityManager.CurrentTier);
            Assert.AreEqual(0, QualitySettings.GetQualityLevel());
            Assert.AreEqual("Low (Battery Saver)", MobileQualityManager.GetTierDisplayName(MobileQualityTier.Low));

            // Switch to High (Crisp)
            eventFired = false;
            MobileQualityManager.SetQualityTier(MobileQualityTier.High);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(MobileQualityTier.High, receivedTier);
            Assert.AreEqual(MobileQualityTier.High, MobileQualityManager.CurrentTier);
            Assert.AreEqual(2, QualitySettings.GetQualityLevel());
            Assert.AreEqual("High (Crisp)", MobileQualityManager.GetTierDisplayName(MobileQualityTier.High));
        }

        [Test]
        public void MobileQualityManager_TierDescriptions_AreValid()
        {
            string lowDesc = MobileQualityManager.GetTierDescription(MobileQualityTier.Low);
            string medDesc = MobileQualityManager.GetTierDescription(MobileQualityTier.Medium);
            string highDesc = MobileQualityManager.GetTierDescription(MobileQualityTier.High);

            Assert.IsNotEmpty(lowDesc);
            Assert.IsNotEmpty(medDesc);
            Assert.IsNotEmpty(highDesc);
            Assert.IsTrue(lowDesc.Contains("battery", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(medDesc.Contains("60 FPS", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(highDesc.Contains("MSAA Off", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(medDesc.Contains("MSAA Off", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(highDesc.Contains("4x", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
