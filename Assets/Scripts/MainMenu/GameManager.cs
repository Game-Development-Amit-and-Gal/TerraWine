using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData Data { get; private set; } = new GameData();

    [SerializeField] private string firstScene = "SampleScene";

    [Header("Systems")]
    [SerializeField] private IntroController introController;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private EconomyManager economyManager;


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        

     
        if (!introController) introController = FindObjectOfType<IntroController>();
        if (!sceneLoader) sceneLoader = FindObjectOfType<SceneLoader>();
        if (!economyManager) economyManager = FindObjectOfType<EconomyManager>();
    }
   

    public void NewGame()
    {
        StartCoroutine(NewGameFlow());
    }

    private IEnumerator NewGameFlow()
    {
        if (introController != null)
            yield return introController.PlayIntroIfNeeded();

        Data = new GameData
        {
            sceneName = firstScene,
            playerX = 0,
            playerY = 0,
            money = 500,
            season = 1,
            lastRealTimeTicks = DateTime.UtcNow.Ticks,

           
            tutorialCompleted = false,
            sampleSceneGuideDone = false,
            worldMapGuideDone = false,
            cellarGuideDone = false
        };

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetAll();
            InventoryManager.Instance.Add("Cabernet_Sauvignon_Seed", 5);
            InventoryManager.Instance.Add("Grenache_Seed", 5);
            InventoryManager.Instance.Add("Petit_verdot_Seed", 1);
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

        SaveGame();
    }




    public void ContinueGame()
    {
        var loaded = SaveSystem.Load();
        if (loaded == null)
        {
            Debug.LogWarning("[GameManager] No saved game found. Cannot continue.");
            return;
        }

        Data = loaded;

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

        SaveSystem.Save(Data);
        PlantManager.Instance?.SaveAll();
    }


    public void AddMoney(int amount)
    {
        if (economyManager != null)
            economyManager.AddMoney(amount);
    }

    public bool TrySpendMoney(int amount)
    {
        return economyManager != null && economyManager.TrySpend(amount);
    }



    private void OnApplicationQuit()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationQuit called. Saving game.");
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationPause called. Pause: " + pause);
        if (pause) SaveGame();
    }
}
