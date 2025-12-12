using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles top-down player movement using the new Input System,
/// plays directional walking animations, adds an idle breathing effect,
/// and supports automatic movement along a path (for mini-map pathfinding).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    // ------------------------------
    //  MOVEMENT SETTINGS
    // ------------------------------

    [Header("Movement")]
    [Tooltip("Player movement speed in world units per second.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Minimum movement required to register movement (prevents tiny floating input).")]
    [SerializeField] private float movementThreshold = 0.01f;

    [Tooltip("Diagonal normalization threshold (when vector length is above this, normalize).")]
    [SerializeField] private float diagonalNormalizeThreshold = 1f;

    // ------------------------------
    //  AUTO MOVE (PATHFINDING)
    // ------------------------------

    [Header("Auto Move (Mini-Map / AI)")]
    [Tooltip("True while the player is currently following an automatic path.")]
    [SerializeField] private bool useAutoMove = false;

    [Tooltip("Distance from a path point at which we consider it 'reached' and move to the next one.")]
    [SerializeField] private float autoMoveStopDistance = 0.1f;

    [Tooltip("How close to zero distance we allow before dividing (safety against zero-length vectors).")]
    [SerializeField] private float minDistanceForDirection = 0.0001f;

    /// <summary>
    /// Queue of world positions that form the path.
    /// The player walks from one point to the next.
    /// </summary>
    private readonly Queue<Vector3> pathPoints = new Queue<Vector3>();

    /// <summary>
    /// The current point along the path that we are moving towards.
    /// </summary>
    private Vector3 currentPathTarget;

    /// <summary>
    /// True while we have a valid path to follow.
    /// </summary>
    private bool hasPath = false;

    // ------------------------------
    //  ANIMATION SETTINGS
    // ------------------------------

    [Header("Directional Sprite Animation")]
    [Tooltip("Animation frames for walking downward.")]
    [SerializeField] private Sprite[] walkDown;

    [Tooltip("Animation frames for walking upward.")]
    [SerializeField] private Sprite[] walkUp;

    [Tooltip("Animation frames for walking right.")]
    [SerializeField] private Sprite[] walkRight;

    [Tooltip("Animation frames for walking left.")]
    [SerializeField] private Sprite[] walkLeft;

    [Tooltip("Time (seconds) before switching to next animation frame.")]
    [SerializeField] private float frameDuration = 0.15f;

    // ------------------------------
    //  IDLE BREATHING EFFECT
    // ------------------------------

    [Header("Idle Breathing Effect")]
    [Tooltip("Breathing height change (recommended 0.02–0.05).")]
    [SerializeField] private float breatheAmplitude = 0.03f;

    [Tooltip("Breathing animation speed.")]
    [SerializeField] private float breatheSpeed = 1.5f;

    // ------------------------------
    //  INTERNAL STATE
    // ------------------------------

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    /// <summary>
    /// The movement vector decided in Update (keyboard or auto move).
    /// </summary>
    private Vector2 desiredMovement;

    /// <summary>
    /// The actual movement applied in FixedUpdate (used for animation).
    /// </summary>
    private Vector2 lastFrameMovement;

    private float frameTimer = 0f;
    private int frameIndex = 0;

    private enum Direction { Down, Up, Right, Left }
    private Direction currentDir = Direction.Down;

    private Vector3 baseScale;

    // ------------------------------
    //  PUBLIC API
    // ------------------------------

    /// <summary>
    /// Called by the pathfinding system (MiniMapClickToMove) to start
    /// automatically walking along a full path.
    /// </summary>
    /// <param name="worldPath">List of world positions (waypoints) from start to goal.</param>
    public void SetPath(List<Vector3> worldPath)
    {
        pathPoints.Clear();

        if (worldPath == null || worldPath.Count == 0)
        {
            Debug.Log("[PlayerMovement] SetPath called with NULL or EMPTY path.");
            hasPath = false;
            useAutoMove = false;
            return;
        }

        Debug.Log("[PlayerMovement] SetPath with " + worldPath.Count +
                  " points. First=" + worldPath[0] +
                  " Last=" + worldPath[worldPath.Count - 1]);

        // Enqueue all points in the path
        foreach (Vector3 point in worldPath)
        {
            pathPoints.Enqueue(point);
        }

        if (pathPoints.Count == 0)
        {
            Debug.Log("[PlayerMovement] After enqueue, pathPoints is EMPTY.");
            hasPath = false;
            useAutoMove = false;
            return;
        }

        // Take the first target
        currentPathTarget = pathPoints.Dequeue();
        Debug.Log("[PlayerMovement] First path target = " + currentPathTarget);

        hasPath = true;
        useAutoMove = true;
    }

    /// <summary>
    /// Backwards-compatible helper: move in a straight line to a single target,
    /// by creating a tiny "path" of one point.
    /// </summary>
    public void SetAutoMoveTarget(Vector3 worldTarget)
    {
        List<Vector3> singlePointPath = new List<Vector3> { worldTarget };
        SetPath(singlePointPath);
    }

    // ------------------------------
    //  UNITY METHODS
    // ------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        Debug.Log("[PlayerMovement] Awake on " + gameObject.name);
    }

    private void Update()
    {
        HandleInputOrAutoMove();
        HandleAnimationAndBreathing();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // ------------------------------
    //  LOGIC: INPUT / AUTO MOVE
    // ------------------------------

    private void HandleInputOrAutoMove()
    {
        desiredMovement = Vector2.zero;

        if (hasPath)
        {
            Vector2 currentPos = rb.position;
            Vector2 target2D = new Vector2(currentPathTarget.x, currentPathTarget.y);
            Vector2 toTarget = target2D - currentPos;
            float distance = toTarget.magnitude;

            Debug.Log($"[PlayerMovement] hasPath pos={currentPos}, target={target2D}, dist={distance}");

            // Reached the current point in the path
            if (distance <= autoMoveStopDistance)
            {
                if (pathPoints.Count > 0)
                {
                    // Move on to the next point in the path
                    currentPathTarget = pathPoints.Dequeue();
                    Debug.Log("[PlayerMovement] Reached path point, next target=" + currentPathTarget +
                              " remaining=" + pathPoints.Count);
                }
                else
                {
                    // Finished the whole path
                    Debug.Log("[PlayerMovement] Finished path.");
                    hasPath = false;
                    useAutoMove = false;
                    desiredMovement = Vector2.zero;
                    return;
                }
            }
            else
            {
                // Move towards currentPathTarget
                float safeDistance = Mathf.Max(distance, minDistanceForDirection);
                desiredMovement = toTarget / safeDistance; // normalized direction
                Debug.Log("[PlayerMovement] desiredMovement=" + desiredMovement);
            }
        }
        else
        {
            // Keyboard (new Input System)
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) desiredMovement.x = -1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) desiredMovement.x = 1f;
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) desiredMovement.y = 1f;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) desiredMovement.y = -1f;
            }

            // Normalize diagonal keyboard movement
            if (desiredMovement.sqrMagnitude > diagonalNormalizeThreshold)
                desiredMovement = desiredMovement.normalized;
        }
    }

    // ------------------------------
    //  LOGIC: MOVEMENT PHYSICS
    // ------------------------------

    private void ApplyMovement()
    {
        // Movement step this physics frame
        Vector2 step = desiredMovement * moveSpeed * Time.fixedDeltaTime;

        // Store for animation (direction & "is moving")
        lastFrameMovement = step;

        // Move with Rigidbody2D so collisions still work
        rb.MovePosition(rb.position + step);

        // Keep transform in sync on Z axis
        transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);

        Debug.Log("[PlayerMovement] ApplyMovement step=" + step + " newPos=" + rb.position);
    }

    // ------------------------------
    //  LOGIC: ANIMATION & BREATHING
    // ------------------------------

    private void HandleAnimationAndBreathing()
    {
        const float zeroFloat = 0f;
        const int zeroInt = 0;

        bool isMoving = lastFrameMovement.sqrMagnitude > (movementThreshold * movementThreshold);

        if (isMoving)
        {
            // Decide direction from last frame movement
            if (Mathf.Abs(lastFrameMovement.x) > Mathf.Abs(lastFrameMovement.y))
                currentDir = lastFrameMovement.x > zeroFloat ? Direction.Right : Direction.Left;
            else
                currentDir = lastFrameMovement.y > zeroFloat ? Direction.Up : Direction.Down;

            // Advance animation frames
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer = zeroFloat;
                frameIndex++;
            }
        }
        else
        {
            frameIndex = zeroInt;
            frameTimer = zeroFloat;
        }

        Sprite[] currentAnim = GetCurrentAnimArray();
        if (currentAnim != null && currentAnim.Length > zeroInt)
        {
            frameIndex %= currentAnim.Length;
            sr.sprite = currentAnim[frameIndex];
        }

        // Breathing when idle
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
            transform.localScale = baseScale;
        }
    }

    // ------------------------------
    //  HELPERS
    // ------------------------------

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
