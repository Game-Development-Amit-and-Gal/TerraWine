using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System (Keyboard/Gamepad)

/// <summary>
/// Handles the UI panel for purchasing barrels.
/// Supports regular and premium purchases and closes via ESC/start button.
/// </summary>
public class BarrelShopUI : MonoBehaviour
{
    // Singleton for easy access from BarrelPurchase scripts
    public static BarrelShopUI Instance { get; private set; }

    [SerializeField] private GameObject panel;   // UI purchase panel (assigned in Inspector)

    private BarrelPurchase currentBarrel;        // The barrel currently selected for purchase

    private void Awake()
    {
        // Enforce Singleton: destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Make sure the menu starts hidden
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        // Detect ESC (keyboard) or START (gamepad) using Input System
        bool pressedEsc =
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        // If pressed and the panel is open → close the UI
        if (pressedEsc && panel != null && panel.activeInHierarchy)
            Close();
    }

    /// <summary>
    /// Opens the UI and sets the target purchase barrel.
    /// </summary>
    public void Open(BarrelPurchase barrel)
    {
        currentBarrel = barrel;         // Save the selected barrel reference
        if (panel != null)
            panel.SetActive(true);      // Show the UI
    }

    /// <summary>
    /// Closes the UI and clears the selected barrel.
    /// </summary>
    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        currentBarrel = null; // Remove the reference to prevent accidental purchases
    }

    /// <summary>
    /// Button: Buy a normal barrel.
    /// </summary>
    public void OnClickBuyNormal()
    {
        if (currentBarrel == null) return;
        currentBarrel.BuyNormal();  // Trigger purchase logic
        Close();                    // Close UI after action
    }

    /// <summary>
    /// Button: Buy a premium barrel.
    /// </summary>
    public void OnClickBuyPremium()
    {
        if (currentBarrel == null) return;
        currentBarrel.BuyPremium(); // Trigger purchase logic
        Close();                    // Close UI after action
    }
}
