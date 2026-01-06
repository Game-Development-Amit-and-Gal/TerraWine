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
    [SerializeField] private GameObject ResourcesBottom;
    [SerializeField] private GameObject WineBottlesBottom;
    [SerializeField] private GameObject DesignBottom;

    [SerializeField] private bool isSellUI = false;
    [SerializeField] private TruckSeller truckSeller;

    [SerializeField] private bool isBuyUI = false;
    [SerializeField] private GameObject UpdateBottom;
    [SerializeField] private GameObject SecurityBottom;
    [SerializeField] private GameObject DesignBuyBottom;

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

        if (!isBuyUI)
        {
            if (ResourcesBottom != null)
            {
                if (active) TutorialManager.Instance?.SetFlag("Bag Open");
                ResourcesBottom.SetActive(active);
            }

            if (WineBottlesBottom != null)
                WineBottlesBottom.SetActive(active);

            if (DesignBottom != null)
                DesignBottom.SetActive(active);

            if (UpdateBottom != null) UpdateBottom.SetActive(false);
            if (SecurityBottom != null) SecurityBottom.SetActive(false);
            if (DesignBuyBottom != null) DesignBuyBottom.SetActive(false);
        }
        else
        {
            if (ResourcesBottom != null) ResourcesBottom.SetActive(false);
            if (WineBottlesBottom != null) WineBottlesBottom.SetActive(false);
            if (DesignBottom != null) DesignBottom.SetActive(false);

            if (UpdateBottom != null) UpdateBottom.SetActive(active);
            if (SecurityBottom != null) SecurityBottom.SetActive(active);
            if (DesignBuyBottom != null) DesignBuyBottom.SetActive(active);
        }
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
    public void ShowResourcesTab() { currentCategory = ItemCategory.Resources; currentPage = 0; Redraw(); }
    public void ShowWineBottlesTab() { currentCategory = ItemCategory.WineBottles; currentPage = 0; Redraw(); }
    public void ShowDesignTab() { currentCategory = ItemCategory.Design; currentPage = 0; Redraw(); }
    public void ShowUpdateTab() { currentCategory = ItemCategory.Update; currentPage = 0; Redraw(); }
    public void ShowSecurityTab() { currentCategory = ItemCategory.Security; currentPage = 0; Redraw(); }
    public void ShowDesignBuyTab() { currentCategory = ItemCategory.DesignBuy; currentPage = 0; Redraw(); }

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

            // NEW: Plant button
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

            // NEW: PlantButton behavior (Seeds only)
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
