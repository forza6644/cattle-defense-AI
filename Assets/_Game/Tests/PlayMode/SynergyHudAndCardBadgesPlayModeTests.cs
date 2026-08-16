using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class SynergyHudAndCardBadgesPlayModeTests
    {
        private GameObject uiGO;
        private UIManager uiManager;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            if (UIManager.Instance != null)
            {
                Object.DestroyImmediate(UIManager.Instance.gameObject);
            }

            uiGO = new GameObject("UIManager", typeof(UIManager));
            uiManager = uiGO.GetComponent<UIManager>();
            createdObjects.Add(uiGO);
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
        public IEnumerator UIManager_BuildsSynergyHud_WithoutErrors()
        {
            Assert.IsNotNull(UIManager.Instance, "UIManager instance must be active.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator UIManager_CardSynergyDetector_IdentifiesComboCards()
        {
            var method = typeof(UIManager).GetMethod("CheckCardSynergyWithPlacedHeroes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(method, "CheckCardSynergyWithPlacedHeroes method must exist.");

            var choice = new RunProgressionManager.CardChoice
            {
                title = "Frost Fire Blast",
                description = "Triggers Thermal Shock burst damage",
                cardType = "Boost",
                rarity = "Rare"
            };

            // Should execute without null reference exception even when no heroes are placed yet
            bool result = (bool)method.Invoke(uiManager, new object[] { choice });
            Assert.IsFalse(result); // false when no heroes deployed

            yield return null;
        }
    }
}
