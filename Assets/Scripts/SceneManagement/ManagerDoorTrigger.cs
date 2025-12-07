using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger placed on a doorway that loads the Manager Office scene
/// when the Player enters it.
/// </summary>
public class ManagerDoorTrigger : MonoBehaviour
{
    [Tooltip("The name of the scene for the Manager Office.")]
    public string sceneName = "Manager_Office";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react if the object that entered is the Player
        if (other.CompareTag("Player"))
        {
            // Load the target scene immediately
            SceneManager.LoadScene(sceneName);
        }
    }
}
