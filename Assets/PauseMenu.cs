using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    void Start() { if (panel) panel.SetActive(false); }

    void Update()
    {
        // חדש: Keyboard.current
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool show = !panel.activeSelf;
            panel.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }
    }

    public void OnSave() => GameManager.Instance.SaveGame();
    public void OnResume() { panel.SetActive(false); Time.timeScale = 1f; }
    public void OnQuitToMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
}
