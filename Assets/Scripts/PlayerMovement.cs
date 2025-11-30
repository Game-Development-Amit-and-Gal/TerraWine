using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkRight;
    public Sprite[] walkLeft;

    public float frameDuration = 0.15f; // כמה זמן כל פריים מוצג

    // פרמטרים לנשימה
    public float breatheAmplitude = 0.03f; // כמה חזק הנשימה (0.02–0.05 זה עדין)
    public float breatheSpeed = 1.5f;      // מהירות הנשימה

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 movement;
    private float frameTimer = 0f;
    private int frameIndex = 0;

    private enum Direction { Down, Up, Right, Left }
    private Direction currentDir = Direction.Down;

    private Vector3 baseScale; // הגודל המקורי של הדמות

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale; // שומרים את הגודל ההתחלתי
    }

    void Update()
    {
        // קלט מה־Input System החדש
        movement = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                movement.x = -1f;

            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                movement.x = 1f;

            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                movement.y = 1f;

            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                movement.y = -1f;
        }

        // נורמליזציה לאלכסון
        if (movement.sqrMagnitude > 1f)
            movement = movement.normalized;

        bool isMoving = movement.sqrMagnitude > 0.01f;

        // עדכון כיוון לפי התנועה
        if (isMoving)
        {
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                currentDir = movement.x > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                currentDir = movement.y > 0 ? Direction.Up : Direction.Down;
            }

            // עדכון פריים של ההליכה
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer = 0f;
                frameIndex++;
            }
        }
        else
        {
            // עומדת – תמיד פריים ראשון
            frameIndex = 0;
            frameTimer = 0f;
        }

        // בחירה של הספרייט הנכון לפי כיוון ופריים
        Sprite[] currentAnim = GetCurrentAnimArray();
        if (currentAnim != null && currentAnim.Length > 0)
        {
            frameIndex %= currentAnim.Length;
            sr.sprite = currentAnim[frameIndex];
        }

        // ---------- נשימה ----------
        if (!isMoving)
        {
            // נשימה עדינה בציר ה-Y
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
            // בזמן הליכה – גודל רגיל
            transform.localScale = baseScale;
        }
        // ----------------------------
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

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
