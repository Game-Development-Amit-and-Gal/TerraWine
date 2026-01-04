using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MiniGamePlatformerController2D : MonoBehaviour
{
    private const float ZERO = 0f;
    private const float LEFT = -1f;
    private const float RIGHT = 1f;

    // Contact normal threshold: if the contact normal points up enough -> grounded
    private const float MIN_GROUND_NORMAL_Y = 0.5f;

    [Header("Scene Gate")]
    [SerializeField] private string miniGameScene = "ClosingWallMiniGame";

    [Header("Disable these in mini-game")]
    [SerializeField] private Behaviour[] disableInMiniGame;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float miniGravityScale = 3f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpVel = 10f;

    [Header("Frames")]
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite[] walkLeft;

    [Header("Timing")]
    [SerializeField] private float frameDuration = 0.12f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private int maxContacts = 16;

    private ContactFilter2D groundFilter;
    private ContactPoint2D[] contacts;

    

    private bool isMini;
    private float moveX;
    private bool jumpQueued;

    private float originalGravity;
    private RigidbodyConstraints2D originalConstraints;
    private bool cachedOriginal;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();

        contacts = new ContactPoint2D[maxContacts];
        groundFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = groundMask
        };
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        Apply(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (cachedOriginal) RestoreRb();
    }

    private void OnSceneChanged(Scene oldS, Scene newS) => Apply(newS);

    private void Apply(Scene s)
    {
        isMini = s.name == miniGameScene;

        foreach (var b in disableInMiniGame)
            if (b) b.enabled = !isMini;

        if (isMini) SetupRbForMini();
        else if (cachedOriginal) RestoreRb();

        moveX = ZERO;
        jumpQueued = false;
    }

    private void SetupRbForMini()
    {
        if (!rb) return;

        if (!cachedOriginal)
        {
            originalGravity = rb.gravityScale;
            originalConstraints = rb.constraints;
            cachedOriginal = true;
        }

        rb.gravityScale = miniGravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void RestoreRb()
    {
        if (!rb) return;
        rb.gravityScale = originalGravity;
        rb.constraints = originalConstraints;
    }

    private void Update()
    {
        if (!isMini) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        moveX = ZERO;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) moveX = LEFT;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) moveX = RIGHT;

        if (kb.spaceKey.wasPressedThisFrame)
            jumpQueued = true;
    }

    private void FixedUpdate()
    {
        if (!isMini || !rb) return;

        Vector2 v = rb.linearVelocity;
        v.x = moveX * moveSpeed;

        if (jumpQueued && IsGroundedByContacts())
            v.y = jumpVel;

        rb.linearVelocity = v;
        jumpQueued = false;
    }

    private bool IsGroundedByContacts()
    {
        int count = rb.GetContacts(groundFilter, contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y >= MIN_GROUND_NORMAL_Y)
                return true;
        }
        return false;
    }
}
