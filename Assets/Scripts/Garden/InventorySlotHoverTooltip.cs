using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private ItemSO currentItem;

    public void SetItem(ItemSO item)
    {
        currentItem = item;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null) return;
        InventoryTooltipUI.Instance?.Show(currentItem.displayName, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryTooltipUI.Instance?.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (currentItem == null) return;
        InventoryTooltipUI.Instance?.Show(currentItem.displayName, eventData.position);
    }
}
