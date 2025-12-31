// Assets/Scripts/Inventory/InventoryManager.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
///   This class Is a Singleton based class that manages 
///   the Inventory UI and other resources of the Player's Inventory
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; } // Set the Singleton

    [Header("Bag Settings")]
    [Min(1)] public int capacity = 20; // Set its Capacity

    [Header("Event UI")]
    public UnityEvent onChanged; // Fires when Inventory updates (UI listens to this)
    const string Key = "PROFILE::DEFAULT::INVENTORY"; // Key Used to store inventory JSON in PlayerPrefs

    private Dictionary<string, ItemSO> catalog = new(); // Store all possible item Definitions
                                                        // Key = Item Id, Value = Item 
    private List<InventorySlot> slots = new();         // Player's current inventory

    public IReadOnlyList<InventorySlot> Slots => slots; // Getter method
    public static bool openedBagGardenTutorial = false;

    void Awake() // Ensures Only one Instance Exists
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Load all the Resources/Items
        foreach (var item in Resources.LoadAll<ItemSO>("Items"))
            if (!catalog.ContainsKey(item.id)) catalog.Add(item.id, item);

        Load(); //Loads inventory from saved PlayerPrefs.
        onChanged?.Invoke();  // Updates UI automatically if assigned
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

 
    public ItemSO GetDefinition(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return catalog.TryGetValue(id, out var item) ? item : null;
    }

 
    public List<InventorySlot> GetAllWineBottleSlots()
    {
        List<InventorySlot> result = new List<InventorySlot>();

        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.id) || s.amount <= 0) continue;

            var item = GetDefinition(s.id);
            if (item != null && item.isWineBottle && s.amount > 0)
            {
                result.Add(s);
            }
        }

        return result;
    }
    #endregion

    #region Save/Load
    void Save()  /* Converts inventory data into JSON.
                    Saves it using the key. 
                */
    {
        var data = new InventorySave { capacity = capacity, slots = slots };
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    void Load()
    {
        // Clear any previous runtime data to avoid duplicates
        slots.Clear();
        int one = 1;

        // Check if inventory data was saved before
        // If it doesn't exist, just keep the current capacity (at least 1)
        if (!PlayerPrefs.HasKey(Key))
        {
            capacity = Mathf.Max(one, capacity);  // Ensure minimum capacity = 1
            return; // Nothing to load, exit
        }

        // Retrieve the saved JSON string from PlayerPrefs
        var json = PlayerPrefs.GetString(Key);

        // Convert the JSON back into an InventorySave class instance
        var data = JsonUtility.FromJson<InventorySave>(json);

        // Restore bag capacity (again ensure it’s never lower than 1)
        capacity = Mathf.Max(one, data.capacity);

        // Restore saved slots, if no slots were saved then use an empty list
        slots.AddRange(data.slots ?? new List<InventorySlot>()); // ?? is A Coalescing operator same as -> if (condition) do ... else ..
    }
    public bool AddCategory(ItemCategory category, int amountPerItem = 99)
    {
        bool allAdded = true;

        foreach (var kvp in catalog)
        {
            ItemSO item = kvp.Value;

           
            if (item.category != category)
                continue;

       
            bool ok = Add(item.id, amountPerItem);

        
            if (!ok)
                allAdded = false;
        }

        return allAdded; 
    }





    public void ResetAll()
    {
        slots.Clear();                 // Remove all items from the inventory (empty it)
        PlayerPrefs.DeleteKey(Key);     // Delete the saved inventory data from PlayerPrefs
        Save();                         // Save the now–empty inventory state
        onChanged?.Invoke();            // Notify the UI so it updates immediately
    }

    #endregion
}
