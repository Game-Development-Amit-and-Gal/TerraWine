using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuspectCardUI : MonoBehaviour
{
    [SerializeField] private Button portraitButton;   // Button on the Portrait Image
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text wineryText;

    private EnemyItemSO suspect;
    private ThiefGuessMiniGame game;

    public void Bind(EnemyItemSO suspect, ThiefGuessMiniGame game)
    {
        this.suspect = suspect;
        this.game = game;

        nameText.text = suspect.enemyName;
        wineryText.text = suspect.wineryName;

        RefreshPortrait();

        portraitButton.onClick.RemoveAllListeners();
        portraitButton.onClick.AddListener(() => game.OnSuspectChosen(suspect));
    }

    public void RefreshPortrait()
    {
        var sprite = suspect.GetDisplayPortrait();
        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
    }
}
