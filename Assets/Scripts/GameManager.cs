using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData Data { get; private set; } = new GameData();

    [SerializeField] string firstScene = "SampleScene";

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

    // ===== NEW GAME =====
    public void NewGame()
    {
        // 1. נתוני משחק חדשים
        Data = new GameData
        {
            sceneName = firstScene,
            playerX = 0,
            playerY = 0,
            money = 500,
            season = 1,
            lastRealTimeTicks = DateTime.UtcNow.Ticks   // זמן התחלה
        };

        // 2. אינבנטורי חדש + פריטים התחלתיים
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetAll();
            // ה-id חייב להיות בדיוק כמו ב-ItemSO !
            InventoryManager.Instance.Add("Cabernet_Sauvignon_Seed", 5);
        }
        else
        {
            Debug.LogWarning("[GameManager] NewGame: no InventoryManager.Instance found");
        }

        // 3. איפוס כל הערוגות
        PlantManager.Instance?.ResetAll();

        // 4. שמירה
        SaveSystem.Save(Data);

        // 5. טעינת סצנת ההתחלה
        StartCoroutine(LoadAndPlace(Data.sceneName,
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

        // זמן שמירה אחרון
        Data.lastRealTimeTicks = DateTime.UtcNow.Ticks;

        // שמירת GameData
        SaveSystem.Save(Data);

        // שמירת כל הערוגות
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
