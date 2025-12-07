using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    /// <summary>
    /// The transform the camera should follow (usually the Player).
    /// </summary>
    public Transform target;

    /// <summary>
    /// Controls how smoothly the camera follows the target.
    /// Higher values = faster catch-up, lower = slower/softer motion.
    /// </summary>
    public float smoothSpeed = 5f;

    /// <summary>
    /// LateUpdate is used because camera movement should happen
    /// after all other objects have already moved this frame.
    /// Ensures no jitter when following physics or animated movement.
    /// </summary>
    void LateUpdate()
    {
        // If there's no target assigned, do nothing.
        if (target == null)
            return;

        /// <summary>
        /// Create a desired camera position using the target's X and Y,
        /// but keep the camera's current Z (so it stays at the same depth).
        /// </summary>
        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        /// <summary>
        /// Smoothly interpolate (lerp) between the current camera position
        /// and the desired position based on smoothSpeed and deltaTime.
        /// This produces a smooth camera follow instead of snapping instantly.
        /// </summary>
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
