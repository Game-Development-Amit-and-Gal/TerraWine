using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotClick : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [HideInInspector] public string itemId;
    [HideInInspector] public Image iconImage;

    [Header("Truck Sell Mode (Only for selling wine bottles)")]
    [SerializeField] private bool isTruckSell = false;
    [SerializeField] private TruckSeller truckSeller;

    // ---------------- HOVER TOOLTIP ----------------

    public void OnPointerEnter(PointerEventData eventData) => ShowTooltip(eventData.position);
    public void OnPointerMove(PointerEventData eventData) => ShowTooltip(eventData.position);
    public void OnPointerExit(PointerEventData eventData) => InventoryTooltipUI.Instance?.Hide();

    private void ShowTooltip(Vector2 screenPos)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            InventoryTooltipUI.Instance?.Hide();
            return;
        }

        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            InventoryTooltipUI.Instance?.Hide();
            return;
        }

        ItemSO so = inv.GetDefinition(itemId);
        if (so == null)
        {
            InventoryTooltipUI.Instance?.Hide();
            return;
        }

        string text = string.IsNullOrWhiteSpace(so.displayName) ? so.id : so.displayName;
        InventoryTooltipUI.Instance?.Show(text, screenPos);
    }

    // ---------------- CLICK (slot click) ----------------
    public void OnPointerClick(PointerEventData eventData)
    {
        // תמיד נסגור Tooltip בלחיצה (גם אם זה לא Sell Mode)
        InventoryTooltipUI.Instance?.Hide();

        // ✅ בחירת SEED נעשית דרך PlantButton, לא דרך לחיצה על ה-slot.
        // לכן כאן נשאיר רק Sell Mode.
        if (!isTruckSell) return;

        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (string.IsNullOrEmpty(itemId)) return;

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
    }
}
