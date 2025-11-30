using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData Data { get; private set; } = new GameData();

    [SerializeField] string firstScene = "SampleScene";
    [Header("UI")]
    [SerializeField] private Canvas mainMenuCanvas;
    [Header("Main Menu World")]
    [SerializeField] private GameObject mainMenuRoot;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void NewGame()
    {
        StartCoroutine(PlayIntroAndStartGame());
    }

    private IEnumerator PlayIntroAndStartGame()
    {
        // לכבות את הקנבס של המסך הראשי
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);
        if (mainMenuRoot != null)
            mainMenuRoot.gameObject.SetActive(false);

        VideoPlayer videoPlayer = GameObject.Find("IntroVideoPlayer")?.GetComponent<VideoPlayer>();
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.Play();

            yield return new WaitUntil(() => videoPlayer.isPlaying);
            yield return new WaitUntil(() => !videoPlayer.isPlaying);
        }
        else
        {
            Debug.LogWarning("[GameManager] No VideoPlayer or clip found. Skipping intro.");
        }

        // מכאן ממשיכים כמו שיש לך:
        Data = new GameData
        {
            sceneName = firstScene,
            playerX = 0,
            playerY = 0,
            money = 500,
            season = 1,
            lastRealTimeTicks = DateTime.UtcNow.Ticks
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
        SaveSystem.Save(Data);

        yield return StartCoroutine(LoadAndPlace(Data.sceneName,
                                      new Vector2(Data.playerX, Data.playerY)));
    }


    // ===== CONTINUE GAME =====
    public void ContinueGame()
    {
        var loaded = SaveSystem.Load();
        if (loaded == null)
        {
            Debug.LogWarning("[GameManager] No saved game found. Cannot continue.");
            return;
        }

        // חישוב כמה זמן אמיתי עבר מאז השמירה (שימושי להמשך)
        long nowTicks = DateTime.UtcNow.Ticks;
        float deltaSeconds = 0f;
        if (loaded.lastRealTimeTicks != 0)
        {
            deltaSeconds = (float)new TimeSpan(nowTicks - loaded.lastRealTimeTicks)
                                          .TotalSeconds;
        }

        Data = loaded;

        // טוענים סצנה + מחזירים שחקן + משחזרים ערוגות
        StartCoroutine(LoadAndPlaceAndRestore(Data.sceneName,
                                              new Vector2(Data.playerX, Data.playerY),
                                              deltaSeconds));
    }
    // ===== CHANGE SCENE DURING GAME =====
    public void ChangeScene(string sceneName, Vector2 newPlayerPos)
    {
        StartCoroutine(ChangeSceneCoroutine(sceneName, newPlayerPos));
    }

    private IEnumerator ChangeSceneCoroutine(string sceneName, Vector2 newPlayerPos)
    {
        // 1. שומרות משחק + (אם יש ערוגות) מעדכנות lastRealTimeTicks ומצב ערוגות
        SaveGame();

        // 2. טוענות את הסצנה החדשה
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
        yield return null;

        // 3. ממקמות את השחקן
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            p.transform.position = new Vector3(newPlayerPos.x, newPlayerPos.y, p.transform.position.z);

        // 4. אם בסצנה החדשה יש ערוגות – נתקדם בזמן מאז פעם אחרונה ששמרנו ערוגות
        if (PlantManager.Instance != null && PlantManager.Instance.HasAnyPlotsInScene())
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            float deltaSeconds = 0f;

            if (Data.lastRealTimeTicks != 0)
            {
                deltaSeconds = (float)new TimeSpan(nowTicks - Data.lastRealTimeTicks)
                                           .TotalSeconds;
            }

            PlantManager.Instance.LoadAll(deltaSeconds);

            Debug.Log("[GameManager] Loaded plants with deltaSeconds = " + deltaSeconds);
        }
    }




    // ===== SAVE =====
    public void SaveGame()
    {
        // מיקום השחקן
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Data.playerX = p.transform.position.x;
            Data.playerY = p.transform.position.y;
        }

        // שם הסצנה
        Data.sceneName = SceneManager.GetActiveScene().name;

        bool hasPlots = PlantManager.Instance != null && PlantManager.Instance.HasAnyPlotsInScene();

        // אם יש ערוגות בסצנה – זה רגע חשוב לצמחים, נעדכן lastRealTimeTicks
        if (hasPlots)
        {
            Data.lastRealTimeTicks = DateTime.UtcNow.Ticks;
        }

        // שומרים את ה-GameData (כסף, עונה וכו’)
        SaveSystem.Save(Data);

        // שומרים את מצב הערוגות – אבל SaveAll כבר לא ידרוס אם אין ערוגות
        PlantManager.Instance?.SaveAll();
    }


    // ===== HELPERS FOR LOADING =====
    IEnumerator LoadAndPlace(string scene, Vector2 pos)
    {
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) yield return null;
        yield return null;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);

        // במשחק חדש – הערוגות כבר אינן שמורות, ResetAll כבר נעשה
    }

    IEnumerator LoadAndPlaceAndRestore(string scene, Vector2 pos, float deltaSeconds)
    {
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) yield return null;
        yield return null;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);

        // עכשיו כשהסצנה עלתה ויש PlantPlot-ים – משחזרים את מצב הערוגות
        PlantManager.Instance?.LoadAll(deltaSeconds);
    }

    // ===== MONEY =====
    public void AddMoney(int amount)
    {
        Data.money += amount;
    }

    public bool TrySpendMoney(int amount)
    {
        if (Data.money < amount)
        {
            Debug.Log("[GameManager] Not enough money. Have: " +
                      Data.money + " need: " + amount);
            return false;
        }

        Data.money -= amount;
        Debug.Log("[GameManager] Spent " + amount +
                  ". New balance: " + Data.money);
        return true;
    }

    // ===== APP LIFECYCLE =====
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
