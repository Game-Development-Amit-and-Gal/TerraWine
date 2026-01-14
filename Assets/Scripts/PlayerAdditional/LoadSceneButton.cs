//using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneButton : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName = "ClosingWallMiniGame";

    public void Load()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        else SceneManager.LoadScene(sceneName);
    }
}
