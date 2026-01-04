// Assets/Scripts/MiniGames/ClosingWall/MiniGameFailTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class MiniGameFailTrigger : MonoBehaviour
{
    [SerializeField] private string worldMapSceneName = "WorldMap";
    [SerializeField] private bool disableAfterUse = true;

    private bool used;

    private void Reset()
    {
        // לרוב זה הכי נוח שיהיה Trigger, כדי שלא "יתקע"
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;

        // אין שלל!
        MiniGameLootBuffer.Instance?.Clear();

        if (disableAfterUse)
            GetComponent<Collider2D>().enabled = false;

        SceneManager.LoadScene(worldMapSceneName);
    }
}
