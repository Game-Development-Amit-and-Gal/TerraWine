using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    void Update()
    {
        if (GameManager.Instance == null) return;

        moneyText.text = GameManager.Instance.Data.money.ToString();
    }
}
