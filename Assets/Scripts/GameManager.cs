using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameData Data { get; private set; } = new GameData();

    [SerializeField] string firstScene = "Garden"; // סצנת התחלה (לפי השם שלך)
    
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void NewGame()
    {
        Data = new GameData { sceneName = firstScene, playerX = 0, playerY = 0, money = 0, season = 1 };
        SaveSystem.Save(Data);
        StartCoroutine(LoadAndPlace(Data.sceneName, new Vector2(Data.playerX, Data.playerY)));
    }

    public void ContinueGame()
    {
        
        var loaded = SaveSystem.Load(); if (loaded == null) 
        {
            Debug.LogWarning("[GameManager] No saved game found. Cannot continue.");
            return;
        };
        Data = loaded;
        StartCoroutine(LoadAndPlace(Data.sceneName, new Vector2(Data.playerX, Data.playerY)));
    }

    public void SaveGame()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { Data.playerX = p.transform.position.x; Data.playerY = p.transform.position.y; }
        Data.sceneName = SceneManager.GetActiveScene().name;
        SaveSystem.Save(Data);
    }

    IEnumerator LoadAndPlace(string scene, Vector2 pos)
    {
        
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) yield return null;
        yield return null; // פריים לוודא שהשחקן נטען
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);
    }

    private void OnApplicationQuit()
    {
        if(SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationQuit called. Saving game.");
        SaveGame();
    }
    
    private void OnApplicationPause(bool pause)
    {   if(SceneManager.GetActiveScene().name == "MainMenu") return;
        Debug.Log("[GameManager] OnApplicationPause called. Pause: " + pause);
        if (pause) SaveGame();
    }
}
