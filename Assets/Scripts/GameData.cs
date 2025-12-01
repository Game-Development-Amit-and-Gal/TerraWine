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
    public long lastRealTimeTicks;
    public List<InventoryItem> inventory = new List<InventoryItem>();
    public List<OwnedBarrelData> ownedBarrels = new List<OwnedBarrelData>();
}
[System.Serializable]
public class OwnedBarrelData
{
    public string id;
    public bool isPremium;
}
