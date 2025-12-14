using System;
using System.Collections;
using System.Collections.Generic;
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

    public GameData Data { get; private set; } = new GameData();

    [SerializeField] private string firstScene = "SampleScene";

    [Header("Systems")]
    [SerializeField] private IntroController introController;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private EconomyManager economyManager;

    [Header("Recipes (Start Unlocked)")]
    [Tooltip("Recipe IDs that will be unlocked on New Game (stored in save data, not in inventory).")]
    [SerializeField]
    private string[] startingRecipeIds =
    {
        "Merlot_Easygoing",
        "Young_Cabernet"
    };

    private int startingMoney = 500;
    private int playerXs = 0;
    private int playerYs = 0;

    private enum seasons { None, Earth, War, Judge }

    [Obsolete]
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!introController) introController = FindObjectOfType<IntroController>();
        if (!sceneLoader) sceneLoader = FindObjectOfType<SceneLoader>();
        if (!economyManager) economyManager = FindObjectOfType<EconomyManager>();
    }

    // ------------------------------
    // NEW GAME / CONTINUE
    // ------------------------------

    public void NewGame()
    {
        Debug.Log("[GameManager] NEW GAME clicked -> deleting old save");
        SaveSystem.Delete();                 // <-- קריטי: מוחק save ישן
        StartCoroutine(NewGameFlow());
        TutorialManager.tutorialIsRunning = true;
        TutorialManager.tutorialIsRunningGardenScene = true;
    }

    private IEnumerator NewGameFlow()
    {
        if (introController != null)
            yield return introController.PlayIntroIfNeeded();

        // Create brand new profile
        Data = new GameData
        {
            sceneName = firstScene,
            playerX = playerXs,
            playerY = playerYs,
            money = startingMoney,
            season = (int)seasons.Earth,
            lastRealTimeTicks = DateTime.UtcNow.Ticks,

            tutorialCompleted = false,
            sampleSceneGuideDone = false,
            worldMapGuideDone = false,
            cellarGuideDone = false,

            // כדי שלא יהיה מצב שגיים דאטה בונה רשימות עם שאריות
            inventory = new List<InventoryItem>(),
            ownedBarrels = new List<OwnedBarrelData>(),
            barrelAging = new List<BarrelAgingSave>(),
            unlockedRecipeIds = new List<string>() // <-- תמיד מאפסים
        };

        // Unlock starting recipes
        if (startingRecipeIds != null)
        {
            foreach (var recipeId in startingRecipeIds)
            {
                if (string.IsNullOrWhiteSpace(recipeId)) continue;
                if (!Data.unlockedRecipeIds.Contains(recipeId))
                    Data.unlockedRecipeIds.Add(recipeId);
            }
        }

        Debug.Log("[GameManager] NewGame unlockedRecipeIds = " + string.Join(", ", Data.unlockedRecipeIds));

        // שמירה מידית כדי לנעול את הניו-גיים החדש לפני טעינת סצנה
        SaveSystem.Save(Data);

        // Reset inventory and give starting items
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetAll();
            InventoryManager.Instance.Add("Cabernet_Sauvignon_Seed", 5);
            InventoryManager.Instance.Add("Grenache_Seed", 5);
            InventoryManager.Instance.Add("Petit_verdot_Seed", 1);
            InventoryManager.Instance.AddCategory(ItemCategory.Update, 1);
        }
        else
        {
            Debug.LogWarning("[GameManager] NewGame: no InventoryManager.Instance found");
        }

        PlantManager.Instance?.ResetAll();

        if (sceneLoader != null)
        {
            yield return sceneLoader.LoadSceneAndPlacePlayer(
                Data.sceneName,
                new Vector2(Data.playerX, Data.playerY)
            );
        }

        // Save again after scene load (player position, etc.)
        SaveGame();
    }

    public void ContinueGame()
    {
        Debug.Log("[GameManager] CONTINUE clicked");

        TutorialManager.tutorialIsRunning = false;
        // Try to load previously saved game data
        var loaded = SaveSystem.Load();
        if (loaded == null)
        {
            Debug.LogWarning("[GameManager] No saved game found. Cannot continue.");
            return;
        }

        Data = loaded;

        if (Data.unlockedRecipeIds == null)
            Data.unlockedRecipeIds = new List<string>();

        Debug.Log("[GameManager] Continue unlockedRecipeIds = " + string.Join(", ", Data.unlockedRecipeIds));

        if (sceneLoader != null)
        {
            StartCoroutine(
                sceneLoader.LoadScenePlaceAndRestorePlants(
                    Data.sceneName,
                    new Vector2(Data.playerX, Data.playerY),
                    Data.lastRealTimeTicks
                )
            );
        }
    }

    // ------------------------------
    // SCENES / SAVE
    // ------------------------------

    public void ChangeScene(string sceneName, Vector2 newPlayerPos)
    {
        if (sceneLoader != null)
        {
            StartCoroutine(
                sceneLoader.ChangeScene(
                    sceneName,
                    newPlayerPos,
                    Data.lastRealTimeTicks
                )
            );
        }
    }

    public void SaveGame()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Data.playerX = p.transform.position.x;
            Data.playerY = p.transform.position.y;
        }

        Data.sceneName = SceneManager.GetActiveScene().name;

        bool hasPlots = PlantManager.Instance != null &&
                        PlantManager.Instance.HasAnyPlotsInScene();
        if (hasPlots)
        {
            Data.lastRealTimeTicks = DateTime.UtcNow.Ticks;
        }

        if (Data.unlockedRecipeIds == null)
            Data.unlockedRecipeIds = new List<string>();

        SaveSystem.Save(Data);
        PlantManager.Instance?.SaveAll();
    }

    // ------------------------------
    // MONEY
    // ------------------------------

    public void AddMoney(int amount)
    {
        if (economyManager != null)
            economyManager.AddMoney(amount);
    }

    public bool TrySpendMoney(int amount)
    {
        return economyManager != null && economyManager.TrySpend(amount);
    }

    // ------------------------------
    // RECIPES API
    // ------------------------------

    public bool IsRecipeUnlocked(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId)) return false;
        return Data.unlockedRecipeIds != null && Data.unlockedRecipeIds.Contains(recipeId);
    }

    public bool UnlockRecipe(string recipeId, bool saveImmediately = true)
    {
        if (string.IsNullOrWhiteSpace(recipeId)) return false;

        if (Data.unlockedRecipeIds == null)
            Data.unlockedRecipeIds = new List<string>();

        if (Data.unlockedRecipeIds.Contains(recipeId))
            return false;

        Data.unlockedRecipeIds.Add(recipeId);

        if (saveImmediately)
            SaveGame();

        return true;
    }

    private void OnApplicationQuit()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationQuit -> Saving game.");
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationPause Pause=" + pause);
        if (pause) SaveGame();
    }
}
