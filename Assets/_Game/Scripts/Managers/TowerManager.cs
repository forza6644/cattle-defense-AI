using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Stonehold
{
    /// <summary>
    /// Player interaction with towers. On left click (new Input System) it raycasts
    /// from the camera: clicking a Tower opens the upgrade/sell panel, clicking an
    /// empty TowerSlot opens the build menu. Placement, upgrade and sell all move
    /// gold through the EconomyManager; costs and refunds come from ScriptableObjects.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private TowerData[] availableTowers;

        public TowerData[] AvailableTowers => availableTowers;

        private Camera cam;

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            // Clicks on UI elements belong to the UI, not the world.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideSelectionPanels();
                }

                return;
            }

            Tower tower = hit.collider.GetComponentInParent<Tower>();
            if (tower != null)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowTowerPanel(tower);
                }

                return;
            }

            TowerSlot slot = hit.collider.GetComponentInParent<TowerSlot>();
            if (slot != null && !slot.IsOccupied)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBuildMenu(slot);
                }

                return;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideSelectionPanels();
            }
        }

        /// <summary>Buys and places a tower on the given slot. Returns true on success.</summary>
        public bool PlaceTower(TowerSlot slot, TowerData towerData)
        {
            if (slot == null || slot.IsOccupied || towerData == null || towerData.prefab == null)
            {
                return false;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(towerData.cost))
            {
                Debug.Log("Not enough gold to place " + towerData.towerName + " (need " + towerData.cost + ").");
                return false;
            }

            GameObject placed = Instantiate(towerData.prefab, slot.transform.position, Quaternion.identity);
            slot.SetOccupied(true);

            Tower tower = placed.GetComponent<Tower>();
            if (tower != null)
            {
                tower.Slot = slot;
            }

            Debug.Log("Placed " + towerData.towerName + " for " + towerData.cost + " gold.");
            return true;
        }

        /// <summary>Refund for selling the tower, from GameConfig's refund fraction.</summary>
        public int GetSellValue(Tower tower)
        {
            float refund = config != null ? config.sellRefundPercent : 0.5f;
            return Mathf.RoundToInt(tower.TotalInvested * refund);
        }

        /// <summary>Sells a tower: refunds gold and frees its slot.</summary>
        public void SellTower(Tower tower)
        {
            if (tower == null)
            {
                return;
            }

            int refund = GetSellValue(tower);
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(refund);
            }

            if (tower.Slot != null)
            {
                tower.Slot.SetOccupied(false);
            }

            Debug.Log("Sold " + tower.Data.towerName + " for " + refund + " gold.");
            Destroy(tower.gameObject);
        }
    }
}
