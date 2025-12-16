using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger placed on a doorway that loads a new scene when the Player enters.
/// </summary>
public class basementDoorTrigger : MonoBehaviour
{
    [Tooltip("The name of the scene to load when entering the basement/room.")]
    public string sceneName = "basement";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that touched this trigger is the Player
        if (other.CompareTag("Player"))
        {
            TutorialManager.Instance?.SetFlag("Basement");
            // Load the target scene immediately
            SceneManager.LoadScene(sceneName);
        }
    }
}
