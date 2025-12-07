// Assets/Scripts/Barrels/Barrel.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles barrel behavior: starting aging (wine production), finishing it,
/// and harvesting bottles when ready.
/// Works together with BarrelUI for player interactions.
/// </summary>
public class Barrel : MonoBehaviour, IPointerClickHandler
{
    // ---------------------- Identity ----------------------
    [Header("Identity")]
    [SerializeField] private string barrelId = "BARREL_1"; // Unique ID (useful for saving if you have multiple barrels)

    // ---------------------- Item IDs ----------------------
    [Header("Items")]
    [SerializeField] private string grapeItemId = "Cabernet_Sauvignon_Grap";      // Inventory ID for grapes
    [SerializeField] private string bottleItemId = "Cabernet_Sauvignon_Bottle";  // Inventory ID for resulting wine bottle
    [SerializeField] private int grapesPerBottle = 5;                             // Grapes required per bottle

    // ---------------------- Wine Aging Data ----------------------
    [Header("Aging Times (seconds)")]
    [SerializeField] private float semiDrySeconds = 20f;  // Example: 20 seconds (representing 2 minutes)
    [SerializeField] private float drySeconds = 300f;     // Example: 300 seconds (5 minutes)

    // ---------------------- UI Reference ----------------------
    [Header("UI")]
    [SerializeField] private BarrelUI ui; // UI panel for selecting amount and wine type

    // ---------------------- Barrel State ----------------------
    int grapesInside = 0;      // Grapes currently inside barrel
    bool isAging = false;      // TRUE while aging is in progress
    bool isReady = false;      // TRUE when wine is done aging
    float remainingSeconds = 0f;
    float totalSeconds = 0f;

    // Reset values to clear the barrel
    float resetSeconds = 0f;
    int resetGrapesInside = 0;
    bool resetIsAgingAndReady = false;

    bool isDry = false; // TRUE = Dry wine, FALSE = Semi-Dry

    // ---------------------- Public Properties ----------------------
    public int GrapesPerBottle => grapesPerBottle;
    public float SemiDrySeconds => semiDrySeconds;
    public float DrySeconds => drySeconds;

    /// <summary>
    /// Handles clicking on the barrel in the scene:
    /// - If empty → open UI to choose grapes & wine type
    /// - If wine ready → harvest bottles
    /// - If still aging → show debug message
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only react to left mouse button
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // If barrel is empty and not aging → open UI
        if (!isAging && !isReady)
        {
            if (InventoryManager.Instance == null || ui == null) return;

            // Count available grapes in player's inventory
            int grapesInBag = InventoryManager.Instance.CountOf(grapeItemId);

            // Open UI for selection
            ui.OpenForBarrel(this, grapesInBag);
        }
        // If wine is ready → collect bottles
        else if (isReady)
        {
            HarvestBottles();
        }
        else
        {
            // Still aging → not ready
            Debug.Log("[Barrel] Wine is still aging...");
        }
    }

    /// <summary>
    /// Called by BarrelUI after the player chooses amount of grapes and wine type.
    /// Starts the aging process.
    /// </summary>
    public void StartAging(int grapesToUse, bool makeDry)
    {
        if (isAging || isReady) return;                 // Prevent restarting aging
        if (InventoryManager.Instance == null) return;  // Safety check

        // Only allow multiples of grapesPerBottle
        int usableGrapes = (grapesToUse / grapesPerBottle) * grapesPerBottle;
        if (usableGrapes <= 0) return;

        // Check inventory amount
        int inBag = InventoryManager.Instance.CountOf(grapeItemId);
        if (inBag < usableGrapes)
        {
            Debug.LogWarning($"[Barrel] Not enough grapes. Have {inBag}, tried to use {usableGrapes}");
            return;
        }

        // Remove grapes from inventory
        bool removed = InventoryManager.Instance.Remove(grapeItemId, usableGrapes);
        if (!removed)
        {
            Debug.LogWarning("[Barrel] Failed to remove grapes from inventory");
            return;
        }

        // Store grapes and set wine type
        grapesInside = usableGrapes;
        isDry = makeDry;

        // Choose proper aging time
        totalSeconds = makeDry ? drySeconds : semiDrySeconds;
        remainingSeconds = totalSeconds;

        // Set state to start aging
        isAging = true;
        isReady = false;

        // Stop any old routines and start a new one
        StopAllCoroutines();
        StartCoroutine(AgingRoutine());

        Debug.Log($"[Barrel] Started aging {grapesInside} grapes for {(makeDry ? "dry" : "semi-dry")} wine. Time={totalSeconds}s");
    }

    /// <summary>
    /// Coroutine that simulates wine aging over time.
    /// </summary>
    System.Collections.IEnumerator AgingRoutine()
    {
        // Decrease time every frame until done
        while (remainingSeconds > resetSeconds && isAging)
        {
            remainingSeconds -= Time.deltaTime;
            // Optional: update UI timer here
            yield return null;
        }

        if (!isAging) yield break;

        // Wine finished aging
        isAging = false;
        isReady = true;
        remainingSeconds = resetSeconds;

        Debug.Log("[Barrel] Wine is ready!");
    }

    /// <summary>
    /// Converts the aged grapes inside the barrel into bottled wine.
    /// Adds bottles to inventory and resets barrel.
    /// </summary>
    void HarvestBottles()
    {
        if (!isReady || grapesInside <= resetGrapesInside) return;
        if (InventoryManager.Instance == null) return;

        // Calculate number of bottles
        int bottles = grapesInside / grapesPerBottle;
        int noBottles = 0; // avoid magic numbers
        if (bottles <= noBottles)
        {
            Debug.LogWarning("[Barrel] No full bottles to harvest");
            return;
        }

        // Add bottles to inventory
        bool added = InventoryManager.Instance.Add(bottleItemId, bottles);
        Debug.Log($"[Barrel] Harvested {bottles} bottles of wine. success={added}");

        // Reset barrel to empty state
        grapesInside = resetGrapesInside;
        isReady = resetIsAgingAndReady;
        isAging = resetIsAgingAndReady;
        totalSeconds = resetSeconds;
        remainingSeconds = resetSeconds;
    }
}
