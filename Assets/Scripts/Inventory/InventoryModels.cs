using System;
using System.Collections.Generic;

/// <summary>
/// Represents one stack of an item in the inventory.
/// Example: id = "wine_red", amount = 3
/// </summary>
[Serializable]
public class InventorySlot
{
    public string id;      // The item’s unique ID (matches ItemSO.id)
    public int amount;     // How many of this item are in the slot
}

/// <summary>
/// Data container used for saving/loading the entire inventory.
/// This is converted to/from JSON using JsonUtility.
/// </summary>
[Serializable]
public class InventorySave
{
    public int capacity = 20;                      // Maximum number of slots the bag can hold
    public List<InventorySlot> slots = new();      // All items currently stored in the inventory
}
