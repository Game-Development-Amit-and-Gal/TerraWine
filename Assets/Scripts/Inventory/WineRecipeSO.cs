using System;
using System.Collections.Generic;
using UnityEngine;

public enum WineDryness
{
    SemiDry,
    Dry
}

[Serializable]
public struct ItemAmount
{
    [Tooltip("Ingredient item name or ID (e.g., CABERNET_GRAPES).")]
    public string itemName;

    [Tooltip("How many units of this item are required/produced.")]
    [Min(1)]
    public int amount;
}

[Serializable]
public struct WineOutput
{
    [Tooltip("Aging time in seconds for this dryness.")]
    [Min(0f)]
    public float timeSeconds;

    [Tooltip("Bottle item produced for this dryness (drag an ItemSO bottle).")]
    public ItemSO bottleItem;

    [Tooltip("How many bottles are produced.")]
    [Min(1)]
    public int bottleAmount;

    [Tooltip("Wine rating shown to the player (1-10).")]
    [Range(1, 10)]
    public int rating;
}

[CreateAssetMenu(menuName = "TerraWine/Wine Recipe", fileName = "NewWineRecipe")]
public class WineRecipeSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique technical ID used by code/save. Auto-filled from asset name if empty.")]
    public string id;

    [Tooltip("Wine name shown in UI (e.g., Cabernet Sauvignon).")]
    public string wineName;

    // ------------------------------
    //  Barrel (DO NOT TOUCH)
    // ------------------------------

    [Header("Barrel")]
    [Tooltip("Drag the barrel prefab (or any GameObject reference you use to represent a barrel).")]
    public GameObject barrelPrefab;

    [Tooltip("Optional: barrel icon for UI (if you don't use a prefab here).")]
    public Sprite barrelIcon;

    // ------------------------------
    //  Ingredients (NAME + AMOUNT)
    // ------------------------------

    [Header("Grapes (Ingredients)")]
    [Tooltip("Add grapes item name + amounts required for this recipe.")]
    public List<ItemAmount> grapes = new List<ItemAmount>();

    [Header("Outputs (Semi-Dry)")]
    public WineOutput semiDry;

    [Header("Outputs (Dry)")]
    public WineOutput dry;

    [Header("Defaults")]
    [SerializeField, Min(1)] private int defaultBottleAmount = 1;
    [SerializeField, Range(1, 10)] private int defaultRating = 5;
    [SerializeField, Min(1)] private int defaultGrapeAmount = 1;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name.ToUpperInvariant().Replace(' ', '_');

        if (grapes == null)
            grapes = new List<ItemAmount>();

        // IMPORTANT:
        // Do NOT remove empty entries here, otherwise Unity '+' creates an empty row
        // and OnValidate deletes it immediately (looks like you "can't add grapes").
        for (int i = 0; i < grapes.Count; i++)
        {
            ItemAmount ia = grapes[i];

            // Keep editable, just sanitize
            if (!string.IsNullOrWhiteSpace(ia.itemName))
                ia.itemName = ia.itemName.Trim();

            if (ia.amount < 1)
                ia.amount = defaultGrapeAmount;

            grapes[i] = ia;
        }

        // Ensure valid bottle amounts
        if (semiDry.bottleAmount < 1) semiDry.bottleAmount = defaultBottleAmount;
        if (dry.bottleAmount < 1) dry.bottleAmount = defaultBottleAmount;

        // Ensure non-negative times
        if (semiDry.timeSeconds < 0f) semiDry.timeSeconds = 0f;
        if (dry.timeSeconds < 0f) dry.timeSeconds = 0f;

        // Ensure rating range
        if (semiDry.rating == 0) semiDry.rating = defaultRating;
        if (dry.rating == 0) dry.rating = defaultRating;

        semiDry.rating = Mathf.Clamp(semiDry.rating, 1, 10);
        dry.rating = Mathf.Clamp(dry.rating, 1, 10);
    }

    // Optional: manually clean empty ingredient names when YOU want
    [ContextMenu("Clean Empty Grape Names")]
    private void CleanEmptyGrapeNames()
    {
        for (int i = grapes.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(grapes[i].itemName))
                grapes.RemoveAt(i);
        }
    }

    public WineOutput GetOutput(WineDryness dryness)
    {
        return dryness == WineDryness.SemiDry ? semiDry : dry;
    }
}
