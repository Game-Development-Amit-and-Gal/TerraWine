using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using System.Threading.Tasks;

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
        // Ensure EnemyRaidManager exists (persistent)
        if (EnemyRaidManager.Instance == null)
        {
            var go = new GameObject("EnemyRaidManager");
            go.AddComponent<EnemyRaidManager>();
        }



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
        SaveSystem.Delete();
        _ = DeleteCloudSafe();

        var sm = FindFirstObjectByType<SeasonManager>();
        if (sm != null) sm.ResetForNewGame();

        StartCoroutine(NewGameFlow());
        TutorialManager.tutorialIsRunning = true;
        TutorialManager.tutorialIsRunningGardenScene = true;
    }


    private IEnumerator NewGameFlow()
    {
        if (introController != null)
            yield return introController.PlayIntroIfNeeded();

        Data = new GameData
        {
            calendarYear = 1,
            calendarSeasonIndex = 0,
            calendarDay = 1,
            calendarLastUpdateTicks = DateTime.UtcNow.Ticks,

            sceneName = firstScene,
            playerX = playerXs,
            playerY = playerYs,
            money = startingMoney,
            season = (int)seasons.Earth,
            lastRealTimeTicks = DateTime.UtcNow.Ticks,
            wineScore = 0,
            securityLevel = 0,
            dailyActionsUsed = 0,
            dailyActionsResetTicks = 0,
            lastRaidTicks = 0,
            stolenRecipeIds = new List<string>(),
            raidLog = new List<string>(),
            waterMax = 20,
            waterCurrent = 5,

            waterLastUpdateTicks = DateTime.UtcNow.Ticks,
            waterGrowingCountSnapshot = 0,
            waterDrainRemainder = 0f,

            tutorialCompleted = false,
            sampleSceneGuideDone = false,
            worldMapGuideDone = false,
            wineGuideDone = false,
            basementGuideDone = false,
            wineryReceptionGuideDone = false,


            inventory = new List<InventoryItem>(),
            ownedBarrels = new List<OwnedBarrelData>(),
            barrelAging = new List<BarrelAgingSave>(),
            unlockedRecipeIds = new List<string>()
        };

        // ✅ הוספה: אתחול קווטה אחרי יצירת Data
        ActionQuotaManager.Instance?.InitializeForCurrentProfile();

        // Unlock starting recipes...
        if (startingRecipeIds != null)
        {
            foreach (var recipeId in startingRecipeIds)
            {
                if (string.IsNullOrWhiteSpace(recipeId)) continue;
                if (!Data.unlockedRecipeIds.Contains(recipeId))
                    Data.unlockedRecipeIds.Add(recipeId);
            }
        }

        SaveSystem.Save(Data);

        // Reset inventory and give starting items
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetAll();
            InventoryManager.Instance.Add("Cabernet_Sauvignon_Seed", 5);
            InventoryManager.Instance.Add("Grenache_Seed", 5);
            InventoryManager.Instance.Add("Petit_verdot_Seed", 1);
            InventoryManager.Instance.Add("Colombard_Seed", 2);
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

    public async void ContinueGame()
    {
        Debug.Log("[GameManager] CONTINUE clicked");
        TutorialManager.tutorialIsRunning = false;

        Debug.Log("[GameManager] IsSignedIn=" + AuthenticationService.Instance.IsSignedIn);

        GameData loaded = null;

        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                var json = await CloudSaveSystem.LoadStringAsync("savegame");

                if (!string.IsNullOrEmpty(json))
                {
                    loaded = JsonUtility.FromJson<GameData>(json);
                    Debug.Log("[GameManager] Loaded from CLOUD (key=savegame). jsonLen=" + json.Length);
                }
                else
                {
                    Debug.LogWarning("[GameManager] CLOUD returned empty/null for key=savegame -> fallback to LOCAL");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] Not signed in -> skip CLOUD -> fallback to LOCAL");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameManager] Cloud load failed -> fallback to LOCAL. " + e.Message);
        }

        if (loaded == null)
        {
            loaded = SaveSystem.Load();
            Debug.Log("[GameManager] Loaded from LOCAL (savegame.json)");
        }

        if (loaded == null)
        {
            Debug.LogWarning("[GameManager] No saved game found (cloud+local). Cannot continue.");
            return;
        }

        Data = loaded;

        if (Data.unlockedRecipeIds == null) Data.unlockedRecipeIds = new List<string>();
        if (Data.stolenRecipeIds == null) Data.stolenRecipeIds = new List<string>();
        if (Data.raidLog == null) Data.raidLog = new List<string>();

        ActionQuotaManager.Instance?.InitializeForCurrentProfile();
        EnemyRaidManager.Instance?.ScheduleRaidOnNextSceneLoad("continue");

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
        EnemyRaidManager.Instance?.ScheduleRaidOnNextSceneLoad("scene_change");

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
        _ = SaveCloudSafe();
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

 
    private async Task DeleteCloudSafe()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                return;

            await CloudSaveSystem.DeleteAsync("savegame");
            Debug.Log("[GameManager] Cloud Delete OK");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameManager] Cloud Delete failed: " + e.Message);
        }
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
    private async Task SaveCloudSafe()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                return;

            await CloudSaveSystem.SaveStringAsync("savegame", JsonUtility.ToJson(Data, true));

            Debug.Log("[GameManager] Cloud Save OK");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameManager] Cloud Save failed: " + e.Message);
        }
    }



    //ADDED BY CHATGPT 31.12
    public void AddItemsAndSave(IEnumerable<(string itemId, int amount)> items)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] AddItemsAndSave: no InventoryManager.");
            return;
        }

        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.itemId) || it.amount <= 0) continue;
            InventoryManager.Instance.Add(it.itemId, it.amount);
        }

        SaveGame(); // one local save + one cloud save
    }


}
