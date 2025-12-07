using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the main menu buttons: New Game, Continue, and Quit.
/// Enables the Continue button only if a saved game exists.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button continueBtn; // UI button for continuing a saved game

    void Start()
    {
        // If a save file exists, enable the Continue button. Otherwise, disable it.
        if (continueBtn != null)
            continueBtn.interactable = SaveSystem.HasSave();
    }

    public void OnNewGame()
    {
        Debug.Log("[MainMenu] New Game Clicked");
        GameManager.Instance.NewGame(); // Start a new playthrough
    }

    public void OnContinue()
    {
        Debug.Log("[MainMenu] Continue clicked");
        GameManager.Instance.ContinueGame(); // Load previous save
    }

    public void OnQuit()
    {
        Debug.Log("[MainMenu] OnQuit Clicked");
        Application.Quit(); // Exit the application
    }
}
