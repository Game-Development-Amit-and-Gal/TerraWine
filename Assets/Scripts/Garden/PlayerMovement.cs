using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles top-down player movement using the new Input System,
/// plays directional walking animations,
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

    [Header("Auto Move Animation LookAhead")]
    [Tooltip("כמה נקודות קדימה לחשב את כיוון האנימציה (לא את הפיזיקה).")]
    [SerializeField] private int animLookAhead = 6;

    // ------------------------------
    //  FEET / GROUND OFFSET
    // ------------------------------

    [Header("Feet / Ground Offset")]
    [Tooltip("Offset from Rigidbody position to the character's feet (Y negative = down).")]
    [SerializeField] private Vector2 feetOffset = new Vector2(0f, -0.5f);

    /// <summary> World position of the character's feet (rb.position + offset). </summary>
    public Vector2 FeetPosition => rb.position + feetOffset;

    // ------------------------------
    //  DEBUG – PATH DRAWING
    // ------------------------------

    [Header("Debug Path")]
    [SerializeField] private bool drawPathGizmos = true;

    // Stores the last path for drawing in SceneView
    private readonly List<Vector3> debugPathPoints = new List<Vector3>();

    // ------------------------------
    //  PATH STATE (LIST + INDEX)
    // ------------------------------

    private readonly List<Vector3> activePath = new List<Vector3>();
    private int pathIndex = 0;

    private bool hasPath = false;

    // For animation direction stability (update only when pathIndex changes)
    private int lastAnimPathIndex = -1;
    private Vector2 cachedAnimMovement = Vector2.zero;

    // Optional debug targets
    private Vector3 currentMoveTarget;
    private Vector3 currentLookTarget;

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
    //  INTERNAL STATE
    // ------------------------------

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    /// <summary> Movement direction for PHYSICS (actual movement). </summary>
    private Vector2 desiredMovement;

    /// <summary> Movement direction for ANIMATION (visual intent). </summary>
    private Vector2 animMovement;

    private float frameTimer = 0f;
    private int frameIndex = 0;

    private enum Direction { Down, Up, Right, Left }
    private Direction currentDir = Direction.Down;

    private Vector3 baseScale;

    // ------------------------------
    //  TUTORIAL MOVEMENT GATE
    // ------------------------------

    [Header("Tutorial Gate")]
    [Tooltip("If true: allows arrow-key movement even while tutorial is running.")]
    [SerializeField] private bool allowMoveDuringTutorial = false;

    [Tooltip("If true: blocks auto-move/path while tutorial is running.")]
    [SerializeField] private bool blockAutoMoveDuringTutorial = true;
    [SerializeField] private string moveWithArrowsFlagName = "Move arrow keys";
    private bool moveFlagSent = false;

    /// <summary>
    /// Call from TutorialManager when you want to allow / disallow movement during a tutorial step.
    /// </summary>
    public void SetAllowMoveDuringTutorial(bool allow)
    {
        allowMoveDuringTutorial = allow;
        if (allow) moveFlagSent = false;


        if (!allow)
        {
            // stop movement immediately
            desiredMovement = Vector2.zero;
            animMovement = Vector2.zero;

            // optionally stop auto move
            if (blockAutoMoveDuringTutorial)
            {
                hasPath = false;
                useAutoMove = false;
                activePath.Clear();
                pathIndex = 0;
                cachedAnimMovement = Vector2.zero;
                lastAnimPathIndex = -1;
            }
        }
    }

    // ------------------------------
    //  PUBLIC API
    // ------------------------------

    public void SetPath(List<Vector3> worldPath)
    {
        activePath.Clear();
        debugPathPoints.Clear();

        pathIndex = 0;
        lastAnimPathIndex = -1;
        cachedAnimMovement = Vector2.zero;

        if (worldPath == null || worldPath.Count == 0)
        {
            Debug.Log("[PlayerMovement] SetPath called with NULL or EMPTY path.");
            hasPath = false;
            useAutoMove = false;
            return;
        }

        activePath.AddRange(worldPath);
        debugPathPoints.AddRange(worldPath);

        hasPath = true;
        useAutoMove = true;

        Debug.Log("[PlayerMovement] SetPath with " + worldPath.Count +
                  " points. First=" + worldPath[0] +
                  " Last=" + worldPath[worldPath.Count - 1]);
    }

    public void SetAutoMoveTarget(Vector3 worldTarget)
    {
        SetPath(new List<Vector3> { worldTarget });
    }

    // ------------------------------
    //  UNITY
    // ------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (TutorialManager.tutorialIsRunning && !allowMoveDuringTutorial)
            return;


        HandleInputOrAutoMove();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (TutorialManager.tutorialIsRunning && !allowMoveDuringTutorial)
            return;
        ApplyMovement();
        
    }

    // ------------------------------
    //  INPUT / AUTO MOVE
    // ------------------------------

    // הופך וקטור לכאילו "לחיצות מקשים" (-1/0/1 לכל ציר), ומשאיר אלכסון נורמליזד
    private Vector2 QuantizeLikeKeys(Vector2 v)
    {
        float x = Mathf.Abs(v.x) > 0.001f ? Mathf.Sign(v.x) : 0f;
        float y = Mathf.Abs(v.y) > 0.001f ? Mathf.Sign(v.y) : 0f;

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude > 1f) dir = dir.normalized;
        return dir;
    }

    private void HandleInputOrAutoMove()
    {
        desiredMovement = Vector2.zero;
        animMovement = Vector2.zero;
        if (TutorialManager.tutorialIsRunning && allowMoveDuringTutorial && blockAutoMoveDuringTutorial)
        {
            hasPath = false;
            useAutoMove = false;
            activePath.Clear();
            pathIndex = 0;
            cachedAnimMovement = Vector2.zero;
            lastAnimPathIndex = -1;
        }

        // -------- מצב אוטומטי: תנועה על הצירים, אנימציה "כאילו מקשים" --------
        if (hasPath && useAutoMove && activePath.Count > 0)
        {
            Vector2 feetPos = FeetPosition;

            // להתקדם במסלול אם כבר הגענו לנקודה הנוכחית
            while (pathIndex < activePath.Count)
            {
                Vector2 p = new Vector2(activePath[pathIndex].x, activePath[pathIndex].y);
                if (Vector2.Distance(feetPos, p) > autoMoveStopDistance)
                    break;

                pathIndex++;
            }

            // סיימנו מסלול
            if (pathIndex >= activePath.Count)
            {
                hasPath = false;
                useAutoMove = false;
                desiredMovement = Vector2.zero;
                animMovement = Vector2.zero;
                cachedAnimMovement = Vector2.zero;
                return;
            }

            // 1) MOVEMENT (פיזיקה): לכיוון הנקודה הבאה בלבד (זה שומר על הליכה על הצירים)
            currentMoveTarget = activePath[pathIndex];
            Vector2 nextTarget = new Vector2(currentMoveTarget.x, currentMoveTarget.y);
            Vector2 toNext = nextTarget - feetPos;

            float dist = toNext.magnitude;
            if (dist < minDistanceForDirection) dist = minDistanceForDirection;

            // ברוב המקרים זה יהיה (1,0) או (0,1) כי המסלול הוא 4-כיוונים
            desiredMovement = toNext / dist;

            // 2) ANIMATION (ויזואלי): מחשבים קדימה רק כשנכנסים ל-node חדש
            if (pathIndex != lastAnimPathIndex)
            {
                int lookIndex = Mathf.Min(pathIndex + animLookAhead, activePath.Count - 1);
                currentLookTarget = activePath[lookIndex];

                Vector2 lookTarget2D = new Vector2(currentLookTarget.x, currentLookTarget.y);
                Vector2 toLook = lookTarget2D - feetPos;

                // זה ייתן למשל למעלה+ימינה גם אם פיזית כרגע הולכים רק ימינה
                cachedAnimMovement = QuantizeLikeKeys(toLook);
                lastAnimPathIndex = pathIndex;
            }

            // מחזיקים כיוון אנימציה יציב בין tiles
            animMovement = cachedAnimMovement;

            // אם משום מה יצא אפס - נפולבק לכיוון התנועה הפיזי
            if (animMovement == Vector2.zero)
                animMovement = QuantizeLikeKeys(desiredMovement);

            // נורמליזציה של אלכסון (למקרה שהמסלול שלך כן מחזיר אלכסון)
            if (desiredMovement.sqrMagnitude > diagonalNormalizeThreshold)
                desiredMovement = desiredMovement.normalized;

            return;
        }

        // -------- מצב חופשי – מקלדת --------
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) desiredMovement.x = -1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) desiredMovement.x = 1f;
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) desiredMovement.y = 1f;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) desiredMovement.y = -1f;
        }
        // --- NEW: fire tutorial flag once when player actually tries to move with keys ---
        if (!moveFlagSent &&
            TutorialManager.tutorialIsRunning &&
            allowMoveDuringTutorial &&
            desiredMovement.sqrMagnitude > 0.0001f)
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.SetFlag(moveWithArrowsFlagName);

            moveFlagSent = true;
        }


        // נורמליזציה של אלכסון מהמקלדת
        if (desiredMovement.sqrMagnitude > diagonalNormalizeThreshold)
            desiredMovement = desiredMovement.normalized;

        // במצב ידני, האנימציה עוקבת אחרי הקלט בפועל
        animMovement = desiredMovement;

        // איפוס cache כדי שלא "יישאר" כיוון אנימציה מהאוטומט
        lastAnimPathIndex = -1;
        cachedAnimMovement = Vector2.zero;
    }

    // ------------------------------
    //  MOVEMENT PHYSICS
    // ------------------------------

    private void ApplyMovement()
    {
        Vector2 step = desiredMovement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + step);

        // שמירה על אותו Z
        transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);
    }

    // ------------------------------
    //  ANIMATION
    // ------------------------------

    private void HandleAnimation()
    {
        const float zeroFloat = 0f;
        const int zeroInt = 0;

        // חשוב: האנימציה מתבססת על animMovement ולא על desiredMovement
        bool isMoving = animMovement.sqrMagnitude > (movementThreshold * movementThreshold);

        if (isMoving)
        {
            if (Mathf.Abs(animMovement.x) > Mathf.Abs(animMovement.y))
                currentDir = animMovement.x > zeroFloat ? Direction.Right : Direction.Left;
            else
                currentDir = animMovement.y > zeroFloat ? Direction.Up : Direction.Down;

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

        transform.localScale = baseScale;
    }

    private Sprite[] GetCurrentAnimArray()
    {
        Sprite[] fallback = (walkDown != null && walkDown.Length > 0) ? walkDown : null;
        Sprite[] result = null;

        switch (currentDir)
        {
            case Direction.Down:
                result = (walkDown != null && walkDown.Length > 0) ? walkDown : fallback;
                break;

            case Direction.Up:
                result = (walkUp != null && walkUp.Length > 0) ? walkUp : fallback;
                break;

            case Direction.Right:
                result = (walkRight != null && walkRight.Length > 0) ? walkRight : fallback;
                break;

            case Direction.Left:
                result = (walkLeft != null && walkLeft.Length > 0) ? walkLeft : walkRight;
                if (result == null || result.Length == 0) result = fallback;
                break;
        }

        return result;
    }

    // ------------------------------
    //  GIZMOS – ציור המסלול
    // ------------------------------

    private void OnDrawGizmos()
    {
        if (!drawPathGizmos) return;

        if (debugPathPoints != null && debugPathPoints.Count > 0)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < debugPathPoints.Count; i++)
            {
                Gizmos.DrawSphere(debugPathPoints[i], 0.06f);
                if (i < debugPathPoints.Count - 1)
                    Gizmos.DrawLine(debugPathPoints[i], debugPathPoints[i + 1]);
            }
        }

        // יעד תנועה (ה-node הבא) בצהוב
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentMoveTarget, 0.10f);

        // יעד look-ahead (רק לכיוון אנימציה) באדום
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentLookTarget, 0.10f);
    }
}
