using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class MetaProgressionStorePlayModeTests
    {
        private GameObject container;
        private MetaUpgradeManager manager;
        private int originalMetaGold;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 2.0f;
            originalMetaGold = SaveManager.MetaGold;

            if (MetaUpgradeManager.Instance != null)
            {
                manager = MetaUpgradeManager.Instance;
            }
            else
            {
                container = new GameObject("MetaProgressionStoreTests_Container");
                manager = container.AddComponent<MetaUpgradeManager>();
            }
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
            if (container != null)
            {
                Object.DestroyImmediate(container);
            }
            SaveManager.ResetAll();
            if (originalMetaGold > 0)
            {
                SaveManager.AddMetaGold(originalMetaGold);
            }
            if (manager != null)
            {
                manager.LoadUpgrades();
            }
        }

        [UnityTest]
        public IEnumerator PurchaseUpgrade_DeductsMetaGold_IncrementsLevel_AndPersistsToSaveManager()
        {
            yield return null;

            SaveManager.ResetAll();
            SaveManager.AddMetaGold(1000);
            manager.LoadUpgrades();

            int initialGold = SaveManager.MetaGold;
            Assert.AreEqual(1000, initialGold);

            var hpUpgrade = manager.Upgrades.FirstOrDefault(u => u.id == "castle_hp");
            Assert.IsNotNull(hpUpgrade, "Castle HP upgrade definition must exist.");
            int cost = hpUpgrade.GetCost();
            Assert.AreEqual(100, cost, "Base cost for Castle HP upgrade should be 100.");

            bool purchased = manager.PurchaseUpgrade("castle_hp");
            Assert.IsTrue(purchased, "PurchaseUpgrade should return true with sufficient MetaGold.");
            Assert.AreEqual(1000 - cost, SaveManager.MetaGold, "MetaGold must be deducted by exact upgrade cost.");
            Assert.AreEqual(1, SaveManager.GetUpgradeLevel("castle_hp"), "SaveManager upgrade level should be 1.");
            Assert.AreEqual(10, manager.GetCastleHpBonus(), "Castle HP bonus (+10 per level) should equal 10.");
        }

        [UnityTest]
        public IEnumerator PurchaseUpgrade_InsufficientGold_ReturnsFalseAndPreservesState()
        {
            yield return null;

            SaveManager.ResetAll();
            SaveManager.AddMetaGold(20);
            manager.LoadUpgrades();

            var damageUpgrade = manager.Upgrades.FirstOrDefault(u => u.id == "damage");
            Assert.IsNotNull(damageUpgrade, "Crystal Attack upgrade definition must exist.");
            int cost = damageUpgrade.GetCost();
            Assert.AreEqual(150, cost, "Base cost for Crystal Attack should be 150.");

            bool purchased = manager.PurchaseUpgrade("damage");
            Assert.IsFalse(purchased, "PurchaseUpgrade must return false when player has insufficient MetaGold.");
            Assert.AreEqual(20, SaveManager.MetaGold, "MetaGold must not be deducted on failed purchase.");
            Assert.AreEqual(0, SaveManager.GetUpgradeLevel("damage"), "Upgrade level must remain 0.");
            Assert.AreEqual(1.0f, manager.GetGlobalDamageMultiplier(), 0.001f, "Global damage multiplier should remain 1.0.");
        }

        [UnityTest]
        public IEnumerator PermanentUpgrades_PersistAcrossSaveLoad()
        {
            yield return null;

            SaveManager.ResetAll();
            SaveManager.AddMetaGold(2000);
            manager.LoadUpgrades();

            Assert.IsTrue(manager.PurchaseUpgrade("castle_hp"), "First Castle HP purchase must succeed.");
            Assert.IsTrue(manager.PurchaseUpgrade("damage"), "First Crystal Attack purchase must succeed.");
            Assert.IsTrue(manager.PurchaseUpgrade("gold_bonus"), "First MetaGold Bonus purchase must succeed.");

            int levelHpBefore = SaveManager.GetUpgradeLevel("castle_hp");
            int levelDamageBefore = SaveManager.GetUpgradeLevel("damage");
            int levelGoldBefore = SaveManager.GetUpgradeLevel("gold_bonus");

            Assert.AreEqual(1, levelHpBefore);
            Assert.AreEqual(1, levelDamageBefore);
            Assert.AreEqual(1, levelGoldBefore);

            // Simulate reload / restart
            SaveManager.LoadProgress();
            manager.LoadUpgrades();

            Assert.AreEqual(1, SaveManager.GetUpgradeLevel("castle_hp"));
            Assert.AreEqual(1, SaveManager.GetUpgradeLevel("damage"));
            Assert.AreEqual(1, SaveManager.GetUpgradeLevel("gold_bonus"));

            Assert.AreEqual(10, manager.GetCastleHpBonus());
            Assert.AreEqual(1.15f, manager.GetGlobalDamageMultiplier(), 0.001f);
            Assert.AreEqual(1.10f, manager.GetGoldBonusMultiplier(), 0.001f);
        }

        [UnityTest]
        public IEnumerator UpgradeCostGrowth_IncreasesCostPerLevel()
        {
            yield return null;

            SaveManager.ResetAll();
            SaveManager.AddMetaGold(5000);
            manager.LoadUpgrades();

            var u = manager.Upgrades.FirstOrDefault(x => x.id == "castle_hp");
            Assert.IsNotNull(u);

            int costLvl0 = u.GetCost();
            Assert.AreEqual(100, costLvl0);

            Assert.IsTrue(manager.PurchaseUpgrade("castle_hp"));
            int costLvl1 = u.GetCost();
            Assert.AreEqual(150, costLvl1, "Level 1 cost with 1.5 growth should be 150.");

            Assert.IsTrue(manager.PurchaseUpgrade("castle_hp"));
            int costLvl2 = u.GetCost();
            Assert.AreEqual(225, costLvl2, "Level 2 cost with 1.5 growth should be 225.");
        }
    }
}
