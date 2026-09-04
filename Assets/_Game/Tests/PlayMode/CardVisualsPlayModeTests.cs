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
            Canvas[] existingCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingCanvases.Length; i++)
            {
                if (existingCanvases[i] != null && existingCanvases[i].gameObject != null && existingCanvases[i].gameObject.name != "FaderCanvas")
                {
                    Object.DestroyImmediate(existingCanvases[i].gameObject);
                }
            }
            GameObject[] existingCards = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingCards.Length; i++)
            {
                if (existingCards[i] != null && existingCards[i].name.StartsWith("Card_"))
                {
                    Object.DestroyImmediate(existingCards[i]);
                }
            }

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
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject != null && canvases[i].gameObject.name != "FaderCanvas")
                {
                    Object.DestroyImmediate(canvases[i].gameObject);
                }
            }
            GameObject[] cards = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].name.StartsWith("Card_"))
                {
                    Object.DestroyImmediate(cards[i]);
                }
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

        [UnityTest]
        public IEnumerator CardDraftUI_FullCardSurface_IsTappable()
        {
            yield return null;

            RunProgressionManager.CardChoice[] choices = new RunProgressionManager.CardChoice[]
            {
                new RunProgressionManager.CardChoice("Add Archer", "Recruit Archer.", () => { }, "Add", "Common"),
                new RunProgressionManager.CardChoice("Add Bombardier", "Recruit Bombardier.", () => { }, "Add", "Rare"),
                new RunProgressionManager.CardChoice("Deep Freeze", "Slow and freeze.", () => { }, "Card", "Epic")
            };

            uiManager.OnShowLevelUpDraft(choices);
            yield return null;

            int tappableCards = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject go = allObjects[i];
                if (go == null || !go.name.StartsWith("Card_"))
                {
                    continue;
                }

                Image image = go.GetComponent<Image>();
                Button button = go.GetComponent<Button>();
                Assert.IsNotNull(image, $"{go.name} must have an Image for full-surface raycasts.");
                Assert.IsTrue(image.raycastTarget, $"{go.name} Image must receive taps; CreateImage defaults raycastTarget off.");
                Assert.IsNotNull(button, $"{go.name} must have a Button covering the card body.");
                Assert.IsTrue(button.interactable, $"{go.name} body button must be interactable while the draft is open.");
                tappableCards++;
            }

            Assert.AreEqual(3, tappableCards, "All 3 draft cards must be full-surface tappable.");
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
