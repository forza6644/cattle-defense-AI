using NUnit.Framework;
using UnityEngine;

namespace Stonehold.Tests
{
    [TestFixture]
    public class HeroArtifactManagerTests
    {
        private GameObject go;
        private HeroArtifactManager manager;

        [SetUp]
        public void SetUp()
        {
            HeroArtifactManager.ResetForTesting();
            go = new GameObject("TestHeroArtifactManager");
            manager = go.AddComponent<HeroArtifactManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
            HeroArtifactManager.ResetForTesting();
        }

        [Test]
        public void Catalog_HasDefinedRelics()
        {
            Assert.IsNotNull(manager.AllCatalog);
            Assert.GreaterOrEqual(manager.AllCatalog.Count, 5, "Relic catalog should contain defined artifacts.");
        }

        [Test]
        public void EquipAndUnequipRelic_ModifiesStatBonuses()
        {
            manager.UnequipRelic(0);
            manager.UnequipRelic(1);
            manager.UnequipRelic(2);

            Assert.AreEqual(0f, manager.TotalReactionDamageBonus);

            manager.EquipRelic("relic_pyromancer_crown", 0);
            Assert.Greater(manager.TotalReactionDamageBonus, 0f, "Equipping pyromancer crown should increase reaction damage bonus.");

            manager.UnequipRelic(0);
            Assert.AreEqual(0f, manager.TotalReactionDamageBonus, "Unequipping should clear stat bonus.");
        }

        [Test]
        public void UnlockRelic_AddsToInventory()
        {
            string newRelic = "relic_chrono_dial";
            bool unlocked = manager.UnlockRelic(newRelic);

            Assert.IsTrue(unlocked || manager.IsUnlocked(newRelic));
            Assert.IsTrue(manager.IsUnlocked(newRelic));
        }
    }
}
