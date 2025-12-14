// Assets/Scripts/Economy/TruckSeller.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TruckSeller : MonoBehaviour
{
    [Header("Selling through the truck")] // Abillity to Sell wine through the truck
    [Tooltip("Modification on the Price '")]
    [Range(0f, 2f)]
    [SerializeField] private float priceMultiplier = 1f;

    [Tooltip(" Trigger in order to Identify whether the player stands near the truck")] 
    [SerializeField] private bool useTrigger = true;

    [Header("Sell UI")]
    [SerializeField] private GameObject sellPanel;   
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private MiniMapClickToMove clickMover;
    [SerializeField] private PlayerMovement regularMover;
    private bool enable = true;

    private bool playerInside = false;

    private void Start() // Hide the panel at first
    {
      
        if (sellPanel != null)
            sellPanel.SetActive(false);

        clickMover = GetComponent<MiniMapClickToMove>();
        regularMover = GetComponent<PlayerMovement>();
    }

    private void Reset() // Automatically sets the truck's collider to Trigger mode so it can detect players entering. 
    {

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) // 
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true; // Player can press E 
        Debug.Log("[In Range] Press E in order to open a sale"); 
    }

    private void OnTriggerExit2D(Collider2D other) 
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = false; // Player can't press E to open a sale
        ClosePanel();
        Debug.Log("[Out of Range] Can't open a sale here");
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Keyboard.current == null) return;

     
        if (Keyboard.current.eKey.wasPressedThisFrame) // After the player is near the Truck and pressed E open the sale pannel
        {
            clickMover.enabled = !enable;
            regularMover.enabled = !enable;
            enable = !enable;
            TogglePanel();
        }
    }



    private void TogglePanel() // Open the sale pannel
    {
        if (sellPanel == null)
        {
            Debug.LogWarning("[TruckSeller] No Pannel has been inserted in the inspector");
            return;
        }

        
        var invUI = sellPanel.GetComponent<InventoryUI>();

      
        bool wantOpen = !sellPanel.activeSelf; // if the player want to open the pannel

        if (invUI != null)
        {
            if (wantOpen)
            {
                invUI.Open();   //Open the Inventory
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


    private void ClosePanel() // Close the panel
    {
        if (sellPanel != null)
            sellPanel.SetActive(false); // hide the pannel on the game
            
    }

    private void RefreshPreview()
    {
        if (summaryText == null) return;

        int preview = CalculateTotalWineValue(); // Calculate Total Wine bottles value
        if (preview > 0)
            summaryText.text = $" ₪{preview} for all Your Wine Bottles"; // for all Your Wine Bottles
        else
            summaryText.text = "No Wine Bottles for Sale";
    }

    private int CalculateTotalWineValue()
    {
        int indicator = 0; // avoid magic numbers
        if (InventoryManager.Instance == null) return indicator; // If the Inventory isnt initialized return.

        List<InventorySlot> wineSlots = InventoryManager.Instance.GetAllWineBottleSlots(); // Get the total Wine Bottles
        int totalMoney = 0; // init the total return value

        int zero = 0; // avoid magic numbers

        foreach (var slot in wineSlots)
        {
            ItemSO item = InventoryManager.Instance.GetDefinition(slot.id); // Get the current Item information
            if (item == null || slot.amount <= zero) continue; // If its null or there aren't any bottles continue.

            int pricePerBottle = Mathf.Max(zero, item.price);  // init the current's kind information
            int amount = slot.amount;

            int value = Mathf.RoundToInt(pricePerBottle * amount * priceMultiplier); // Get the total value per the current kind
            totalMoney += value; // Add it to the return value
        }

        return totalMoney;
    }

    
    public void ConfirmSellAllWine() // Sells the Wine and add the money to the player
    {
        int zero = 0;
        int totalMoney = SellAllWineInternal();

        if (totalMoney > zero) // If there was profit add it to the player's total balance
        {
            GameManager.Instance.AddMoney(totalMoney);
            Debug.Log($"[TruckSeller] Sold wine bottles for {totalMoney}₪. New balance: {GameManager.Instance.Data.money}");
        }
        else // Else, No profit has been made.
        {
            Debug.Log("[TruckSeller] There’s nothing to sell or the price is 0");
        }

        ClosePanel(); // Close Pannel
    }


    public void CancelSell() // Player Regrets, and is cancelling the sale
    {
        Debug.Log("[TruckSeller] You cancelled the sale");
        ClosePanel();
    }


    private int SellAllWineInternal() //Sell all the Wine Bottle and adding the profit to the players balance
    {

        int indicator = 0;
        int zero = 0;
        if (InventoryManager.Instance == null || GameManager.Instance == null)
        {
            Debug.LogWarning("[TruckSeller] Missing InventoryManager or GameManager"); // Missing in the inspector
            return indicator;
        }

        List<InventorySlot> wineSlots = InventoryManager.Instance.GetAllWineBottleSlots(); // Get all the wine bottles
        if (wineSlots.Count == zero)
        {
            return indicator;
        }

        int totalMoney = 0;

        foreach (var slot in new List<InventorySlot>(wineSlots)) // Traverse each wine kind and extract its total value and add to the total sum
        {
            ItemSO item = InventoryManager.Instance.GetDefinition(slot.id); // get the wine information
            if (item == null || slot.amount <= zero) continue;  // if there aren't any continue to the next Kind

            int pricePerBottle = Mathf.Max(zero, item.price); // Get its price per bottle
            int amount = slot.amount; // The amount of bottles

            int value = Mathf.RoundToInt(pricePerBottle * amount * priceMultiplier); // Total value for the Kind of Wine
            totalMoney += value;

            InventoryManager.Instance.Remove(slot.id, amount); // Remove the Bottle
        }

        return totalMoney;
    }
    public void SellOneBottle(string itemId) // Same Function as the above But calculates the value for One bottle
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
