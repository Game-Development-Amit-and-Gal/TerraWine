using UnityEngine;
using UnityEngine.EventSystems;   // בשביל IPointerClickHandler
using UnityEngine.UI;            // בשביל Image

public class InventorySlotClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public string itemId;
    [HideInInspector] public Image iconImage;

    public void OnPointerClick(PointerEventData eventData)
    {
        // רק קליק שמאלי
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (string.IsNullOrEmpty(itemId))
            return;

        // טוענים את ה-ItemSO לפי ה-id
        ItemSO so = Resources.Load<ItemSO>($"Items/{itemId}");
        if (so == null)
        {
            Debug.LogWarning($"[InventorySlotClick] No ItemSO found for id={itemId}");
            return;
        }

        // --- אם זה seed → בחירת זרע ל-PlantingController ---
        if (so.isSeed)
        {
            if (PlantingController.Instance != null && iconImage != null)
            {
                PlantingController.Instance.SelectSeed(itemId, iconImage.sprite);
            }
        }
        else
        {
            // פה בעתיד אפשר לשים לוגיקה של גדרות / דקורציה / כלים
            Debug.Log($"[InventorySlotClick] {itemId} is NOT a seed (clicked normally).");
        }

        // 👇 בכל מקרה (seed או לא) – נסגור את הקנבס של האינבנטורי
        var invUI = GetComponentInParent<InventoryUI>();
        if (invUI != null)
        {
            invUI.Close();
        }
    }
}
