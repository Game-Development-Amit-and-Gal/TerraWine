using System;
using System.Collections.Generic;

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
    public int wineScore = 0;

    // NOTE: keeping your original field name, but if it's a typo you can rename it safely
    public int hineScore = 0;

    public long lastRealTimeTicks;    // Timestamp for real-time progression (offline growth)

    // Daily actions (reset every day at 08:00)
    public int dailyActionsUsed = 0;      // כמה פעולות נוצלו מאז הריסט האחרון
    public long dailyActionsResetTicks = 0;

    public int waterCurrent = 20;
    public int waterMax = 20;

    public long waterLastUpdateTicks = 0;      // UTC ticks
    public int waterGrowingCountSnapshot = 0;  // כמה עציצים גדלו בעת העדכון האחרון
    public float waterDrainRemainder = 0f;     // צבירת שברים לירידה חלקה

    // ---------------- SEASONS / CALENDAR ----------------
    public int calendarYear = 1;        // 1..3 (או בלי הגבלה אם תרצי)
    public int calendarSeasonIndex = 0; // 0=Earth, 1=Vine, 2=Winery
    public int calendarDay = 1;         // 1..15
    public long calendarLastUpdateTicks = 0; // UTC ticks לחישוב זמן שעבר (אופציונלי)

    // ---------------- RAID / SECURITY ----------------
    public int securityLevel = 0;                 // רמת אבטחה (שדרוגים)
    public long lastRaidTicks = 0;                // מתי היה Raid אחרון (UTC ticks)
    public List<string> stolenRecipeIds = new();  // מתכונים שנגנבו (כדי להחזיר בעתיד)
    public List<string> raidLog = new();          // יומן אירועים (אופציונלי)

    // ---------------- INVENTORY / BARRELS ----------------
    public List<InventoryItem> inventory = new();          // All items in player's inventory
    public List<OwnedBarrelData> ownedBarrels = new();     // All cellar barrels the player owns
    public List<string> unlockedRecipeIds = new();
    public List<BarrelAgingSave> barrelAging = new();

    // ---------------- TUTORIAL (OLD FLAGS - keep if you still use them) ----------------
    public bool tutorialCompleted;       // General tutorial finished?
    public bool sampleSceneGuideDone;    // Intro scene guide completed?
    public bool worldMapGuideDone;       // World map guide completed?
    public bool wineGuideDone;
    public bool basementGuideDone;
    public bool wineryReceptionGuideDone;

    // ---------------- TUTORIAL (NEW: multi tutorials per scene + ordered via flags) ----------------
    // Each key is: "SceneName::TutorialId"  (example: "SampleScene::Intro", "SampleScene::BuildBed")
    public List<string> completedTutorialKeys = new();

    // Persistent flags that survive save/load (use for "when should tutorial #2 appear?")
    public List<string> tutorialPersistentFlags = new();
}

/// <summary>
/// Represents a barrel owned by the player.
/// </summary>
[Serializable]
public class OwnedBarrelData
{
    public string id;        // Barrel type identifier (matches ScriptableObject barrel ID)
    public bool isPremium;   // True if the barrel is a premium/high-quality one
}

[Serializable]
public class BarrelAgingSave
{
    public string barrelId;

    public bool isAging;
    public bool isReady;

    public string recipeId;
    public WineDryness dryness;

    public long agingStartTicks;
    public long agingEndTicks;

    public string bottleItemId;
    public int bottleAmount;
}
