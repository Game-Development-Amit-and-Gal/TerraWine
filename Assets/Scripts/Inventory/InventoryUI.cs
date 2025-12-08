using System.Collections.Generic;      
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the visual inventory window in the UI.
/// It listens for inventory changes, creates item slot UI elements,
/// switches categories (tabs), and supports both normal inventory view
/// and special Sell/Buy modes depending on the scene.
/// </summary>

public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject panel;        // The main inventory UI panel (window to show/hide)
    [SerializeField] Transform gridParent;    // Parent transform that will hold all the item slot UI elements
    [SerializeField] GameObject slotPrefab;   // Prefab used to visually represent a single inventory slot

    [SerializeField] GameObject extraImage;          // Extra UI image/decorator shown above the bottom bar
    [SerializeField] GameObject ResourcesBottom;     // Bottom UI section used when viewing resource items
    [SerializeField] GameObject WineBottlesBottom;   // Bottom UI section used when viewing wine bottles
    [SerializeField] GameObject DesignBottom;        // Bottom UI section used when viewing design-related items


    [SerializeField] bool isSellUI = false;      // If true, this inventory panel is used for selling items (truck interaction)
    [SerializeField] TruckSeller truckSeller;    // Reference to the TruckSeller script that handles selling logic


    [SerializeField] bool isBuyUI = false;             // If true, this inventory panel is used for buying items (shop mode)
    [SerializeField] GameObject UpdateBottom;          // Bottom UI shown when viewing update/upgrade purchase options
    [SerializeField] GameObject SecurityBottom;        // Bottom UI shown when buying security-related items
    [SerializeField] GameObject DesignBuyBottom;       // Bottom UI shown when buying design-themed items



    [SerializeField] ItemCategory currentCategory = ItemCategory.Resources;   // The currently active category/tab being displayed in the inventory


    private void OnEnable() // Bind the onChanged Object with redraw function
    {
        // If there is no InventoryManager (should not happen), stop
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] No InventoryManager in scene!");
            return;
        }

        // Subscribe to inventory change events so UI refreshes automatically
        InventoryManager.Instance.onChanged.AddListener(Redraw);

        // Immediately draw the current inventory when the panel becomes active
        Redraw();
    }


    private void OnDisable()
    {
        // If InventoryManager doesn’t exist, nothing to unsubscribe from
        if (InventoryManager.Instance == null) return;

        // Stop listening to inventory change events when UI is hidden
        InventoryManager.Instance.onChanged.RemoveListener(Redraw);

        // Hide any bottom UI sections when the panel closes
        SetExtraUiActive(false);
    }


    private void SetExtraUiActive(bool active)
    {
        // Toggle the decorative image at the top of the bottom UI section
        if (extraImage != null)
            extraImage.SetActive(active);

        // If NOT in Buy mode → show standard bottom layouts (Resources/Wine/Design)
        if (!isBuyUI)
        {
            // Show standard category bottoms when inventory is active
            if (ResourcesBottom != null)
                ResourcesBottom.SetActive(active);

            if (WineBottlesBottom != null)
                WineBottlesBottom.SetActive(active);

            if (DesignBottom != null)
                DesignBottom.SetActive(active);

            // Hide BUY-related bottoms in this mode
            if (UpdateBottom != null)
                UpdateBottom.SetActive(false);

            if (SecurityBottom != null)
                SecurityBottom.SetActive(false);

            if (DesignBuyBottom != null)
                DesignBuyBottom.SetActive(false);
        }
        else  // If in Buy mode → show purchase UI sections instead
        {
            // Hide the standard inventory bottoms
            if (ResourcesBottom != null)
                ResourcesBottom.SetActive(false);
            if (WineBottlesBottom != null)
                WineBottlesBottom.SetActive(false);
            if (DesignBottom != null)
                DesignBottom.SetActive(false);

            // Show BUY-related bottoms
            if (UpdateBottom != null)
                UpdateBottom.SetActive(active);
            if (SecurityBottom != null)
                SecurityBottom.SetActive(active);
            if (DesignBuyBottom != null)
                DesignBuyBottom.SetActive(active);
        }
    }


    public void Toggle()
    {
        // Flip the panel’s current visibility state (open ↔ closed)
        bool newState = !panel.activeSelf;

        // Apply the new visibility to the main inventory panel
        panel.SetActive(newState);

        // Show/hide the bottom UI sections based on this state
        SetExtraUiActive(newState);

        // If the panel has just opened, refresh the displayed items
        if (newState) Redraw();
    }


    public void Open()
    {
        // Show the inventory panel
        panel.SetActive(true);

        // Show the bottom UI sections as well
        SetExtraUiActive(true);

        // Refresh the inventory display immediately
        Redraw();
    }


    public void Close()
    {
        // Hide the inventory panel
        panel.SetActive(false);

        // Hide the bottom UI sections as well
        SetExtraUiActive(false);
    }


    private void Update()
    {
        // If the panel does not exist or is not currently open, ignore input
        if (panel == null || !panel.activeSelf) return;

        // Allow the player to close the inventory using the Escape key
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }



    public void ShowResourcesTab()
    {
        // Switch the active tab/category to Resources
        currentCategory = ItemCategory.Resources;

        // Refresh the UI to display only items from this category
        Redraw();
    }


    public void ShowWineBottlesTab()
    {
        // Switch the active tab/category to Wine Bottles
        currentCategory = ItemCategory.WineBottles;

        // Refresh the UI to display only items from this category
        Redraw();
    }


    public void ShowDesignTab()
    {
        // Switch the active tab/category to Design items
        currentCategory = ItemCategory.Design;

        // Refresh the UI to display only items from this category
        Redraw();
    }

    public void ShowUpdateTab()
    {
        // Switch the active tab/category to Upgrade items
        currentCategory = ItemCategory.Update;

        // Refresh the UI to display only items from this category
        Redraw();
    }

    public void ShowSecurityTab()
    {
        // Switch the active tab/category to Security items
        currentCategory = ItemCategory.Security;

        // Refresh the UI to display only items from this category
        Redraw();
    }


    public void ShowDesignBuyTab()
    {
        // Switch the active tab/category to Design items for purchase
        currentCategory = ItemCategory.DesignBuy;

        // Refresh the UI to display only items from this category
        Redraw();
    }


    void Redraw()
    {
        foreach (Transform c in gridParent)
            Destroy(c.gameObject);

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        int capacity = inv.capacity;


        List<InventorySlot> filtered = new List<InventorySlot>();

        foreach (var s in inv.Slots)
        {
            if (string.IsNullOrEmpty(s.id) || s.amount <= 0)
                continue;

            ItemSO so = inv.GetDefinition(s.id);
            if (so == null)
                continue;

            if (so.category != currentCategory)
                continue;

            filtered.Add(s);
        }


        for (int i = 0; i < capacity; i++)
        {
            var go = Instantiate(slotPrefab, gridParent);

            var imgTr = go.transform.Find("Icon");
            var amountTr = go.transform.Find("Amount");
            var priceTr = go.transform.Find("Price");

            var img = imgTr.GetComponent<Image>();
            var amountTxt = amountTr.GetComponent<TMP_Text>();
            TMP_Text priceTxt = null;
            Image priceIcon = null;
            if (priceTr != null)
            {
                priceTxt = priceTr.GetComponent<TMP_Text>();
                priceIcon = priceTr.GetComponent<Image>();
            }



            if (i < filtered.Count)
            {
                var s = filtered[i];

                ItemSO so = inv.GetDefinition(s.id);
                if (so != null)
                {
                    img.sprite = so.icon;
                    img.enabled = true;
                }
                else
                {
                    img.enabled = false;
                }

                amountTxt.text = s.amount > 1 ? s.amount.ToString() : "";

                if (priceTxt != null || priceIcon != null)
                {
                    if (so != null && so.isWineBottle)
                    {
                        int totalValue = so.price * s.amount;
                        if (priceTxt != null)
                            priceTxt.text = $"₪{totalValue}";


                        if (priceIcon != null)
                            priceIcon.enabled = true;
                        if (priceTr != null)
                            priceTr.gameObject.SetActive(true);
                    }
                    else
                    {

                        if (priceTxt != null)
                            priceTxt.text = "";

                        if (priceIcon != null)
                            priceIcon.enabled = false;
                        if (priceTr != null)
                            priceTr.gameObject.SetActive(false);
                    }
                }

                var click = go.GetComponent<InventorySlotClick>();
                if (click != null)
                {
                    click.itemId = s.id;
                    click.iconImage = img;
                }
            }
            else
            {
                img.enabled = false;
                amountTxt.text = "";


                if (priceTxt != null)
                    priceTxt.text = "";
                if (priceIcon != null)
                    priceIcon.enabled = false;
                if (priceTr != null)
                    priceTr.gameObject.SetActive(false);

                var click = go.GetComponent<InventorySlotClick>();
                if (click != null)
                {
                    click.itemId = "";
                    click.iconImage = img;
                }
            }
        }
    }
}
