using UnityEngine;

public class IdleState : MonoBehaviour
{
    [SerializeField] private Vector2 spawnPosition = new Vector2(-60f, -60f);
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() // called when this state becomes active
    {
        if (!rb) return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.position = spawnPosition;  // teleport
    }
}
