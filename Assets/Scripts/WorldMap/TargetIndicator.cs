using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles UI Arrow rotation and positioning to point towards a world-space target.
/// </summary>
public class TargetIndicator : MonoBehaviour
{
    [Header("References")]
    public Transform target;           // The object we want to point to (e.g., Young Cabernet text)
    private Camera mainCamera;          // Reference to the main camera

    [Header("Settings")]
    public bool lockDirection = false; // Toggle to lock rotation
    public float lockedAngle = 0f;    // The fixed angle if locked
    public float rotationOffset = -90f; // Offset depending on how your arrow sprite is drawn (default assumes pointing right)

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (target == null) return;

        UpdateArrowRotation();
    }

    /// <summary>
    /// Calculates and applies the rotation to point at the target.
    /// </summary>
    private void UpdateArrowRotation()
    {
        if (lockDirection)
        {
            // Option to lock the arrow from a specific direction regardless of movement
            rectTransform.rotation = Quaternion.Euler(0, 0, lockedAngle);
            return;
        }

        // 1. Get the target's position in screen space (pixels)
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.position);

        // 2. Get the arrow's own screen position
        Vector3 arrowScreenPos = transform.position;

        // 3. Calculate direction vector from arrow to target
        Vector2 direction = (Vector2)targetScreenPos - (Vector2)arrowScreenPos;

        // 4. Calculate the angle in radians, then convert to degrees
        // Atan2 handles the X=0 case (vertical alignment) automatically
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 5. Apply the rotation on the Z axis with the sprite offset
        rectTransform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    /// <summary>
    /// Call this from other scripts to change the target and optionally lock the view.
    /// </summary>
    public void SetTarget(Transform newTarget, bool shouldLock = false, float angle = 0f)
    {
        target = newTarget;
        lockDirection = shouldLock;
        lockedAngle = angle;
    }
}