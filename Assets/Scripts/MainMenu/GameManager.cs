using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central controller for the game’s global state.
/// Manages saving/loading, scene transitions, player data, money, and tutorial flags.
/// Acts as a persistent Singleton that survives scene changes.
/// </summary>

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    // Singleton access point for game-wide logic (only one GameManager can exist)

    public GameData Data { get; private set; } = new GameData();
    // Holds all saved data for the current player (money, scene, inventory, tutorials, etc.)

    [SerializeField] private string firstScene = "SampleScene";
    // Name of the starting scene to load when beginning a new game

    [Header("Systems")]
    [SerializeField] private IntroController introController;
    // Controls tutorial intros and dialogue behavior

    [SerializeField] private SceneLoader sceneLoader;
    // Handles asynchronous scene loading and transitions

    [SerializeField] private EconomyManager economyManager;
    // Manages money transactions, pricing, and economy-related calculations

    private int startingMoney = 500;
    private int playerXs = 0;
    private int playerYs = 0;
    private enum seasons { None,Earth, War, Judge }
    void Awake()
    {
        // If another GameManager already exists → destroy this duplicate
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, set this as the global Singleton instance
        Instance = this;


        // Fallback: If these references were not assigned in the Inspector,
        // try to automatically find them in the scene
        if (!introController) introController = FindObjectOfType<IntroController>();
        if (!sceneLoader) sceneLoader = FindObjectOfType<SceneLoader>();
        if (!economyManager) economyManager = FindObjectOfType<EconomyManager>();
    }


    /// <summary>
    /// Starts a brand-new game by running the new-game boot sequence.
    /// Uses a coroutine because the startup process contains timed steps
    /// (delays, animations, loading screens), so it cannot happen instantly
    /// in one frame.
    /// </summary>
    public void NewGame()
    {
        StartCoroutine(NewGameFlow());
    }


    /// <remarks>
    /// ❓ **What is a Coroutine?**
    /// A coroutine is a Unity function that can pause in the middle (using
    /// `yield`, `WaitForSeconds`, or waiting for async operations), and then
    /// continue later without blocking the game.  
    /// This lets us do sequences over time, such as cutscenes, fades,
    /// UI messages, or loading screens, while the game keeps running normally.
    /// </remarks>






    private IEnumerator NewGameFlow()
    {
        // If an intro exists, play it (only if needed) before starting the game
        if (introController != null)
            yield return introController.PlayIntroIfNeeded();

        // Create a fresh save profile with default values for a new game
        Data = new GameData
        {
            sceneName = firstScene,                 // The first scene to load
            playerX = playerXs,                     // Player starting X position
            playerY = playerYs,                     // Player starting Y position
            money = startingMoney,                  // Initial starting money
            season = (int)seasons.Earth,            // Set starting season
            lastRealTimeTicks = DateTime.UtcNow.Ticks, // Timestamp for seasonal logic

            // Reset tutorial / guide progress flags
            tutorialCompleted = false,
            sampleSceneGuideDone = false,
            worldMapGuideDone = false,
            cellarGuideDone = false
        };

        // Reset inventory and give default starting items (if manager exists)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetAll();          // Clear inventory
            InventoryManager.Instance.Add("Cabernet_Sauvignon_Seed", 5); // Starter seeds
            InventoryManager.Instance.Add("Grenache_Seed", 5);
            InventoryManager.Instance.Add("Petit_verdot_Seed", 1);
            InventoryManager.Instance.Add("Garden_bed", 1);
            InventoryManager.Instance.AddCategory(ItemCategory.Update, 1);
        }
        else
        {
            Debug.LogWarning("[GameManager] NewGame: no InventoryManager.Instance found");
        }

        // Reset planted crops / vineyard system if present
        PlantManager.Instance?.ResetAll();

        // Load the actual scene and spawn player at the defined start position
        if (sceneLoader != null)
        {
            yield return sceneLoader.LoadSceneAndPlacePlayer(
                Data.sceneName,
                new Vector2(Data.playerX, Data.playerY)
            );
        }

        // Save the brand-new profile to persistent storage
        SaveGame();
    }




    public void ContinueGame()
    {
        // Try to load previously saved game data
        var loaded = SaveSystem.Load();
        if (loaded == null)                         // If no save exists:
        {
            Debug.LogWarning("[GameManager] No saved game found. Cannot continue.");
            return;                                  // Stop (there is nothing to continue)
        }

        Data = loaded;                               // Apply loaded save to current session

        // If a scene loader exists → load saved scene and restore player + plants
        if (sceneLoader != null)
        {
            StartCoroutine(
                sceneLoader.LoadScenePlaceAndRestorePlants(
                    Data.sceneName,                           // Scene to load
                    new Vector2(Data.playerX, Data.playerY),  // Player saved position
                    Data.lastRealTimeTicks                    // Used to restore plant growth state
                )
            );
        }
    }




    public void ChangeScene(string sceneName, Vector2 newPlayerPos)
    {
        // If a scene loader exists → perform a scene change
        if (sceneLoader != null)
        {
            StartCoroutine(
                sceneLoader.ChangeScene(
                    sceneName,              // Target scene to switch to
                    newPlayerPos,           // New player spawn location in that scene
                    Data.lastRealTimeTicks  // Time reference used to restore plant growth/game state
                )
            );
        }
    }




    public void SaveGame()
    {
        // Try to find the player (needed to store its current position)
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Data.playerX = p.transform.position.x;  // Save X position
            Data.playerY = p.transform.position.y;  // Save Y position
        }

        // Save the current scene name (so the game knows where to load next time)
        Data.sceneName = SceneManager.GetActiveScene().name;

        // If the scene contains crop plots → record the current real-world time
        // (used later to calculate plant growth while the player was away)
        bool hasPlots = PlantManager.Instance != null &&
                        PlantManager.Instance.HasAnyPlotsInScene();
        if (hasPlots)
        {
            Data.lastRealTimeTicks = DateTime.UtcNow.Ticks;
        }

        // Save all core game data to file
        SaveSystem.Save(Data);

        // Also save plant/crop state if possible
        PlantManager.Instance?.SaveAll();
    }



    public void AddMoney(int amount)
    {
        // Forward the request to the economy system to add money to the player
        if (economyManager != null)
            economyManager.AddMoney(amount);
    }

    public bool TrySpendMoney(int amount)
    {
        // Ask the economy system if the player can afford this cost
        // Returns true only if spending was successful
        return economyManager != null && economyManager.TrySpend(amount);
    }




    private void OnApplicationQuit()
    {
        // Do not save if we're in the main menu
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        // App is closing → save current progress
        Debug.Log("[GameManager] OnApplicationQuit called. Saving game.");
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        // Do not save if we're in the main menu
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        // App minimized or backgrounded (especially on mobile) → save progress
        Debug.Log("[GameManager] OnApplicationPause called. Pause: " + pause);

        // Save only when pausing, not when resuming
        if (pause) SaveGame();
    }

}
