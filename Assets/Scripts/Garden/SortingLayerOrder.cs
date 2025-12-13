using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortAbovePlayer : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] int behindOrder = 29;   // well behind player
    [SerializeField] int inFrontOrder = 31;  // well in front of player
    [SerializeField] float yOffset = 0f;     // tweak if pivot isn't centered

    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        if (!player) return;

        float playerY = player.position.y;
        float wellY = transform.position.y + yOffset;

        sr.sortingOrder = (playerY > wellY) ? inFrontOrder : behindOrder;
    }
}
