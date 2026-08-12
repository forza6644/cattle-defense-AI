using UnityEngine;
using UnityEngine.EventSystems;

namespace Stonehold
{
    /// <summary>
    /// Handles mobile touch and mouse raycast interactions for hero placement into active HeroSlots.
    /// Provides precise 3D raycasting to identify empty hero slot bounds during drag-and-drop
    /// or tap-to-place UX, enforcing roster constraints and slot availability.
    /// </summary>
    public class HeroPlacementTouchHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float maxRaycastDistance = 100f;

        private HeroSlot currentHoveredSlot;
        private string pendingHeroId;

        public HeroSlot CurrentHoveredSlot => currentHoveredSlot;
        public string PendingHeroId => pendingHeroId;

        public void SetPendingHero(string heroId)
        {
            pendingHeroId = heroId;
        }

        public void ClearPendingHero()
        {
            pendingHeroId = null;
            currentHoveredSlot = null;
        }

        /// <summary>
        /// Performs a 3D raycast from the given screen point to find an active HeroSlot.
        /// </summary>
        public static bool TryRaycastHeroSlot(Vector3 screenPosition, Camera cameraToUse, out HeroSlot targetSlot)
        {
            targetSlot = null;
            Camera cam = cameraToUse != null ? cameraToUse : Camera.main;
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;

                HeroSlot slot = hit.collider.GetComponentInParent<HeroSlot>();
                if (slot != null && slot.gameObject.activeInHierarchy)
                {
                    targetSlot = slot;
                    return true;
                }
            }

            return false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            UpdateHoveredSlot(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateHoveredSlot(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(pendingHeroId) && currentHoveredSlot != null)
            {
                TryPlaceHeroIntoSlot(pendingHeroId, currentHoveredSlot);
            }
            ClearPendingHero();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;

            if (TryRaycastHeroSlot(eventData.position, raycastCamera, out HeroSlot hitSlot))
            {
                if (!string.IsNullOrEmpty(pendingHeroId))
                {
                    TryPlaceHeroIntoSlot(pendingHeroId, hitSlot);
                    ClearPendingHero();
                }
            }
        }

        private void UpdateHoveredSlot(Vector2 screenPosition)
        {
            if (TryRaycastHeroSlot(screenPosition, raycastCamera, out HeroSlot slot))
            {
                currentHoveredSlot = slot;
            }
            else
            {
                currentHoveredSlot = null;
            }
        }

        public bool TryPlaceHeroIntoSlot(string heroId, HeroSlot targetSlot)
        {
            if (string.IsNullOrEmpty(heroId) || targetSlot == null || targetSlot.IsOccupied)
            {
                return false;
            }

            HeroRosterManager roster = HeroRosterManager.Instance;
            if (roster != null)
            {
                return roster.RecruitHeroIntoSlot(heroId, targetSlot);
            }

            return false;
        }
    }
}
