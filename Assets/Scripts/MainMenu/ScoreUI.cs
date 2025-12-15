using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        scoreText.text = GameManager.Instance.Data.wineScore.ToString();
    }
}
