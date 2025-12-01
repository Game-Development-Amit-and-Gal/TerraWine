// Assets/Scripts/Inventory/InventoryManager.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("הגדרות תיק")]
    [Min(1)] public int capacity = 20;

    [Header("אירועי UI")]
    public UnityEvent onChanged;
    const string Key = "PROFILE::DEFAULT::INVENTORY";

    private Dictionary<string, ItemSO> catalog = new();
    private List<InventorySlot> slots = new();

    public IReadOnlyList<InventorySlot> Slots => slots;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        foreach (var item in Resources.LoadAll<ItemSO>("Items"))
            if (!catalog.ContainsKey(item.id)) catalog.Add(item.id, item);

        Load();
        onChanged?.Invoke();
    }



    #region API
    public bool Add(string id, int amount = 1)
    {
        if (amount <= 0 || !catalog.ContainsKey(id)) return false;
        var item = catalog[id];

       
        if (item.stackable)
        {
            foreach (var s in slots)
            {
                if (s.id == id && s.amount < item.maxStack)
                {
                    int can = Mathf.Min(item.maxStack - s.amount, amount);
                    s.amount += can;
                    amount -= can;
                    if (amount <= 0) { Save(); onChanged?.Invoke(); return true; }
                }
            }
        }

      
        while (amount > 0 && slots.Count < capacity)
        {
            int put = item.stackable ? Mathf.Min(item.maxStack, amount) : 1;
            slots.Add(new InventorySlot { id = id, amount = put });
            amount -= put;
        }

        Save(); onChanged?.Invoke();
        return amount == 0; 
    }

    public bool Remove(string id, int amount = 1)
    {
        if (amount <= 0) return true;
        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            if (slots[i].id != id) continue;
            int take = Mathf.Min(slots[i].amount, amount);
            slots[i].amount -= take;
            amount -= take;
            if (slots[i].amount <= 0) { slots.RemoveAt(i); i--; }
        }
        Save(); onChanged?.Invoke();
        return amount == 0;
    }

    public int CountOf(string id)
    {
        int c = 0; foreach (var s in slots) if (s.id == id) c += s.amount; return c;
    }
    #endregion

    #region Save/Load
    void Save()
    {
        var data = new InventorySave { capacity = capacity, slots = slots };
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    void Load()
    {
        slots.Clear();

        if (!PlayerPrefs.HasKey(Key))
        {

            capacity = Mathf.Max(1, capacity);
            return;
        }

        var json = PlayerPrefs.GetString(Key);
        var data = JsonUtility.FromJson<InventorySave>(json);

        capacity = Mathf.Max(1, data.capacity);
        slots.AddRange(data.slots ?? new List<InventorySlot>());
    }

    public void ResetAll()
    {
        slots.Clear();
        PlayerPrefs.DeleteKey(Key);
        Save(); onChanged?.Invoke();
    }
    #endregion
}
