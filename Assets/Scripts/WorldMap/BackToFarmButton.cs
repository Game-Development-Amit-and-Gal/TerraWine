using UnityEngine;

/// <summary>
/// Back arrow button on the world map.
/// Sends the player back to the farm scene.
/// </summary>
public class BackToFarmButton : MonoBehaviour
{
    // Name of the farm scene (must be in Build Profiles)
    [SerializeField] private string farmSceneName = "SampleScene";

    /// <summary>
    /// Called from the UI Button OnClick event.
    /// </summary>
    public void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            // Use the last saved player position in the farm
            var data = GameManager.Instance.Data;
            Vector2 farmPos = new Vector2(data.playerX, data.playerY);

            GameManager.Instance.ChangeScene(farmSceneName, farmPos);
        }
        else
        {
            Debug.LogWarning("[BackToFarmButton] GameManager.Instance is null.");
        }
    }
}
