using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Visual placement feedback controller for hero recruitment touch & drag interactions.
    /// Manages slot highlight indicators, hover feedback, and placement confirmation cues.
    /// </summary>
    public class HeroPlacementFeedbackUI : MonoBehaviour
    {
        public static HeroPlacementFeedbackUI Instance { get; private set; }

        private Color validHighlightColor = new Color(0.2f, 0.9f, 0.3f, 0.6f);
        private Color invalidHighlightColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Color GetHighlightColor(bool isValid)
        {
            return isValid ? validHighlightColor : invalidHighlightColor;
        }

        public bool IsSlotAvailableForPlacement(HeroSlot slot, string heroId)
        {
            if (slot == null || slot.IsOccupied || string.IsNullOrEmpty(heroId))
            {
                return false;
            }

            HeroRosterManager roster = HeroRosterManager.Instance;
            if (roster != null)
            {
                return roster.CanRecruit(heroId);
            }

            return true;
        }
    }
}
