using UnityEngine;
using UnityEngine.InputSystem;

namespace Stonehold
{
    /// <summary>
    /// Handles player input for towers (new Input System). On left click it raycasts
    /// from the camera: clicking a Tower upgrades it, clicking an empty TowerSlot
    /// places a tower. Which tower gets placed (and its cost) comes from TowerData.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private TowerData placedTowerData; // Arrow Tower for now.

        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
            }

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                return;
            }

            // A tower takes priority (upgrade); otherwise try to place on a slot.
            Tower tower = hit.collider.GetComponentInParent<Tower>();
            if (tower != null)
            {
                tower.TryUpgrade();
                return;
            }

            TowerSlot slot = hit.collider.GetComponentInParent<TowerSlot>();
            if (slot != null)
            {
                TryPlaceTower(slot);
            }
        }

        private void TryPlaceTower(TowerSlot slot)
        {
            if (slot.IsOccupied)
            {
                return;
            }

            if (placedTowerData == null || placedTowerData.prefab == null)
            {
                Debug.LogWarning("TowerManager: placedTowerData (or its prefab) not assigned.");
                return;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(placedTowerData.cost))
            {
                Debug.Log("Not enough gold to place a tower (need " + placedTowerData.cost + ").");
                return;
            }

            Instantiate(placedTowerData.prefab, slot.transform.position, Quaternion.identity);
            slot.SetOccupied(true);
            Debug.Log("Placed " + placedTowerData.towerName + " for " + placedTowerData.cost + " gold.");
        }
    }
}
