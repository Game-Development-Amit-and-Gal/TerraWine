using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button continueBtn;

    void Start()
    {
        if (continueBtn != null) continueBtn.interactable = SaveSystem.HasSave();
    }
    public void OnNewGame() { Debug.Log("[MainMenu] New Game Clicked"); GameManager.Instance.NewGame(); }
    public void OnContinue() { Debug.Log("[MainMenu] Continue clicked"); GameManager.Instance.ContinueGame(); }
    public void OnQuit() { Debug.Log("[MainMenu] OnQuit Clicked"); Application.Quit(); }
}
