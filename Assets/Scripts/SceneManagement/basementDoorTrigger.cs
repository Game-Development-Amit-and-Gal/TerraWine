using UnityEngine;

public class BasementDoorTrigger : MonoBehaviour
{
    [Tooltip("The scene to load when entering.")]
    [SerializeField] private string sceneName = "basement";

    [Tooltip("Where the Player will spawn in the new scene.")]
    [SerializeField] private Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        TutorialManager.Instance?.SetFlag("Basement");
        GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
    }
}
