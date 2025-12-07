using UnityEngine;

/// <summary>
/// Trigger used to enter the winery interior.
/// Automatically switches scenes using the GameManager and places the Player at a defined spawn point.
/// </summary>
public class EnterWineryDoor : MonoBehaviour
{
    [Tooltip("The scene to load when entering (e.g., WineryReception).")]
    public string sceneName = "WineryReception";

    [Tooltip("The position where the Player will spawn inside the new scene.")]
    public Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // Use the GameManager to properly change scenes and place the Player
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
    }
}
