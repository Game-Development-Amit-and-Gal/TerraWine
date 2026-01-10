using UnityEngine;

/// <summary>
/// Controls the individual bottle part that flies across the screen.
/// It handles its own movement and detects if it has flown out of bounds.
/// </summary>
public class MovingPiece : MonoBehaviour
{
    // --- CONSTANTS (No Magic Numbers) ---
    private const float DEFAULT_SCREEN_X_LIMIT = 12f; // The X position at which the object is considered "off-screen"

    // --- SETTINGS ---
    [Header("Movement Settings")]
    [Tooltip("The X position absolute value limit. If x > this or x < -this, piece is destroyed.")]
    [SerializeField] private float _screenLimit = DEFAULT_SCREEN_X_LIMIT;

    // --- STATE ---
    private float _speed;
    private int _direction; // 1 for right, -1 for left
    private bool _isStopped = false;
    private bool _hasGoneOutOfBounds = false;

    /// <summary>
    /// Gets a value indicating whether this piece has flown off the screen.
    /// The Manager checks this to trigger a Game Over.
    /// </summary>
    public bool HasGoneOutOfBounds => _hasGoneOutOfBounds;

    /// <summary>
    /// Sets up the piece with specific speed and direction. 
    /// Called immediately after instantiation by the Manager.
    /// </summary>
    /// <param name="speed">How fast the piece moves in units per second.</param>
    /// <param name="direction">1 for Right, -1 for Left.</param>
    public void Initialize(float speed, int direction)
    {
        _speed = speed;
        _direction = direction;
        _isStopped = false;
        _hasGoneOutOfBounds = false;
    }

    /// <summary>
    /// Halts the movement immediately. 
    /// Called when the player presses Space to attempt a placement.
    /// </summary>
    public void StopMovement()
    {
        _isStopped = true;
    }

    void Update()
    {
        // 1. If we are stopped (player pressed space), do nothing.
        if (_isStopped) return;

        // 2. Move along the X axis based on speed and direction (Frame-rate independent)
        transform.Translate(Vector3.right * _speed * _direction * Time.deltaTime);

        // 3. Check boundaries
        // We use Mathf.Abs so we check both Left (-Limit) and Right (+Limit) at once.
        if (Mathf.Abs(transform.position.x) > _screenLimit)
        {
            _hasGoneOutOfBounds = true;

            // Destroy immediately to clean up the scene. 
            // The Manager holds a reference to this script and will read the 'HasGoneOutOfBounds' property 
            // before the Unity Garbage Collector completely invalidates the C# object wrapper.
            //Destroy(gameObject);
        }
    }
}