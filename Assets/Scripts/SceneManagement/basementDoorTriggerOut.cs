using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger that exits the basement and loads another scene when the Player steps on it.
/// </summary>
public class basementDoorTriggerOut : MonoBehaviour
{
    [Tooltip("The name of the scene to load when exiting the basement.")]
    public string sceneName = "WineryReception";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // Load the designated scene
            SceneManager.LoadScene(sceneName);
        }
    }
}
