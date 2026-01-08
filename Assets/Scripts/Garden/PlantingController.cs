using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;   // Required for the new Input System (Keyboard/Mouse)

/// <summary>
/// Handles seed selection, planting logic, and the visual cursor seed icon.
/// This controller sits between the inventory and the farmland.
/// </summary>
public class PlantingController : MonoBehaviour
{
    public static PlantingController Instance { get; private set; }

    [SerializeField] private float cursorWorldHeight = 1f;     // Desired height (in world units) of the seed icon
                                                               // when used as the cursor

    [Header("Cursor Seed")]
    [SerializeField] private SpriteRenderer cursorSprite;      // The seed icon that follows the mouse cursor

    // The currently selected seed (stored as ItemSO)
    private ItemSO currentSeedItem;
    [Header("UI references")]
    [SerializeField] private RectTransform uiContainer;
    [SerializeField] private RectTransform canvasTransform;
    [SerializeField] private Vector2 textOffset = new Vector2(-1100,0f);

    /// <summary>
    /// True if a seed is currently selected from inventory.
    /// </summary>
    public bool HasSeed => currentSeedItem != null;

    void Awake()
    {
        // Singleton pattern: avoids having more than one controller
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Disable cursor sprite on start (no seed selected yet)
        if (cursorSprite != null)
            cursorSprite.enabled = false;

        if(uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
            
        

 
        // Ensure normal mouse cursor is visible while no seed is selected
        Cursor.visible = true;
    }

    /// <summary>
    /// Called when a seed is selected from inventory.
    /// Updates cursor icon + hides normal mouse cursor.
    /// </summary>
    public void SelectSeed(ItemSO item)
    {
        // If null or not a seed → cancel selection
        if (item == null || !item.isSeed)
        {
            ClearSelection();
            return;
        }

        currentSeedItem = item;

        // Show the cursor seed icon and resize it
        if (cursorSprite != null)
        {
            cursorSprite.sprite = item.icon;
            cursorSprite.enabled = true;
            SetCursorSpriteSize(item.icon);
        }
        //removePromptItem.enabled = true;
        uiContainer.gameObject.SetActive(true);

        // Hide default mouse cursor → we now use the seed icon instead
        Cursor.visible = false;
    }

    /// <summary>
    /// Unselects the seed and restores the default cursor.
    /// </summary>
    public void ClearSelection()
    {
        currentSeedItem = null;

        if (cursorSprite != null)
            cursorSprite.enabled = false;
        uiContainer.gameObject.SetActive(false);

        // Restore the normal mouse cursor
        Cursor.visible = true;
    }

    void OnDisable()
    {
        // Safety measure: if controller is disabled, always show cursor again
        Cursor.visible = true;
    }

    private void Update()
    {
        // Right-click cancels seed selection
        if (HasSeed && Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
        }

        // If no seed or no sprite, stop tracking cursor
        if (!HasSeed || cursorSprite == null || Camera.main == null)
            return;

        // Follow mouse position with the seed icon
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        cursorSprite.transform.position = world;
        if( uiContainer != null && canvasTransform != null)
        {
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasTransform,
                mousePos,
                null,
                out anchoredPos);
            uiContainer.anchoredPosition = anchoredPos + textOffset;
        }
    }

    /// <summary>
    /// Called from PlantPlot after clicking a plot.
    /// Attempts to plant the selected seed on that plot.
    /// </summary>
    public bool TryPlantOn(PlantPlot plot)
    {
        if (!HasSeed || plot == null) return false;
        if (InventoryManager.Instance == null) return false;

        string seedId = currentSeedItem.id;

        // Check how many seeds the player has
        int countBefore = InventoryManager.Instance.CountOf(seedId);

        // If none → cancel selection and fail
        if (countBefore <= 0)
        {
            ClearSelection();
            return false;
        }

        // Cannot plant if the plot is occupied or ready
        if (!plot.CanPlant) return false;

        // Remove exactly 1 seed from inventory
        InventoryManager.Instance.Remove(seedId, 1);

        // Start seed growth in the plot
        plot.StartGrowth(currentSeedItem);

        // If that was the last seed → cancel selection automatically
        int countAfter = InventoryManager.Instance.CountOf(seedId);
        if (countAfter <= 0)
        {
            ClearSelection();
        }

        return true;
    }

    /// <summary>
    /// Dynamically adjusts icon scale so its height matches `cursorWorldHeight`.
    /// Prevents oddly sized cursor icons depending on source sprite resolution.
    /// </summary>
    private void SetCursorSpriteSize(Sprite sprite)
    {
        if (sprite == null || cursorSprite == null)
            return;

        // Pixel-to-unit conversion amount
        float unitsPerPixel = 1f / sprite.pixelsPerUnit;
        float spriteHeightInWorld = sprite.rect.height * unitsPerPixel;

        // Needed scale to match desired height
        float scale = cursorWorldHeight / spriteHeightInWorld;

        cursorSprite.transform.localScale = new Vector3(scale, scale, 1f);
    }
}