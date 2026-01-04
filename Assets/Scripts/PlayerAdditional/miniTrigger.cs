// Assets/Scripts/MiniGames/ClosingWall/MiniGameBarTrigger.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MiniGameBarTrigger : MonoBehaviour
{
    [SerializeField] private MiniGameCropSpawner spawner;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.collider.CompareTag("Player")) return;
        if (spawner == null) return;

        spawner.SpawnAndLaunch(other.transform);
    }

}
