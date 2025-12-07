using UnityEngine;

/// <summary>
/// Handles the world map button click and asks the GameManager to change scene.
/// </summary>
public class WorldMapButton : MonoBehaviour
{
    // Set a world map Name
    [SerializeField] private string worldMapSceneName = "WorldMap";

    // we don't have a player in the world map, so position is not important
    [SerializeField] private Vector2 worldMapPlayerPos = Vector2.zero;

    /// <summary>
    /// Called from the UI Button OnClick event.
    /// </summary>
    public void OnMapClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(worldMapSceneName, worldMapPlayerPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapButton] GameManager.Instance is null.");
        }
    }
}
