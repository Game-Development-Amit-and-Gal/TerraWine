using UnityEngine;

/// <summary>
/// Categories used to organize items in the inventory UI.
/// Each item belongs to one category for filtering and display.
/// </summary>
public enum ItemCategory
{
    Resources,
    WineBottles,
    Design,
    Update,
    Security,
    DesignBuy
}

/// <summary>
/// ScriptableObject that defines all the data for a single item in TerraWine.
/// 
/// This asset describes:
/// - Identity (ID, display name)
/// - Visuals (icon, sprites)
/// - Stack behavior (stackable, max stack size)
/// - Price in shop / selling systems
/// - Type flags (is this item a seed? a wine bottle?)
/// - Seed-specific settings (growth time, harvest result, plot sprites)
/// 
/// The actual game logic (planting, harvesting, selling, etc.)
/// uses these fields to know how to treat each item.
/// </summary>
[CreateAssetMenu(menuName = "TerraWine/Item", fileName = "NewItem")]
public class ItemSO : ScriptableObject
{
    // ───────────────────────────────── Identity ─────────────────────────────────

    /// <summary>
    /// Technical unique ID used by code and save system.
    /// If left empty, it is auto-filled from the asset name in OnValidate.
    /// Example: "CABERNET_SAUVIGNON_SEED".
    /// </summary>
    [Header("Identity")]
    public string id;

    /// <summary>
    /// Name that is shown to the player in UI (inventory, shop, tooltips, etc.).
    /// Example: "Cabernet Sauvignon Seed".
    /// </summary>
    public string displayName;


    // ───────────────────────────── Visuals / Shop ──────────────────────────────

    /// <summary>
    /// Icon used in inventory, shop, hotbar, etc.
    /// </summary>
    [Header("Visuals / Shop")]
    public Sprite icon;

    /// <summary>
    /// If true, multiple copies of this item can be stacked in one inventory slot.
    /// If false, maxStack is forced to 1 (see OnValidate).
    /// </summary>
    public bool stackable = true;

    /// <summary>
    /// Maximum number of this item that can exist in a single stack.
    /// Only meaningful if <see cref="stackable"/> is true.
    /// </summary>
    [Min(1)]
    public int maxStack = 99;

    /// <summary>
    /// Base price of this item in the economy system.
    /// This can be used for buying / selling at shops or with the truck.
    /// </summary>
    [Min(0)]
    public int price = 0;


    // ─────────────────────────────── Type / Logic ──────────────────────────────

    /// <summary>
    /// True if this item represents a seed that can be planted in a plot.
    /// When true, the "Seed Settings" fields are expected to be valid.
    /// </summary>
    [Header("Type (Logic)")]
    public bool isSeed;

    /// <summary>
    /// True if this item represents a wine bottle (finished product).
    /// Can be used for selling, competitions, etc.
    /// </summary>
    public bool isWineBottle;


    // ───────────────────── Inventory Category (UI grouping) ────────────────────

    /// <summary>
    /// Category used to group items into tabs/filters in the inventory UI.
    /// For example: Resources, Wine Bottles, Design items, etc.
    /// </summary>
    [Header("Inventory Category (UI)")]
    public ItemCategory category = ItemCategory.Resources;


    // ────────────────────────────── Seed Settings ──────────────────────────────

    /// <summary>
    /// Only relevant if <see cref="isSeed"/> is true.
    /// Time (in seconds) from planting until the plot becomes ready to harvest.
    /// Example: 180 seconds = 3 in-game minutes.
    /// </summary>
    [Header("Seed Settings")]
    [Tooltip("Only used if this item is a seed.")]
    [Min(1)]
    public float growTimeSeconds = 180f;

    /// <summary>
    /// Item that is produced when harvesting a plot planted with this seed.
    /// Example: a seed item may produce a grape resource item.
    /// </summary>
    [Tooltip("Item given when harvesting a fully grown plot of this seed.")]
    public ItemSO harvestItem;

    /// <summary>
    /// How many items are granted on harvest from a single fully grown plot.
    /// Example: 10 grapes from one mature vine.
    /// </summary>
    [Min(1)]
    public int harvestAmount = 10;

    /// <summary>
    /// Sprite used for the plot while the seed is planted but not yet ready.
    /// This defines how the plot looks during growth.
    /// </summary>
    public Sprite plantedPlotSprite;

    /// <summary>
    /// Sprite used when the plot has finished growing and is ready to harvest.
    /// </summary>
    public Sprite readyPlotSprite;


    // ─────────────────────────────── Validation ────────────────────────────────

    /// <summary>
    /// Called automatically by Unity in the editor whenever a value changes.
    /// 
    /// Responsibilities:
    /// - If <see cref="id"/> is empty or whitespace, it auto-generates an ID
    ///   from the asset name in UPPER_SNAKE_CASE (spaces replaced by '_').
    /// - If <see cref="stackable"/> is false, it forces <see cref="maxStack"/> to 1
    ///   to avoid invalid combinations (non-stackable items with stack count > 1).
    /// 
    /// This helps keep data consistent without requiring manual cleanup.
    /// </summary>
    private void OnValidate()
    {
        // Auto-generate ID from asset name if not provided
        if (string.IsNullOrWhiteSpace(id))
            id = name.ToUpper().Replace(' ', '_');

        // Ensure non-stackable items cannot have a maxStack > 1
        if (!stackable)
            maxStack = 1;
    }
}
