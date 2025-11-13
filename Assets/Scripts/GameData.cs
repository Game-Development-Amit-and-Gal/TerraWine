using System;
using System.Collections.Generic;

[Serializable]
public class InventoryItem { public string id; public int amount; }

[Serializable]
public class GameData
{
    public string sceneName;
    public float playerX, playerY;
    public int money;
    public int season = 1;
    public List<InventoryItem> inventory = new List<InventoryItem>();
}
