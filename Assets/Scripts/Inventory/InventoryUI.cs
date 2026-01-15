using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject slotPrefab;

    [SerializeField] private GameObject extraImage;

    // MAIN tabs (Selected versions)
    [Header("Main Tabs - Selected")]
    [SerializeField] private GameObject ResourcesBottom;
    [SerializeField] private GameObject WineBottlesBottom;
    [SerializeField] private GameObject DesignBottom;

    // MAIN tabs (Unselected versions)
    [Header("Main Tabs - Unselected")]
    [SerializeField] private GameObject ResourcesBottom_Unselected;
    [SerializeField] private GameObject WineBottlesBottom_Unselected;
    [SerializeField] private GameObject DesignBottom_Unselected;

    [SerializeField] private bool isSellUI = false;
    [SerializeField] private TruckSeller truckSeller;

    [SerializeField] private bool isBuyUI = false;

    // BUY tabs (Selected versions)
    [Header("Buy Tabs - Selected")]
    [SerializeField] private GameObject UpdateBottom;
    [SerializeField] private GameObject SecurityBottom;
    [SerializeField] private GameObject DesignBuyBottom;

    // BUY tabs (Unselected versions)
    [Header("Buy Tabs - Unselected")]
    [SerializeField] private GameObject UpdateBottom_Unselected;
    [SerializeField] private GameObject SecurityBottom_Unselected;
    [SerializeField] private GameObject DesignBuyBottom_Unselected;

    [SerializeField] private ItemCategory currentCategory = ItemCategory.Resources;

    [Header("Paging")]
    [SerializeField, Min(1)] private int itemsPerPage = 4;
    [SerializeField] private GameObject nextPageButton;
    [SerializeField] private GameObject prevPageButton;
    [SerializeField] private TMP_Text pageLabel;

    private int currentPage = 0;

    private void OnEnable()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] No InventoryManager in scene!");
            return;
        }

        InventoryManager.Instance.onChanged.AddListener(Redraw);
        Redraw();

        // ensure correct tab visuals if opened/enabled while panel already active
        if (panel != null && panel.activeInHierarchy)
        {
            SetExtraUiActive(true);
            RefreshTabSelection();
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance == null) return;

        InventoryManager.Instance.onChanged.RemoveListener(Redraw);
        InventoryTooltipUI.Instance?.Hide();
        SetExtraUiActive(false);
    }

    private void SetExtraUiActive(bool active)
    {
        if (extraImage != null)
            extraImage.SetActive(active);

        if (!active)
        {
            SetAllTabsVisible(false);
            return;
        }

        // Show only the relevant set (Main vs Buy)
        if (!isBuyUI)
        {
            SetMainTabsVisible(true);
            SetBuyTabsVisible(false);

            // bag open tutorial only when opening (same behavior you had)
            TutorialManager.Instance?.SetFlag("Bag Open");
        }
        else
        {
            SetMainTabsVisible(false);
            SetBuyTabsVisible(true);
        }

        RefreshTabSelection();
    }

    // --- Tabs visibility helpers (turn sets ON/OFF) ---

    private void SetAllTabsVisible(bool visible)
    {
        // MAIN selected
        if (ResourcesBottom != null) ResourcesBottom.SetActive(false);
        if (WineBottlesBottom != null) WineBottlesBottom.SetActive(false);
        if (DesignBottom != null) DesignBottom.SetActive(false);

        // MAIN unselected
        if (ResourcesBottom_Unselected != null) ResourcesBottom_Unselected.SetActive(false);
        if (WineBottlesBottom_Unselected != null) WineBottlesBottom_Unselected.SetActive(false);
        if (DesignBottom_Unselected != null) DesignBottom_Unselected.SetActive(false);

        // BUY selected
        if (UpdateBottom != null) UpdateBottom.SetActive(false);
        if (SecurityBottom != null) SecurityBottom.SetActive(false);
        if (DesignBuyBottom != null) DesignBuyBottom.SetActive(false);

        // BUY unselected
        if (UpdateBottom_Unselected != null) UpdateBottom_Unselected.SetActive(false);
        if (SecurityBottom_Unselected != null) SecurityBottom_Unselected.SetActive(false);
        if (DesignBuyBottom_Unselected != null) DesignBuyBottom_Unselected.SetActive(false);
    }

    private void SetMainTabsVisible(bool visible)
    {
        if (!visible)
        {
            if (ResourcesBottom != null) ResourcesBottom.SetActive(false);
            if (WineBottlesBottom != null) WineBottlesBottom.SetActive(false);
            if (DesignBottom != null) DesignBottom.SetActive(false);

            if (ResourcesBottom_Unselected != null) ResourcesBottom_Unselected.SetActive(false);
            if (WineBottlesBottom_Unselected != null) WineBottlesBottom_Unselected.SetActive(false);
            if (DesignBottom_Unselected != null) DesignBottom_Unselected.SetActive(false);
            return;
        }

        // When visible, we will control which one is selected via RefreshTabSelection().
        // So we can just enable the "unselected" as a baseline and hide selected for now.
        if (ResourcesBottom_Unselected != null) ResourcesBottom_Unselected.SetActive(true);
        if (WineBottlesBottom_Unselected != null) WineBottlesBottom_Unselected.SetActive(true);
        if (DesignBottom_Unselected != null) DesignBottom_Unselected.SetActive(true);

        if (ResourcesBottom != null) ResourcesBottom.SetActive(false);
        if (WineBottlesBottom != null) WineBottlesBottom.SetActive(false);
        if (DesignBottom != null) DesignBottom.SetActive(false);
    }

    private void SetBuyTabsVisible(bool visible)
    {
        if (!visible)
        {
            if (UpdateBottom != null) UpdateBottom.SetActive(false);
            if (SecurityBottom != null) SecurityBottom.SetActive(false);
            if (DesignBuyBottom != null) DesignBuyBottom.SetActive(false);

            if (UpdateBottom_Unselected != null) UpdateBottom_Unselected.SetActive(false);
            if (SecurityBottom_Unselected != null) SecurityBottom_Unselected.SetActive(false);
            if (DesignBuyBottom_Unselected != null) DesignBuyBottom_Unselected.SetActive(false);
            return;
        }

        if (UpdateBottom_Unselected != null) UpdateBottom_Unselected.SetActive(true);
        if (SecurityBottom_Unselected != null) SecurityBottom_Unselected.SetActive(true);
        if (DesignBuyBottom_Unselected != null) DesignBuyBottom_Unselected.SetActive(true);

        if (UpdateBottom != null) UpdateBottom.SetActive(false);
        if (SecurityBottom != null) SecurityBottom.SetActive(false);
        if (DesignBuyBottom != null) DesignBuyBottom.SetActive(false);
    }

    // --- Tab selection visuals ---

    private void RefreshTabSelection()
    {
        if (!isBuyUI)
            RefreshTabSelection_Main();
        else
            RefreshTabSelection_Buy();
    }

    private void RefreshTabSelection_Main()
    {
        bool res = currentCategory == ItemCategory.Resources;
        bool wine = currentCategory == ItemCategory.WineBottles;
        bool design = currentCategory == ItemCategory.Design;

        if (ResourcesBottom != null) ResourcesBottom.SetActive(res);
        if (ResourcesBottom_Unselected != null) ResourcesBottom_Unselected.SetActive(!res);

        if (WineBottlesBottom != null) WineBottlesBottom.SetActive(wine);
        if (WineBottlesBottom_Unselected != null) WineBottlesBottom_Unselected.SetActive(!wine);

        if (DesignBottom != null) DesignBottom.SetActive(design);
        if (DesignBottom_Unselected != null) DesignBottom_Unselected.SetActive(!design);
    }

    private void RefreshTabSelection_Buy()
    {
        bool upd = currentCategory == ItemCategory.Update;
        bool sec = currentCategory == ItemCategory.Security;
        bool designBuy = currentCategory == ItemCategory.DesignBuy;

        if (UpdateBottom != null) UpdateBottom.SetActive(upd);
        if (UpdateBottom_Unselected != null) UpdateBottom_Unselected.SetActive(!upd);

        if (SecurityBottom != null) SecurityBottom.SetActive(sec);
        if (SecurityBottom_Unselected != null) SecurityBottom_Unselected.SetActive(!sec);

        if (DesignBuyBottom != null) DesignBuyBottom.SetActive(designBuy);
        if (DesignBuyBottom_Unselected != null) DesignBuyBottom_Unselected.SetActive(!designBuy);
    }

    public void Toggle()
    {
        InventoryTooltipUI.Instance?.Hide();
        if (panel == null) return;

        bool newState = !panel.activeSelf;

        SetOpenBagTutorial(newState, panel);

        panel.SetActive(newState);
        SetExtraUiActive(newState);

        if (newState)
        {
            currentPage = 0;
            Redraw();
            RefreshTabSelection();
        }
        else
        {
            SetPagingUiActive(false);
        }
    }

    public void Open()
    {
        if (panel == null) return;

        panel.SetActive(true);
        SetExtraUiActive(true);
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void Close()
    {
        if (panel == null) return;

        panel.SetActive(false);
        TutorialManager.Instance?.SetFlag("Bag Close");
        InventoryTooltipUI.Instance?.Hide();
        SetExtraUiActive(false);

        SetPagingUiActive(false);
        SetOpenBagTutorial(false, panel);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // Tabs
    public void ShowResourcesTab()
    {
        currentCategory = ItemCategory.Resources;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void ShowWineBottlesTab()
    {
        currentCategory = ItemCategory.WineBottles;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void ShowDesignTab()
    {
        currentCategory = ItemCategory.Design;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void ShowUpdateTab()
    {
        currentCategory = ItemCategory.Update;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void ShowSecurityTab()
    {
        currentCategory = ItemCategory.Security;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    public void ShowDesignBuyTab()
    {
        currentCategory = ItemCategory.DesignBuy;
        currentPage = 0;
        Redraw();
        RefreshTabSelection();
    }

    // Paging
    public void NextPage()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        int total = CountFiltered(inv);
        int maxPage = total > 0 ? (total - 1) / itemsPerPage : 0;

        if (currentPage < maxPage)
        {
            currentPage++;
            Redraw();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            Redraw();
        }
    }

    // Draw
    private void Redraw()
    {
        if (panel == null || !panel.activeInHierarchy)
        {
            SetPagingUiActive(false);
            return;
        }

        if (gridParent == null || slotPrefab == null)
        {
            Debug.LogWarning("[InventoryUI] gridParent/slotPrefab missing!");
            return;
        }

        foreach (Transform c in gridParent)
            Destroy(c.gameObject);

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        List<InventorySlot> filtered = new List<InventorySlot>();
        foreach (var s in inv.Slots)
        {
            if (string.IsNullOrEmpty(s.id) || s.amount <= 0) continue;

            ItemSO so = inv.GetDefinition(s.id);
            if (so == null) continue;

            if (so.category != currentCategory) continue;
            filtered.Add(s);
        }

        int total = filtered.Count;
        if (total <= 0)
        {
            SetPagingUiActive(false);
            return;
        }

        int maxPage = (total - 1) / itemsPerPage;
        currentPage = Mathf.Clamp(currentPage, 0, maxPage);

        if (prevPageButton != null) prevPageButton.SetActive(currentPage > 0);
        if (nextPageButton != null) nextPageButton.SetActive(currentPage < maxPage);

        if (pageLabel != null)
        {
            pageLabel.enabled = true;
            pageLabel.gameObject.SetActive(true);

            var cg = pageLabel.GetComponentInParent<CanvasGroup>();
            if (cg != null && cg.alpha <= 0f) cg.alpha = 1f;

            int shownMax = Mathf.Max(1, maxPage + 1);
            pageLabel.text = $"{(currentPage + 1)}/{shownMax}";
        }

        int start = currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, total);

        for (int i = start; i < end; i++)
        {
            var slot = filtered[i];
            ItemSO so = inv.GetDefinition(slot.id);
            if (so == null) continue;

            var go = Instantiate(slotPrefab, gridParent);

            var imgTr = go.transform.Find("Icon");
            var amountTr = go.transform.Find("Amount");
            var priceTr = go.transform.Find("Price");
            var nameTr = go.transform.Find("Name");
            var timeTr = go.transform.Find("ReadyTime");

            // Plant button
            var plantBtnTr = go.transform.Find("PlantButton");
            var plantBtn = plantBtnTr != null ? plantBtnTr.GetComponent<Button>() : null;

            var img = imgTr != null ? imgTr.GetComponent<Image>() : null;
            var amountTxt = amountTr != null ? amountTr.GetComponent<TMP_Text>() : null;

            TMP_Text priceTxt = null;
            Image priceIcon = null;
            if (priceTr != null)
            {
                priceTxt = priceTr.GetComponent<TMP_Text>();
                if (priceTxt == null) priceTxt = priceTr.GetComponentInChildren<TMP_Text>(true);

                priceIcon = priceTr.GetComponent<Image>();
                if (priceIcon == null) priceIcon = priceTr.GetComponentInChildren<Image>(true);
            }

            var nameTxt = nameTr != null ? nameTr.GetComponent<TMP_Text>() : null;
            var timeTxt = timeTr != null ? timeTr.GetComponent<TMP_Text>() : null;

            // icon
            if (img != null)
            {
                if (so.icon != null)
                {
                    img.sprite = so.icon;
                    img.enabled = true;
                }
                else
                {
                    img.sprite = null;
                    img.enabled = false;
                }
            }

            // amount
            if (amountTxt != null)
                amountTxt.text = slot.amount > 1 ? slot.amount.ToString() : "";

            // NAME
            if (nameTxt != null)
            {
                string nameToShow = !string.IsNullOrWhiteSpace(so.displayName) ? so.displayName : so.id;
                nameTxt.text = nameToShow;
                nameTxt.gameObject.SetActive(true);
            }

            // READY TIME (Seeds only)
            if (timeTxt != null)
            {
                if (so.isSeed)
                {
                    timeTxt.text = $"Grow Time: {FormatTime(so.growTimeSeconds)}";
                    timeTxt.gameObject.SetActive(true);
                }
                else
                {
                    timeTxt.text = "";
                    timeTxt.gameObject.SetActive(false);
                }
            }

            // price (wine bottles only)
            if (priceTr != null)
            {
                bool showPrice = so.isWineBottle;

                if (showPrice)
                {
                    int totalValue = so.price * slot.amount;
                    if (priceTxt != null) priceTxt.text = totalValue.ToString();
                    if (priceIcon != null) priceIcon.enabled = true;
                    priceTr.gameObject.SetActive(true);
                }
                else
                {
                    if (priceTxt != null) priceTxt.text = "";
                    if (priceIcon != null) priceIcon.enabled = false;
                    priceTr.gameObject.SetActive(false);
                }
            }

            // keep tooltip script (but selection will be via PlantButton)
            var click = go.GetComponent<InventorySlotClick>();
            if (click != null)
            {
                click.itemId = slot.id;
                click.iconImage = img;
            }

            // PlantButton behavior (Seeds only)
            if (plantBtn != null)
            {
                bool canPlant = so.isSeed && !isSellUI && !isBuyUI;

                plantBtn.gameObject.SetActive(canPlant);
                plantBtn.onClick.RemoveAllListeners();

                if (canPlant)
                {
                    string seedId = slot.id; // capture safe

                    plantBtn.onClick.AddListener(() =>
                    {
                        var inv2 = InventoryManager.Instance;
                        if (inv2 == null) return;

                        var seedSo = inv2.GetDefinition(seedId);
                        if (seedSo == null || !seedSo.isSeed) return;

                        TutorialManager.Instance?.SetFlag("Press Seed");
                        PlantingController.Instance?.SelectSeed(seedSo);

                        Close();
                    });
                }
            }
        }
    }

    private int CountFiltered(InventoryManager inv)
    {
        int count = 0;
        foreach (var s in inv.Slots)
        {
            if (string.IsNullOrEmpty(s.id) || s.amount <= 0) continue;

            ItemSO so = inv.GetDefinition(s.id);
            if (so == null) continue;

            if (so.category != currentCategory) continue;
            count++;
        }
        return count;
    }

    private void SetPagingUiActive(bool active)
    {
        if (prevPageButton != null) prevPageButton.SetActive(active && currentPage > 0);
        if (nextPageButton != null) nextPageButton.SetActive(active);
        if (pageLabel != null) pageLabel.gameObject.SetActive(active);
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int s = Mathf.CeilToInt(seconds);
        int m = s / 60;
        s %= 60;

        return $"{m:0}:{s:00}";
    }

    public static void SetOpenBagTutorial(bool openBagTutorial, GameObject panelObj)
    {
        if (panelObj != null && panelObj.CompareTag("BAG"))
        {
            bool prev = InventoryManager.openedBagGardenTutorial;
            InventoryManager.openedBagGardenTutorial = openBagTutorial;

            Debug.Log($"Value of BAG changed from {prev} to {openBagTutorial}");
        }
    }
}
