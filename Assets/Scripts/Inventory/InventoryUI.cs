using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;  

public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject panel;      
    [SerializeField] Transform gridParent;  
    [SerializeField] GameObject slotPrefab; 

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
    }

    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) Redraw();
    }

    public void Open()
    {
        panel.SetActive(true);
        Redraw();
    }

    public void Close()
    {
        panel.SetActive(false);
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


    void Redraw()
    {
        foreach (Transform c in gridParent)
            Destroy(c.gameObject);

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        int capacity = inv.capacity;

        for (int i = 0; i < capacity; i++)
        {
            var go = Instantiate(slotPrefab, gridParent);
            var imgTr = go.transform.Find("Icon");
            var txtTr = go.transform.Find("Amount");

            var img = imgTr.GetComponent<Image>();
            var txt = txtTr.GetComponent<TMP_Text>();

            if (i < inv.Slots.Count)
            {
                var s = inv.Slots[i];
                var so = Resources.Load<ItemSO>($"Items/{s.id}");
                if (so != null)
                {
                    img.sprite = so.icon;
                    img.enabled = true;
                }
                else
                {
                    img.enabled = false;
                }

                txt.text = s.amount > 1 ? s.amount.ToString() : "";

                // <<< חיבור InventorySlotClick >>>
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
                txt.text = "";

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
