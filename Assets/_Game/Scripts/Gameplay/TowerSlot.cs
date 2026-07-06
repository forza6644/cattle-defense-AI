using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// A buildable spot along the path. Can hold one tower at a time. Once occupied
    /// its collider is disabled so clicks pass to the tower placed on top (upgrade).
    /// </summary>
    public class TowerSlot : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }

        public void SetOccupied(bool occupied)
        {
            IsOccupied = occupied;

            Collider slotCollider = GetComponent<Collider>();
            if (slotCollider != null)
            {
                slotCollider.enabled = !occupied;
            }
        }
    }
}
