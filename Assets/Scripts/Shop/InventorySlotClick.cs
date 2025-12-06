using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public string itemId;
    [HideInInspector] public Image iconImage;

    [Header("מצב מכירה במשאית")]
    [SerializeField] bool isTruckSell = false;   
    [SerializeField] TruckSeller truckSeller;   

    public void OnPointerClick(PointerEventData eventData)
    {
       
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

        
        if (so.isSeed)
        {
            if (PlantingController.Instance != null)
            {
                PlantingController.Instance.SelectSeed(so);
            }
        }
        else
        {
            Debug.Log($"[InventorySlotClick] {itemId} is NOT a seed (clicked normally).");
        }

     
        var invUI = GetComponentInParent<InventoryUI>();
        if (invUI != null)
        {
            invUI.Close();
        }
    }
}
