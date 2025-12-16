using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger placed inside the winery.
/// When the player enters, it loads the scene where wine production takes place.
/// </summary>
public class WineryWineTrigger : MonoBehaviour
{
    /// <summary>
    /// Name of the scene to load when the Player walks into the trigger.
    /// Must match a scene name in the Build Settings.
    /// </summary>
    [Tooltip("The target wine-production scene to load when entering.")]
    [SerializeField] private string sceneName = "wine";

    /// <summary>
    /// When another object enters this 2D trigger,
    /// check if it's the Player. If yes — load the target scene.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only respond to the Player entering the trigger
        if (other.CompareTag("Player"))
        {
            TutorialManager.Instance?.SetFlag("Right door");
            SceneManager.LoadScene(sceneName);
        }
        
    }
}
