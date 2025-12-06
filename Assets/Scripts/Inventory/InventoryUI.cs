using System.Collections.Generic;       // ← חשוב
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Transform gridParent;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] GameObject extraImage;
    [SerializeField] GameObject ResourcesBottom;
    [SerializeField] GameObject WineBottlesBottom;
    [SerializeField] GameObject DesignBottom;

    [SerializeField] bool isSellUI = false;
    [SerializeField] TruckSeller truckSeller;

    [SerializeField] bool isBuyUI = false;
    [SerializeField] GameObject UpdateBottom;
    [SerializeField] GameObject SecurityBottom;
    [SerializeField] GameObject DesignBuyBottom;


    [SerializeField] ItemCategory currentCategory = ItemCategory.Resources;

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

        SetExtraUiActive(false);
    }


    private void SetExtraUiActive(bool active)
    {
        if (extraImage != null)
            extraImage.SetActive(active);

      
        if (!isBuyUI)
        {
            if (ResourcesBottom != null)
                ResourcesBottom.SetActive(active);

            if (WineBottlesBottom != null)
                WineBottlesBottom.SetActive(active);

            if (DesignBottom != null)
                DesignBottom.SetActive(active);

            
            if (UpdateBottom != null)
                UpdateBottom.SetActive(false);
            if (SecurityBottom != null)
                SecurityBottom.SetActive(false);
            if (DesignBuyBottom != null)
                DesignBuyBottom.SetActive(false);
        }
        else 
        {
           
            if (ResourcesBottom != null)
                ResourcesBottom.SetActive(false);
            if (WineBottlesBottom != null)
                WineBottlesBottom.SetActive(false);
            if (DesignBottom != null)
                DesignBottom.SetActive(false);

        
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
        bool newState = !panel.activeSelf;
        panel.SetActive(newState);
        SetExtraUiActive(newState);

        if (newState) Redraw();
    }

    public void Open()
    {
        panel.SetActive(true);
        SetExtraUiActive(true);
        Redraw();
    }

    public void Close()
    {
        panel.SetActive(false);
        SetExtraUiActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

  
    public void ShowResourcesTab()
    {
        currentCategory = ItemCategory.Resources;
        Redraw();
    }

    public void ShowWineBottlesTab()
    {
        currentCategory = ItemCategory.WineBottles;
        Redraw();
    }

    public void ShowDesignTab()
    {
        currentCategory = ItemCategory.Design;
        Redraw();
    }
    public void ShowUpdateTab()
    {
        currentCategory = ItemCategory.Update;
        Redraw();
    }

    public void ShowSecurityTab()
    {
        currentCategory = ItemCategory.Security;
        Redraw();
    }

    public void ShowDesignBuyTab()
    {
        currentCategory = ItemCategory.DesignBuy;
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
