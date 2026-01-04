// Assets/Scripts/Economy/TruckSeller.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TruckSeller : MonoBehaviour
{
    [Header("Selling through the truck")]
    [Range(0f, 2f)]
    [SerializeField] private float priceMultiplier = 1f;

    [SerializeField] private bool useTrigger = true;

    [Header("Sell UI")]
    [SerializeField] private GameObject sellPanel;
    [SerializeField] private TMP_Text summaryText;

    // Drag these from the PLAYER in Inspector
    [SerializeField] private MiniMapClickToMove clickMover;
    [SerializeField] private PlayerMovement regularMover;

    private bool playerInside = false;

    private void Start()
    {
        if (sellPanel != null)
            sellPanel.SetActive(false);
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        Debug.Log("[In Range] Press E in order to open a sale");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        // Close + restore movement when leaving
        ClosePanelAndRestoreMovement();

        Debug.Log("[Out of Range] Can't open a sale here");
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // ESC closes panel anywhere
        if (IsPanelOpen() && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanelAndRestoreMovement();
            return;
        }

        if (!playerInside) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (IsPanelOpen())
            {
                ClosePanelAndRestoreMovement();
            }
            else
            {
                OpenPanelAndBlockMovement();
            }
        }
    }

    private bool IsPanelOpen()
    {
        return sellPanel != null && sellPanel.activeSelf;
    }

    private void SetMovementEnabled(bool canMove)
    {
        if (clickMover != null) clickMover.enabled = canMove;
        if (regularMover != null) regularMover.enabled = canMove;
    }

    private bool EnsureMoversAssigned()
    {
        if (clickMover == null || regularMover == null)
        {
            Debug.LogError(
                "[TruckSeller] clickMover/regularMover not assigned. " +
                "Assign them in Inspector from the PLAYER (MiniMapClickToMove + PlayerMovement)."
            );
            return false;
        }
        return true;
    }

    private void OpenPanelAndBlockMovement()
    {
        if (sellPanel == null)
        {
            Debug.LogWarning("[TruckSeller] No Panel has been inserted in the inspector");
            return;
        }

        if (!EnsureMoversAssigned()) return;

        // Block movement
        SetMovementEnabled(false);

        // Open panel (prefer InventoryUI.Open if exists)
        var invUI = sellPanel.GetComponent<InventoryUI>();
        if (invUI != null) invUI.Open();
        else sellPanel.SetActive(true);

        RefreshPreview();
    }

    private void ClosePanelAndRestoreMovement()
    {
        InventoryTooltipUI.Instance?.Hide();
        // Close panel (prefer InventoryUI.Close if exists)
        if (sellPanel != null)
        {
            var invUI = sellPanel.GetComponent<InventoryUI>();
            if (invUI != null) invUI.Close();
            else sellPanel.SetActive(false);
        }

        // Restore movement
        SetMovementEnabled(true);
    }

    private void RefreshPreview()
    {
        if (summaryText == null) return;

        int preview = CalculateTotalWineValue();
        summaryText.text = (preview > 0)
            ? $" ₪{preview} for all Your Wine Bottles"
            : "No Wine Bottles for Sale";
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

            int value = Mathf.RoundToInt(Mathf.Max(0, item.price) * slot.amount * priceMultiplier);
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
            Debug.Log($"[TruckSeller] Sold wine bottles for {totalMoney}₪. New balance: {GameManager.Instance.Data.money}");
        }
        else
        {
            Debug.Log("[TruckSeller] There’s nothing to sell or the price is 0");
        }

        // Close + restore movement
        ClosePanelAndRestoreMovement();
    }

    public void CancelSell()
    {
        Debug.Log("[TruckSeller] You cancelled the sale");

        // Close + restore movement
        ClosePanelAndRestoreMovement();
    }

    private int SellAllWineInternal()
    {
        if (InventoryManager.Instance == null || GameManager.Instance == null)
        {
            Debug.LogWarning("[TruckSeller] Missing InventoryManager or GameManager");
            return 0;
        }

        List<InventorySlot> wineSlots = InventoryManager.Instance.GetAllWineBottleSlots();
        if (wineSlots.Count == 0) return 0;

        int totalMoney = 0;

        foreach (var slot in new List<InventorySlot>(wineSlots))
        {
            ItemSO item = InventoryManager.Instance.GetDefinition(slot.id);
            if (item == null || slot.amount <= 0) continue;

            int value = Mathf.RoundToInt(Mathf.Max(0, item.price) * slot.amount * priceMultiplier);
            totalMoney += value;

            InventoryManager.Instance.Remove(slot.id, slot.amount);
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
        if (item == null || !item.isWineBottle) return;

        int count = InventoryManager.Instance.CountOf(itemId);
        if (count <= 0)
        {
            Debug.Log("[TruckSeller] No bottles to sell for " + itemId);
            return;
        }

        int money = Mathf.RoundToInt(Mathf.Max(0, item.price) * priceMultiplier);

        bool ok = InventoryManager.Instance.Remove(itemId, 1);
        if (!ok) return;

        GameManager.Instance.AddMoney(money);

        RefreshPreview();
        Debug.Log($"[TruckSeller] Sold 1x {item.displayName} for {money}₪");
    }
}
