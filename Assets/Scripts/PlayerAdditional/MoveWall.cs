using UnityEngine;

public class MoveWall : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float wallSpeed = 0.5f;

    [Tooltip("Place this Transform to the RIGHT of the wall. The wall will move right until it reaches it.")]
    [SerializeField] private Transform rightWallBound;

    [SerializeField] private float collisionEpsilon = 0.5f;

    private const float stop = 0f;

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // IMPORTANT: do NOT assign the bound to the wall's own Transform.
        // Drag a separate 'RightWallBound' object from the scene into the inspector.
    }

    private void Update()
    {
        if (rb == null || rightWallBound == null) return;

        // distance remaining until we reach the right bound
        float distance = rightWallBound.position.x - rb.position.x;
        bool farFromBound = distance >= collisionEpsilon;

        if (farFromBound)
        {
            Vector2 v = rb.linearVelocity;
            v.x = wallSpeed; // move RIGHT (no deltaTime when setting velocity)
            rb.linearVelocity = v;
        }
        else
        {
            Vector2 v = rb.linearVelocity;
            v.x = stop; // stop
            rb.linearVelocity = v;
        }
    }
}
