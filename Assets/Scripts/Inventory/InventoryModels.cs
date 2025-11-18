
using System;
using System.Collections.Generic;

[Serializable] public class InventorySlot { public string id; public int amount; }
[Serializable] public class InventorySave { public int capacity = 20; public List<InventorySlot> slots = new(); }
