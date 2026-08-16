using UnityEngine;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Individual pooled entity representing a floating combat damage number or elemental reaction popup.
    /// Handles billboard orientation towards the camera, drift animation, scale bounce, and smooth alpha fade.
    /// </summary>
    public class FloatingCombatTextItem : MonoBehaviour
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Text textComponent;
        private Outline outlineComponent;
        private Shadow shadowComponent;

        private Vector3 startWorldPos;
        private Vector3 velocity;
        private float totalDuration = 0.85f;
        private float remainingTimer;
        private bool isCrit;
        private float baseScale = 1f;

        private FloatingCombatTextManager poolOwner;

        public bool IsActive => gameObject.activeSelf;

        public void SetupComponents(Font font, FloatingCombatTextManager owner)
        {
            poolOwner = owner;
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300f, 60f);

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            textComponent = GetComponent<Text>();
            if (textComponent == null) textComponent = gameObject.AddComponent<Text>();
            textComponent.font = font;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;

            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
            {
                outlineComponent = gameObject.AddComponent<Outline>();
                outlineComponent.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outlineComponent.effectDistance = new Vector2(1.5f, -1.5f);
            }

            shadowComponent = GetComponent<Shadow>();
            if (shadowComponent == null)
            {
                shadowComponent = gameObject.AddComponent<Shadow>();
                shadowComponent.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shadowComponent.effectDistance = new Vector2(2f, -2f);
            }
        }

        public void Spawn(string message, Vector3 worldPosition, Color textColor, float scaleMultiplier = 1f, bool isCritical = false, float duration = 0.85f)
        {
            startWorldPos = worldPosition + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.6f, 1.0f), Random.Range(-0.25f, 0.25f));
            transform.position = startWorldPos;

            totalDuration = Mathf.Max(0.2f, duration);
            remainingTimer = totalDuration;
            isCrit = isCritical;
            baseScale = scaleMultiplier;

            if (textComponent != null)
            {
                textComponent.text = message;
                textComponent.color = textColor;
                textComponent.fontSize = isCrit ? 22 : 18;
                textComponent.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // Upward drift with slight random horizontal drift
            velocity = new Vector3(Random.Range(-0.35f, 0.35f), isCrit ? 2.2f : 1.6f, Random.Range(-0.1f, 0.1f));

            transform.localScale = Vector3.one * 0.012f * baseScale;
            gameObject.SetActive(true);
            UpdateTransformAndVisuals();
        }

        private void Update()
        {
            if (remainingTimer <= 0f)
            {
                Recycle();
                return;
            }

            remainingTimer -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            UpdateTransformAndVisuals();

            if (remainingTimer <= 0f)
            {
                Recycle();
            }
        }

        private void UpdateTransformAndVisuals()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }

            float progress = 1f - (remainingTimer / totalDuration); // 0 -> 1

            // Scale bounce animation for crits / large popups
            float currentScale = 0.012f * baseScale;
            if (isCrit)
            {
                if (progress < 0.2f)
                {
                    float t = progress / 0.2f;
                    currentScale *= Mathf.Lerp(0.5f, 1.5f, t);
                }
                else if (progress < 0.4f)
                {
                    float t = (progress - 0.2f) / 0.2f;
                    currentScale *= Mathf.Lerp(1.5f, 1.1f, t);
                }
                else
                {
                    currentScale *= 1.1f;
                }
            }
            transform.localScale = Vector3.one * currentScale;

            // Alpha fade in the last 40% of duration
            if (canvasGroup != null)
            {
                if (progress > 0.6f)
                {
                    float fadeT = (progress - 0.6f) / 0.4f;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        public void Recycle()
        {
            gameObject.SetActive(false);
            if (poolOwner != null)
            {
                poolOwner.ReturnToPool(this);
            }
        }
    }
}
