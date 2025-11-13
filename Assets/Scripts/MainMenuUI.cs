using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button continueBtn;

    void Start()
    {
        if (continueBtn != null) continueBtn.interactable = SaveSystem.HasSave();
    }
    public void OnNewGame() { GameManager.Instance.NewGame(); }
    public void OnContinue() { GameManager.Instance.ContinueGame(); }
    public void OnQuit() { Application.Quit(); }
}
