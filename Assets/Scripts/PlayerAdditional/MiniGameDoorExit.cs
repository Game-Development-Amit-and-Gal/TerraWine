// Assets/Scripts/MiniGames/ClosingWall/MiniGameDoorExit.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class MiniGameDoorExit : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    [Header("Loot")]
    [SerializeField] private bool commitLootOnExit = true;

    [Header("Safety")]
    [SerializeField] private bool disableAfterUse = true;

    private bool used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;

        if (disableAfterUse)
            GetComponent<Collider2D>().enabled = false;

        if (commitLootOnExit)
            MiniGameLootBuffer.Instance?.CommitToInventory();
        else
            MiniGameLootBuffer.Instance?.Clear();

        SceneManager.LoadScene(worldMapSceneName);
    }


}
