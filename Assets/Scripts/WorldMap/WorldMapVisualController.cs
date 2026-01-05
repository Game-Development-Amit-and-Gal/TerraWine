using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Image))] // This forces the script to sit on an object with an Image
public class WorldMapVisualController : MonoBehaviour
{
    // We don't use [SerializeField] because we find it automatically
    private SeasonManager seasonManager;

    [SerializeField] private SpriteRenderer mapSprite;

    [Header("Visual Configuration")]
    [Tooltip("The standard map (Earth/Vine seasons)")]
    public Sprite standardMapSprite;

    [Tooltip("The map used during the Winery/Competition season")]
    public Sprite competitionMapSprite;

    private void Awake()
    {
        // 1. Grab the Image component on THIS object (the map background)
        mapSprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 2. Find the Manager in the 'Main' scene automatically
        seasonManager = FindFirstObjectByType<SeasonManager>();

        if (seasonManager == null)
        {
            Debug.LogError("WorldMapVisualController: SeasonManager not found! Make sure the Main scene is loaded.");
            return;
        }

        // 3. Update the visuals immediately
        UpdateMapVisuals();
    }

    // TEMPORARY DEBUGGING CODE
    private void Update()
    {
        // Press 'T' on your keyboard to test the swap
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            // Toggle between the two sprites to verify they line up
            if (mapSprite.sprite == standardMapSprite)
            {
                mapSprite.sprite = competitionMapSprite;
                Debug.Log("Debug: Swapped to COMPETITION map.");
            }
            else
            {
                mapSprite.sprite = standardMapSprite;
                Debug.Log("Debug: Swapped to STANDARD map.");
            }
        }
    }

    public void UpdateMapVisuals()
    {
        // Get the season string (e.g., "Earth", "Vine", "Winery")
        string currentSeason = seasonManager.GetCurrentSeason();

        Debug.Log($"Map Visual Update: Season is {currentSeason}");

        // LOGIC: Based on your code, "Winery" seems to be the Competition season
        if (currentSeason == "Winery")
        {
            if (competitionMapSprite != null)
                mapSprite.sprite = competitionMapSprite;
        }
        else
        {
            // For "Earth" or "Vine", use the standard map
            if (standardMapSprite != null)
                mapSprite.sprite = standardMapSprite;
        }

    }



}