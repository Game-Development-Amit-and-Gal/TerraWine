// Assets/Scripts/MiniGames/ClosingWall/MiniGameFlipbookAnim.cs
using UnityEngine;

public class MiniGameFlipbookAnim : MonoBehaviour
{
    private const float ZERO = 0f;
    private const float MIN_MOVE_X = 0.05f;
    private const int ZERO_INT = 0;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;

    [Header("Frames")]
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite[] walkLeft;

    [Header("Timing")]
    [SerializeField] private float frameDuration = 0.12f;

    private float timer;
    private int frameIndex;
    private bool facingRight = true;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float vx = rb ? rb.linearVelocity.x : ZERO;

        if (vx > MIN_MOVE_X) facingRight = true;
        else if (vx < -MIN_MOVE_X) facingRight = false;

        bool moving = Mathf.Abs(vx) > MIN_MOVE_X;

        var frames = facingRight ? walkRight : walkLeft;
        if (frames == null || frames.Length == ZERO_INT) return;

        if (!moving)
        {
            sr.sprite = frames[ZERO_INT];   // idle = first frame
            timer = ZERO;
            frameIndex = ZERO_INT;
            return;
        }

        timer += Time.deltaTime;
        if (timer >= frameDuration)
        {
            timer = ZERO;
            frameIndex++;
            if (frameIndex >= frames.Length) frameIndex = ZERO_INT;
            sr.sprite = frames[frameIndex];
        }
    }
}
