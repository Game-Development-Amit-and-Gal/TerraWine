using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the pause menu UI and game freezing.
/// Uses ESC to toggle, also provides Save, Resume, and Quit functions.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    // The pause menu UI panel (assigned in Inspector)
    [SerializeField] private GameObject panel;

    void Start()
    {
        // Ensure the pause panel starts hidden when entering the game
        if (panel)
            panel.SetActive(false);
    }

    void Update()
    {
        // Check if keyboard exists (important when using new Input System on consoles)
        // When ESC is pressed → toggle pause panel + freeze time
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool show = !panel.activeSelf;  // flip state (show/hide UI)
            panel.SetActive(show);          // show or hide the pause menu

            // Freeze game when menu is open, resume when closing it
            Time.timeScale = show ? 0f : 1f;
        }
    }

    /// <summary>
    /// Save button in pause menu (uses GameManager saving).
    /// </summary>
    public void OnSave() => GameManager.Instance.SaveGame();

    /// <summary>
    /// Resume button — hide the pause UI and unfreeze the game.
    /// </summary>
    public void OnResume()
    {
        float setOne = 1f;
        panel.SetActive(false);
        Time.timeScale = setOne;
    }

    /// <summary>
    /// Quit button — unfreeze, save game, wait briefly, then load main menu.
    /// </summary>
    public async void OnQuitToMenu()
    {
        Time.timeScale = 1f; // ensure game isn't frozen in menu
        int delay = 1000;
        await System.Threading.Tasks.Task.Delay(delay); // small delay for UI effect/animation
        OnSave();                                      // save before quitting
        SceneManager.LoadScene("MainMenu");            // go to main menu scene
    }
}
