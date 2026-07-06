using UnityEngine;
using UnityEngine.InputSystem;

namespace Stonehold
{
    /// <summary>
    /// Handles player input for towers (new Input System). On left click it raycasts
    /// from the camera: clicking a Tower upgrades it, clicking an empty TowerSlot
    /// places an Arrow Tower if the player can afford it.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private GameObject arrowTowerPrefab;
        [SerializeField] private int towerCost = 50;

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

            if (arrowTowerPrefab == null)
            {
                Debug.LogWarning("TowerManager: arrowTowerPrefab not assigned.");
                return;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(towerCost))
            {
                Debug.Log("Not enough gold to place a tower (need " + towerCost + ").");
                return;
            }

            Instantiate(arrowTowerPrefab, slot.transform.position, Quaternion.identity);
            slot.SetOccupied(true);
            Debug.Log("Placed Arrow Tower for " + towerCost + " gold.");
        }
    }
}
