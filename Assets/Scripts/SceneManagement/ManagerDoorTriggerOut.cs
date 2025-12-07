using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger placed on a doorway that sends the player back to the winery reception.
/// </summary>
public class ManagerDoorTriggerOut : MonoBehaviour
{
    [Tooltip("Name of the scene to return to when exiting the Manager Office.")]
    public string sceneName = "WineryReception";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react if the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // Load the target scene immediately
            SceneManager.LoadScene(sceneName);
        }
    }
}
