using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles click interactions on an inventory slot.
/// Depending on the mode, clicking may sell bottles, select seeds for planting,
/// or simply close the inventory.
/// Also shows tooltip on hover (displayName).
/// </summary>
public class InventorySlotClick : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [HideInInspector] public string itemId;     // ID of the item displayed in this slot
    [HideInInspector] public Image iconImage;   // Slot icon reference (set by InventoryUI)

    [Header("Truck Sell Mode (Only for selling wine bottles)")]
    [SerializeField] private bool isTruckSell = false;
    [SerializeField] private TruckSeller truckSeller;

    // ---------------- HOVER TOOLTIP ----------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        ShowTooltip(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryTooltipUI.Instance?.Hide();
    }

    private void ShowTooltip(Vector2 screenPos)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemSO so = inv.GetDefinition(itemId);
        if (so == null) return;

        // אם אין displayName כתוב, ניפול ל-id
        string text = string.IsNullOrWhiteSpace(so.displayName) ? so.id : so.displayName;

        InventoryTooltipUI.Instance?.Show(text, screenPos);
    }

    // ---------------- CLICK ----------------

    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryTooltipUI.Instance?.Hide();
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (string.IsNullOrEmpty(itemId))
            return;

        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[InventorySlotClick] No InventoryManager.Instance");
            return;
        }

        ItemSO so = inv.GetDefinition(itemId);
        if (so == null)
        {
            Debug.LogWarning($"[InventorySlotClick] No ItemSO found for id={itemId}");
            return;
        }

        // ========== TRUCK SELL MODE ==========
        if (isTruckSell)
        {
            if (truckSeller == null)
            {
                Debug.LogWarning("[InventorySlotClick] truckSeller not set");
                return;
            }

            if (!so.isWineBottle)
            {
                Debug.Log($"[InventorySlotClick] {itemId} is not a wine bottle");
                return;
            }

            truckSeller.SellOneBottle(itemId);
            return;
        }

        // ========== PLANTING MODE (Seed selection) ==========
        if (so.isSeed)
        {
            TutorialManager.Instance?.SetFlag("Press Seed");
            PlantingController.Instance?.SelectSeed(so);
        }
        else
        {
            Debug.Log($"[InventorySlotClick] {itemId} is NOT a seed (clicked normally).");
        }

        // ========== UI BEHAVIOR ==========
        var invUI = GetComponentInParent<InventoryUI>();
        if (invUI != null)
        {
            if (invUI.CompareTag("BAG"))
            {
                bool check = InventoryManager.openedBagGardenTutorial = false;
                Debug.Log("Value of BAG is changed from " + check + " Into " + !check);
            }

            invUI.Close();
        }
    }
}
