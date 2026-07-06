using UnityEngine;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Extremely simple HUD: builds a top-left gold label in code (legacy UI Text
    /// with Unity's built-in font, so no TMP setup is needed) and keeps it in sync
    /// with the EconomyManager.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private Text goldLabel;

        private void Awake()
        {
            BuildGoldLabel();
        }

        private void BuildGoldLabel()
        {
            GameObject canvasObject = new GameObject("HUD Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();

            GameObject labelObject = new GameObject("GoldLabel");
            labelObject.transform.SetParent(canvasObject.transform, false);

            goldLabel = labelObject.AddComponent<Text>();
            goldLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goldLabel.fontSize = 28;
            goldLabel.color = Color.yellow;
            goldLabel.alignment = TextAnchor.UpperLeft;
            goldLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            goldLabel.verticalOverflow = VerticalWrapMode.Overflow;
            goldLabel.text = "Gold: 0";

            RectTransform rect = goldLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(15f, -15f);
            rect.sizeDelta = new Vector2(400f, 60f);
        }

        private void Update()
        {
            if (goldLabel == null || EconomyManager.Instance == null)
            {
                return;
            }

            goldLabel.text = "Gold: " + EconomyManager.Instance.Gold;
        }
    }
}
