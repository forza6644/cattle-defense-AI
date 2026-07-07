using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Stonehold
{
    /// <summary>
    /// Player interaction with towers. Supports mouse (desktop) and touch (mobile)
    /// through the new Input System: both funnel into one world-click handler that
    /// raycasts from the camera. Clicking a Tower opens the upgrade/sell panel,
    /// clicking an empty TowerSlot opens the build menu. Placement, upgrade and
    /// sell all move gold through the EconomyManager; costs and refunds come from
    /// ScriptableObjects.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private TowerData[] availableTowers;

        public TowerData[] AvailableTowers => availableTowers;
        public GameConfig Config => config;

        private Camera cam;

        private void Start()
        {
            if (config != null && config.draftRunMode)
            {
                PlaceStartingDefender();
            }
        }

        private void PlaceStartingDefender()
        {
            string selectedId = SaveManager.SelectedStartingDefenderId;
            if (string.IsNullOrEmpty(selectedId))
            {
                selectedId = "archer_defender";
            }

            TowerData startingDefender = null;
            if (availableTowers != null)
            {
                foreach (var t in availableTowers)
                {
                    if (t != null && t.defenderId == selectedId)
                    {
                        startingDefender = t;
                        break;
                    }
                }
            }

            // Fallback to default config starting defender
            if (startingDefender == null && config != null)
            {
                startingDefender = config.defaultStartingDefender;
            }

            // Final fallback to archer_defender
            if (startingDefender == null && availableTowers != null)
            {
                foreach (var t in availableTowers)
                {
                    if (t != null && t.defenderId == "archer_defender")
                    {
                        startingDefender = t;
                        break;
                    }
                }
            }

            if (startingDefender == null)
            {
                Debug.LogWarning("TowerManager: No starting defender configured or found!");
                return;
            }

            TowerSlot[] slots = FindObjectsByType<TowerSlot>();
            if (slots == null || slots.Length == 0)
            {
                Debug.LogWarning("TowerManager: No TowerSlots found in the scene.");
                return;
            }

            System.Array.Sort(slots, (a, b) => string.Compare(a.name, b.name));

            int centerIndex = slots.Length / 2;
            TowerSlot centerSlot = slots[centerIndex];

            if (centerSlot != null && !centerSlot.IsOccupied)
            {
                PlaceTowerFree(centerSlot, startingDefender);
                Debug.Log($"TowerManager: Placed starting defender {startingDefender.towerName} on center slot {centerSlot.name}");
            }
        }

        private void Update()
        {
            if (!TryGetPointerPress(out Vector2 screenPos))
            {
                return;
            }

            // Presses on UI elements belong to the UI, not the world.
            if (IsPointerOverUI())
            {
                return;
            }

            HandleWorldClick(screenPos);
        }

        /// <summary>True on the frame a mouse click or first touch began.</summary>
        private static bool TryGetPointerPress(out Vector2 screenPos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            screenPos = default;
            return false;
        }

        private static bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (eventSystem.IsPointerOverGameObject())
            {
                return true;
            }

            // Touch pointers are tracked by finger id in the UI module.
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                if (eventSystem.IsPointerOverGameObject(touchId))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleWorldClick(Vector2 screenPos)
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
            }

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
                if (config != null && config.draftRunMode)
                {
                    return;
                }

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

            if (UnlockManager.Instance != null && !UnlockManager.Instance.IsTowerUnlocked(towerData))
            {
                Debug.Log(towerData.towerName + " is locked. " + UnlockManager.Instance.GetLockMessage(towerData));
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
            if (tower == null)
            {
                tower = placed.GetComponentInChildren<Tower>();
            }

            if (tower != null)
            {
                tower.Slot = slot;
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayPlace(slot.transform.position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlace();
            }

            Debug.Log("Placed " + towerData.towerName + " for " + towerData.cost + " gold.");
            return true;
        }

        /// <summary>Buys and places a tower on the given slot for free (e.g. from draft rewards).</summary>
        public bool PlaceTowerFree(TowerSlot slot, TowerData towerData)
        {
            if (slot == null || slot.IsOccupied || towerData == null || towerData.prefab == null)
            {
                return false;
            }

            GameObject placed = Instantiate(towerData.prefab, slot.transform.position, Quaternion.identity);
            slot.SetOccupied(true);

            Tower tower = placed.GetComponent<Tower>();
            if (tower == null)
            {
                tower = placed.GetComponentInChildren<Tower>();
            }

            if (tower != null)
            {
                tower.Slot = slot;
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayPlace(slot.transform.position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlace();
            }

            Debug.Log("Placed " + towerData.towerName + " for FREE.");
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
