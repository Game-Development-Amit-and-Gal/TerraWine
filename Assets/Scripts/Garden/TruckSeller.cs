// Assets/Scripts/Economy/TruckSeller.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TruckSeller : MonoBehaviour
{
    [Header("מכירה דרך המשאית")]
    [Tooltip("כפול על המחיר – 1 = 100%, 0.5 = 50% וכו'")]
    [Range(0f, 2f)]
    [SerializeField] private float priceMultiplier = 1f;

    [Tooltip("האם להשתמש ב-Trigger כדי לזהות שחקן ליד המשאית")]
    [SerializeField] private bool useTrigger = true;

    [Header("UI של מכירה")]
    [SerializeField] private GameObject sellPanel;   
    [SerializeField] private TMP_Text summaryText;    

    private bool playerInside = false;

    private void Start()
    {
      
        if (sellPanel != null)
            sellPanel.SetActive(false);
    }

    private void Reset()
    {

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        Debug.Log("[TruckSeller] נכנסת לאיזור המשאית – לחצי E כדי לפתוח מכירה");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        ClosePanel();
        Debug.Log("[TruckSeller] יצאת מאיזור המשאית");
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Keyboard.current == null) return;

     
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TogglePanel();
        }
    }



    private void TogglePanel()
    {
        if (sellPanel == null)
        {
            Debug.LogWarning("[TruckSeller] אין sellPanel מחובר באינספקטור");
            return;
        }

        
        var invUI = sellPanel.GetComponent<InventoryUI>();

      
        bool wantOpen = !sellPanel.activeSelf;

        if (invUI != null)
        {
            if (wantOpen)
            {
                invUI.Open();   
            }
            else
            {
                invUI.Close();
            }
        }
        else
        {
            
            sellPanel.SetActive(wantOpen);
        }

        if (wantOpen)
        {
            RefreshPreview();
        }
    }


    private void ClosePanel()
    {
        if (sellPanel != null)
            sellPanel.SetActive(false);
    }

    private void RefreshPreview()
    {
        if (summaryText == null) return;

        int preview = CalculateTotalWineValue();
        if (preview > 0)
            summaryText.text = $"את עומדת לקבל ₪{preview} על כל בקבוקי היין";
        else
            summaryText.text = "אין לך בקבוקי יין למכירה";
    }

    private int CalculateTotalWineValue()
    {
        if (InventoryManager.Instance == null) return 0;

        List<InventorySlot> wineSlots = InventoryManager.Instance.GetAllWineBottleSlots();
        int totalMoney = 0;

        foreach (var slot in wineSlots)
        {
            ItemSO item = InventoryManager.Instance.GetDefinition(slot.id);
            if (item == null || slot.amount <= 0) continue;

            int pricePerBottle = Mathf.Max(0, item.price);
            int amount = slot.amount;

            int value = Mathf.RoundToInt(pricePerBottle * amount * priceMultiplier);
            totalMoney += value;
        }

        return totalMoney;
    }

    
    public void ConfirmSellAllWine()
    {
        int totalMoney = SellAllWineInternal();

        if (totalMoney > 0)
        {
            GameManager.Instance.AddMoney(totalMoney);
            Debug.Log($"[TruckSeller] נמכרו בקבוקי יין ב-{totalMoney}. יתרה חדשה: {GameManager.Instance.Data.money}");
        }
        else
        {
            Debug.Log("[TruckSeller] אין מה למכור או המחיר יצא 0");
        }

        ClosePanel();
    }


    public void CancelSell()
    {
        Debug.Log("[TruckSeller] ביטלת את המכירה");
        ClosePanel();
    }


    private int SellAllWineInternal()
    {
        if (InventoryManager.Instance == null || GameManager.Instance == null)
        {
            Debug.LogWarning("[TruckSeller] חסר InventoryManager או GameManager");
            return 0;
        }

        List<InventorySlot> wineSlots = InventoryManager.Instance.GetAllWineBottleSlots();
        if (wineSlots.Count == 0)
        {
            return 0;
        }

        int totalMoney = 0;

        foreach (var slot in new List<InventorySlot>(wineSlots))
        {
            ItemSO item = InventoryManager.Instance.GetDefinition(slot.id);
            if (item == null || slot.amount <= 0) continue;

            int pricePerBottle = Mathf.Max(0, item.price);
            int amount = slot.amount;

            int value = Mathf.RoundToInt(pricePerBottle * amount * priceMultiplier);
            totalMoney += value;

            InventoryManager.Instance.Remove(slot.id, amount);
        }

        return totalMoney;
    }
    public void SellOneBottle(string itemId)
    {
        if (InventoryManager.Instance == null || GameManager.Instance == null)
        {
            Debug.LogWarning("[TruckSeller] Missing InventoryManager or GameManager");
            return;
        }

        ItemSO item = InventoryManager.Instance.GetDefinition(itemId);
        if (item == null || !item.isWineBottle)
            return;

        
        int count = InventoryManager.Instance.CountOf(itemId);
        if (count <= 0)
        {
            Debug.Log("[TruckSeller] No bottles to sell for " + itemId);
            return;
        }

        
        int pricePerBottle = Mathf.Max(0, item.price);
        int money = Mathf.RoundToInt(pricePerBottle * priceMultiplier);

       
        bool ok = InventoryManager.Instance.Remove(itemId, 1);
        if (!ok) return;

       
        GameManager.Instance.AddMoney(money);

       
        RefreshPreview();

        Debug.Log($"[TruckSeller] Sold 1x {item.displayName} for {money}₪");
    }

}
