using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Stonehold.Tests
{
    public class CardVisualsPlayModeTests
    {
        private GameObject uiObject;
        private UIManager uiManager;

        [SetUp]
        public void SetUp()
        {
            uiObject = new GameObject("TestUIManager");
            uiManager = uiObject.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (uiObject != null)
            {
                Object.DestroyImmediate(uiObject);
            }
        }

        [UnityTest]
        public IEnumerator CardDraftUI_RendersValidIconSpritesForDraftChoices()
        {
            yield return null;

            RunProgressionManager.CardChoice[] choices = new RunProgressionManager.CardChoice[]
            {
                new RunProgressionManager.CardChoice(
                    "Add Archer",
                    "Recruit Archer into empty slot.",
                    () => { },
                    "Add",
                    "Common",
                    CardIconSpriteGenerator.GetSpriteForCard("Add Archer", "Add", "archer")
                ),
                new RunProgressionManager.CardChoice(
                    "Add Bombardier",
                    "Recruit Bombardier into empty slot.",
                    () => { },
                    "Add",
                    "Rare",
                    CardIconSpriteGenerator.GetSpriteForCard("Add Bombardier", "Add", "bombardier")
                ),
                new RunProgressionManager.CardChoice(
                    "Deep Freeze",
                    "Applies slow and freeze effects.",
                    () => { },
                    "Card",
                    "Epic",
                    CardIconSpriteGenerator.GetSpriteForCard("Deep Freeze", "Card", "frost_mage")
                )
            };

            uiManager.OnShowLevelUpDraft(choices);
            yield return null;

            Image[] iconImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int validIconCount = 0;

            foreach (Image img in iconImages)
            {
                if (img.gameObject.name == "CardIcon")
                {
                    Assert.IsNotNull(img.sprite, "CardIcon Image must have a valid Sprite assigned.");
                    Assert.Greater(img.sprite.rect.width, 0, "CardIcon Sprite width must be greater than 0.");
                    Assert.Greater(img.sprite.rect.height, 0, "CardIcon Sprite height must be greater than 0.");
                    validIconCount++;
                }
            }

            Assert.AreEqual(3, validIconCount, "All 3 draft choice cards must have rendered CardIcon elements.");
        }

        [Test]
        public void CardIconSpriteGenerator_ProducesUniqueHighContrastSpritesPerCategory()
        {
            Sprite archerSprite = CardIconSpriteGenerator.GetSpriteForCard("Add Archer", "Add", "archer");
            Sprite bombardierSprite = CardIconSpriteGenerator.GetSpriteForCard("Add Bombardier", "Add", "bombardier");
            Sprite frostSprite = CardIconSpriteGenerator.GetSpriteForCard("Add Frost Mage", "Add", "frost_mage");

            Assert.IsNotNull(archerSprite, "Archer card sprite must not be null.");
            Assert.IsNotNull(bombardierSprite, "Bombardier card sprite must not be null.");
            Assert.IsNotNull(frostSprite, "Frost Mage card sprite must not be null.");

            Assert.AreNotEqual(archerSprite, bombardierSprite, "Archer and Bombardier sprites must be distinct.");
            Assert.AreNotEqual(archerSprite, frostSprite, "Archer and Frost Mage sprites must be distinct.");
        }
    }
}
