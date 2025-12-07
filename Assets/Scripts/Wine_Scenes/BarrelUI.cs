// Assets/Scripts/Barrels/BarrelUI.cs
using UnityEngine;
using TMPro;

/// <summary>
/// Controls the UI for selecting how many grapes to use in a barrel
/// and whether the produced wine will be dry or semi-dry.
/// </summary>
public class BarrelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;       // UI panel that pops up for choosing settings
    [SerializeField] private TMP_Text grapesText;    // Text field showing selected amount of grapes
    [SerializeField] private TMP_Text modeText;      // Text field showing wine style (Dry / Semi-Dry)

    Barrel currentBarrel;   // The barrel that is currently being configured through the UI

    int maxGrapes;          // Maximum usable grapes (rounded down to BottleSize multiple)
    int selectedGrapes;     // The currently chosen grape amount by the user
    bool makeDry = false;   // Wine mode flag: false = Semi-Dry, true = Dry

    /// <summary>
    /// Opens the UI with a reference to a barrel
    /// and calculates allowed grape usage based on availability.
    /// </summary>
    public void OpenForBarrel(Barrel barrel, int grapesAvailable)
    {
        // Store the target barrel to apply the result later
        currentBarrel = barrel;

        // Ensure the player can only use grapes equal to full bottle multiples
        maxGrapes = (grapesAvailable / barrel.GrapesPerBottle) * barrel.GrapesPerBottle;

        // If there aren't enough grapes to make even one bottle, cancel
        if (maxGrapes <= 0)
        {
            Debug.Log("[BarrelUI] No grapes to use");
            return;
        }

        // Start with the minimum usable amount (exactly one bottle)
        selectedGrapes = barrel.GrapesPerBottle;

        // Default wine type: Semi-Dry
        makeDry = false;

        // Update displayed text before showing the UI
        RefreshUI();

        // Show the UI panel to the player
        panel.SetActive(true);
    }

    /// <summary>
    /// Close the UI window and unlink any active barrel.
    /// </summary>
    public void Close()
    {
        panel.SetActive(false);   // Hide UI panel
        currentBarrel = null;     // Clear reference so we don't affect something wrongly later
    }

    /// <summary>
    /// Increase selected grapes in increments of one bottle,
    /// stopping when the maximum allowed amount is reached.
    /// </summary>
    public void OnMorePressed()
    {
        if (currentBarrel == null) return; // Safety block

        // Add grapes but never go above maxGrapes
        selectedGrapes = Mathf.Min(
            selectedGrapes + currentBarrel.GrapesPerBottle,
            maxGrapes
        );

        RefreshUI(); // Refresh displayed number
    }

    /// <summary>
    /// Decrease selected grapes in bottle-sized steps,
    /// but never allow dropping below the minimum needed for one bottle.
    /// </summary>
    public void OnLessPressed()
    {
        if (currentBarrel == null) return;

        // Reduce grapes but keep it >= 1 bottle
        selectedGrapes = Mathf.Max(
            currentBarrel.GrapesPerBottle,
            selectedGrapes - currentBarrel.GrapesPerBottle
        );

        RefreshUI();
    }

    /// <summary>
    /// Switch to Semi-Dry wine mode.
    /// </summary>
    public void OnSemiDryPressed()
    {
        makeDry = false; // Update state
        RefreshUI();     // Update label
    }

    /// <summary>
    /// Switch to Dry wine mode.
    /// </summary>
    public void OnDryPressed()
    {
        makeDry = true;
        RefreshUI();
    }

    /// <summary>
    /// Finalizes user choice and tells the barrel to start aging.
    /// </summary>
    public void OnConfirmPressed()
    {
        // Safety: If no barrel is selected or no grapes were chosen, just close the UI
        if (currentBarrel == null || selectedGrapes <= 0)
        {
            Close();
            return;
        }

        // Tell the barrel to begin the aging process using these settings
        currentBarrel.StartAging(selectedGrapes, makeDry);

        // Close the UI after confirming
        Close();
    }

    /// <summary>
    /// Updates all text displays based on current settings.
    /// </summary>
    void RefreshUI()
    {
        // Update grapes text
        if (grapesText != null)
            grapesText.text = selectedGrapes.ToString();

        // Update wine style label
        if (modeText != null)
            modeText.text = makeDry
                ? "Dry (5 minutes)"        // Aging duration shown for clarity
                : "Semi-Dry (2 minutes)";
    }
}
