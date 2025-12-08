using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles top-down player movement using the new Input System,
/// plays directional walking animations, and adds an idle breathing effect.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // ------------------------------
    //  MOVEMENT SETTINGS
    // ------------------------------

    [Header("Movement")]
    [Tooltip("Player movement speed in world units per second.")]
    public float moveSpeed = 3f;

    [Tooltip("Minimum movement required to register movement (prevents tiny floating input).")]
    public float movementThreshold = 0.01f;

    [Tooltip("Diagonal normalization threshold (when vector length is above this, normalize).")]
    public float diagonalNormalizeThreshold = 1f;

    // ------------------------------
    //  ANIMATION SETTINGS
    // ------------------------------

    [Header("Directional Sprite Animation")]
    [Tooltip("Animation frames for walking downward.")]
    public Sprite[] walkDown;

    [Tooltip("Animation frames for walking upward.")]
    public Sprite[] walkUp;

    [Tooltip("Animation frames for walking right.")]
    public Sprite[] walkRight;

    [Tooltip("Animation frames for walking left.")]
    public Sprite[] walkLeft;

    [Tooltip("Time (seconds) before switching to next animation frame.")]
    public float frameDuration = 0.15f;

    // ------------------------------
    //  IDLE BREATHING EFFECT
    // ------------------------------

    [Header("Idle Breathing Effect")]
    [Tooltip("Breathing height change (recommended 0.02–0.05).")]
    public float breatheAmplitude = 0.03f;

    [Tooltip("Breathing animation speed.")]
    public float breatheSpeed = 1.5f;

    // ------------------------------
    //  INTERNAL STATE (DO NOT TOUCH)
    // ------------------------------

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 movement;
    private float frameTimer = 0f;
    private int frameIndex = 0;

    private enum Direction { Down, Up, Right, Left }
    private Direction currentDir = Direction.Down;

    private Vector3 baseScale;  // Original scale to restore from breathing

    // ------------------------------
    //  UNITY METHODS
    // ------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale; // Cache original model size
    }

    private void Update()
    {

        int zero = 0;
        float zero_f = 0f;
        // ---------------------------------------
        //  INPUT HANDLING (New Input System)
        // ---------------------------------------

        movement = Vector2.zero;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) movement.x = -1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) movement.x = 1f;
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) movement.y = 1f;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) movement.y = -1f;
        }

        // Normalize diagonal movement to prevent faster speed
        if (movement.sqrMagnitude > diagonalNormalizeThreshold)
            movement = movement.normalized;

        bool isMoving = movement.sqrMagnitude > movementThreshold;

        // ---------------------------------------
        //  DIRECTION & ANIMATION FRAME CONTROL
        // ---------------------------------------

        if (isMoving)
        {
            // Decide whether we are mainly moving horizontally or vertically
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                currentDir = movement.x > zero ? Direction.Right : Direction.Left;
            else
                currentDir = movement.y > zero ? Direction.Up : Direction.Down;

            // Advance animation frames
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer = zero_f;
                frameIndex++;
            }
        }
        else
        {
            // When idle, reset animation to frame 0
            frameIndex = zero;
            frameTimer = zero_f;
        }

        // Apply correct sprite frame
        Sprite[] currentAnim = GetCurrentAnimArray();
        if (currentAnim != null && currentAnim.Length > zero)
        {
            frameIndex %= currentAnim.Length;
            sr.sprite = currentAnim[frameIndex];
        }

        // ---------------------------------------
        //  IDLE BREATHING ANIMATION
        // ---------------------------------------

        if (!isMoving)
        {
            float t = Time.time * breatheSpeed;
            float scaleOffset = 1f + Mathf.Sin(t) * breatheAmplitude;

            transform.localScale = new Vector3(
                baseScale.x,
                baseScale.y * scaleOffset,
                baseScale.z
            );
        }
        else
        {
            transform.localScale = baseScale; // No breathing when walking
        }
    }

    private void FixedUpdate()
    {
        // Physics-based movement
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // ------------------------------
    //  HELPER METHODS
    // ------------------------------

    /// <summary>
    /// Returns animation array according to current facing direction.
    /// </summary>
    private Sprite[] GetCurrentAnimArray()
    {
        switch (currentDir)
        {
            case Direction.Down: return walkDown;
            case Direction.Up: return walkUp;
            case Direction.Right: return walkRight;
            case Direction.Left: return walkLeft;
        }
        return null;
    }
}
