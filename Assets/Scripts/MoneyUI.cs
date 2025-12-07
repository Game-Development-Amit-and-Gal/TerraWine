using TMPro;
using UnityEngine;

/// <summary>
/// Displays the player's current money on the UI.
/// Reads the value from GameManager every frame and updates the text.
/// </summary>
public class MoneyUI : MonoBehaviour
{
    /// <summary>
    /// Reference to the TMP text UI element displaying the money amount.
    /// Must be assigned in the Inspector.
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI moneyText;

    private void Update()
    {
        // If the GameManager doesn't exist (rare but safe check), do not update
        if (GameManager.Instance == null)
            return;

        // Display the current money value from the saved game data
        moneyText.text = GameManager.Instance.Data.money.ToString();
    }
}
