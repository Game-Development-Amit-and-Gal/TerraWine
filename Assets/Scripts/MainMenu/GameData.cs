using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Represents an item stored in the player's inventory.
/// Uses a unique ID string and a quantity value.
/// </summary>
[Serializable]
public class InventoryItem
{
    public string id;      // Unique item identifier (matches ScriptableObject item)
    public int amount;     // How many of this item the player owns
}


/// <summary>
/// Main save file structure for the game.
/// Stores scene, player state, inventory contents, tutorial status, etc.
/// Serialized into JSON when saving.
/// </summary>
[Serializable]
public class GameData
{
    public string sceneName;          // Name of the last scene played (used when loading)
    public float playerX, playerY;    // Saved player position in world space
    public int money;                 // Player's current money amount
    public int season = 1;            // Current in-game season (default: 1)

    public long lastRealTimeTicks;    // Timestamp for real-time progression (offline growth)

    // Collections of owned items and barrels
    public List<InventoryItem> inventory = new List<InventoryItem>();  // All items in player's inventory
    public List<OwnedBarrelData> ownedBarrels = new List<OwnedBarrelData>(); // All cellar barrels the player owns

    // Tutorial progress flags
    public bool tutorialCompleted;       // General tutorial finished?
    public bool sampleSceneGuideDone;    // Intro scene guide completed?
    public bool worldMapGuideDone;       // World map guide completed?
    public bool cellarGuideDone;         // Cellar usage guide completed?
}


/// <summary>
/// Represents a barrel owned by the player.
/// </summary>
[System.Serializable]
public class OwnedBarrelData
{
    public string id;        // Barrel type identifier (matches ScriptableObject barrel ID)
    public bool isPremium;   // True if the barrel is a premium/high-quality one
}
