using UnityEngine;

/// <summary>
/// UI button used on the World Map.
/// Sends the player back to the farm scene and restores their last saved position.
/// </summary>
public class BackToFarmButton : MonoBehaviour
{
    // 👨‍🌾 Name of the farm scene to load (must be added to Build Settings → Scenes In Build)
    [SerializeField] private string farmSceneName = "SampleScene";

    /// <summary>
    /// Called from the UI Button → OnClick() event in the Inspector.
    /// Loads the farm scene and places the player at their last recorded position.
    /// </summary>
    public void OnBackClicked()
    {
        // Validate that the GameManager exists
        if (GameManager.Instance != null)
        {
            // Load last saved position stored in GameData
            var data = GameManager.Instance.Data;
            Vector2 farmPos = new Vector2(data.playerX, data.playerY);

            // Pass scene + exact return position
            GameManager.Instance.ChangeScene(farmSceneName, farmPos);
        }
        else
        {
            Debug.LogWarning("[BackToFarmButton] GameManager.Instance is null. Cannot change scene.");
        }
    }
}
