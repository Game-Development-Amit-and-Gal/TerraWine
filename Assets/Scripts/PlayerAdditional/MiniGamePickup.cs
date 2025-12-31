using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MiniGamePickup : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private int amount = 1;

    public void Configure(string newItemId, int newAmount)
    {
        itemId = newItemId;
        amount = newAmount;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.collider.CompareTag("Player")) return;

        MiniGameLootBuffer.Instance?.Add(itemId, amount);
        Destroy(gameObject);
    }

}
