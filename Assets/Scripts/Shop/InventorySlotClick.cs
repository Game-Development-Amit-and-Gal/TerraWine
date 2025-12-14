using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// Handles click interactions on an inventory slot.
/// Depending on the mode, clicking may sell bottles, select seeds for planting,
/// or simply close the inventory.
/// </summary>
public class InventorySlotClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public string itemId;     // ID of the item displayed in this slot
    [HideInInspector] public Image iconImage;   // Slot icon reference (set by InventoryUI)

    [Header("Truck Sell Mode (Only for selling wine bottles)")]
    [SerializeField] bool isTruckSell = false;  // If TRUE, this slot sells bottles instead of selecting items
    [SerializeField] TruckSeller truckSeller;   // Reference to the seller handling bottle sale

    /// <summary>
    /// This method is called when the user clicks on the slot.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // We only react to left mouse clicks
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Ignore empty slots
        if (string.IsNullOrEmpty(itemId))
            return;

        // Get InventoryManager reference
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[InventorySlotClick] No InventoryManager.Instance");
            return;
        }

        // Get the item definition (ItemSO)
        ItemSO so = inv.GetDefinition(itemId);
        if (so == null)
        {
            Debug.LogWarning($"[InventorySlotClick] No ItemSO found for id={itemId}");
            return;
        }

        // ========== TRUCK SELL MODE ==========
        if (isTruckSell)
        {
            // Ensure we have a reference to the seller
            if (truckSeller == null)
            {
                Debug.LogWarning("[InventorySlotClick] truckSeller not set");
                return;
            }

            // Can only sell wine bottles
            if (!so.isWineBottle)
            {
                Debug.Log($"[InventorySlotClick] {itemId} is not a wine bottle");
                return;
            }

            // Sell one bottle
            truckSeller.SellOneBottle(itemId);
            return;  // Do NOT proceed to planting logic
        }

        // ========== PLANTING MODE (Seed selection) ==========
        if (so.isSeed)
        {
            if (PlantingController.Instance != null)
            {
                // Select the seed to plant
                PlantingController.Instance.SelectSeed(so);
            }
        }
        else
        {
            // Other items perform no special action
            Debug.Log($"[InventorySlotClick] {itemId} is NOT a seed (clicked normally).");
        }

        // ========== UI BEHAVIOR ==========
        // Close the Inventory UI after clicking an item
        var invUI = GetComponentInParent<InventoryUI>();
        if (invUI != null)
        {

            if (invUI.CompareTag("BAG"))
            {
                // for tutorial level 
                bool check = InventoryManager.openedBagGardenTutorial = false;
                Debug.Log("Value of BAG is changed from " + check + " Into " + !check);
            }
            invUI.Close();

        }
    }
}
