using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public string itemId;
    [HideInInspector] public Image iconImage;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (PlantingController.Instance == null) return;
        if (InventoryManager.Instance == null) return;

       
        if (InventoryManager.Instance.CountOf(itemId) <= 0) return;

        
        PlantingController.Instance.SelectSeed(itemId, iconImage.sprite);

      
        var ui = GetComponentInParent<InventoryUI>();
        if (ui != null)
        {
            ui.Close();
        }
    }
}
