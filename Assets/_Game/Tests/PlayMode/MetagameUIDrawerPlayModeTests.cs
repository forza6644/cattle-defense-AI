using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Stonehold.Tests
{
    public class MetagameUIDrawerPlayModeTests
    {
        private GameObject menuUIGO;
        private MainMenuUI menuUI;
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            BestiaryManager.ResetForTesting();
            AchievementManager.ResetForTesting();
            CampaignProgressionManager.ResetForTesting();
            HeroArtifactManager.ResetForTesting();

            menuUIGO = new GameObject("MainMenuUI");
            menuUI = menuUIGO.AddComponent<MainMenuUI>();
            createdObjects.Add(menuUIGO);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();

            BestiaryManager.ResetForTesting();
            AchievementManager.ResetForTesting();
            CampaignProgressionManager.ResetForTesting();
            HeroArtifactManager.ResetForTesting();
        }

        [UnityTest]
        public IEnumerator MainMenu_CanToggleRelicsDrawer()
        {
            yield return null; // allow BuildMenuDelayed to execute

            Assert.DoesNotThrow(() => menuUI.ShowRelicsDrawer(true));
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowRelicsDrawer(false));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_CanToggleBestiaryDrawer()
        {
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowBestiaryDrawer(true));
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowBestiaryDrawer(false));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_CanToggleAchievementsDrawer()
        {
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowAchievementsDrawer(true));
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowAchievementsDrawer(false));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_CanToggleQuestsDrawer()
        {
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowQuestsDrawer(true));
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowQuestsDrawer(false));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_CanToggleWorldMapDrawer()
        {
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowWorldMapDrawer(true));
            yield return null;

            Assert.DoesNotThrow(() => menuUI.ShowWorldMapDrawer(false));
            yield return null;
        }
    }
}
