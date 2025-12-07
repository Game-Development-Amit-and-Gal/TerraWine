using UnityEngine;

/// <summary>
/// Trigger for leaving the interior of the winery back to another scene.
/// Uses the GameManager to change scenes and place the Player in a specific position.
/// </summary>
public class EnterWineryDoorOut : MonoBehaviour
{
    [Tooltip("The scene to load when exiting the winery (e.g., MainVineyard).")]
    public string sceneName = "WineryReception";

    [Tooltip("Spawn position for the Player after leaving the winery.")]
    public Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the Player enters the trigger
        if (other.CompareTag("Player"))
        {
            // Correct scene transition using the GameManager system
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
    }
}
