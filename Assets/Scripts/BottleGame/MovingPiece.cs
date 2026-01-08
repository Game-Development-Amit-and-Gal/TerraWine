using UnityEngine;

// No namespace as requested
public class MovingPiece : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    // Internal state variables
    private int _direction = 1; // 1 for right, -1 for left
    private bool _isStopped = false;

    /// <summary>
    /// Sets up the piece when it is spawned by the manager.
    /// </summary>
    /// <param name="speed">How fast the piece moves.</param>
    /// <param name="direction">1 for Right, -1 for Left.</param>
    public void Initialize(float speed, int direction)
    {
        moveSpeed = speed;
        _direction = direction;
    }

    void Update()
    {
        // If the player stopped the piece, do not move it
        if (_isStopped) return;

        // Move the piece horizontally based on speed and direction
        transform.Translate(Vector3.right * _direction * moveSpeed * Time.deltaTime);

        // Optional: Destroy if it goes too far off-screen to clean up memory
        // (The manager handles the "Miss" logic, this is just for safety)
        if (Mathf.Abs(transform.position.x) > 20f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the player presses Space to lock the piece in place.
    /// </summary>
    public void StopMovement()
    {
        _isStopped = true;
    }
}