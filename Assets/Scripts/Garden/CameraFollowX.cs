using UnityEngine;

public class CameraFollowX : MonoBehaviour
{
    /// <summary>
    /// The transform the camera will follow (usually the Player).
    /// </summary>
    public Transform target;

    /// <summary>
    /// Horizontal offset from the player.
    /// Useful when you want the player to appear slightly left/right on screen
    /// instead of centered exactly.
    /// </summary>
    public float offsetX = 0f;

    /// <summary>
    /// Smoothness of the camera movement.
    /// 0 = instant snap, high values = slower and smoother follow.
    /// </summary>
    public float smooth = 5f;

    /// <summary>
    /// LateUpdate is used to update the camera
    /// after the player has moved for the current frame,
    /// preventing jitter or lag from physics or animations.
    /// </summary>
    void LateUpdate()
    {
        // If no target is assigned, do nothing.
        if (target == null)
            return;

        /// <summary>
        /// Copy the current camera position (so we keep Y and Z unchanged).
        /// </summary>
        Vector3 pos = transform.position;

        /// <summary>
        /// Compute the desired horizontal position:
        /// player's X location + optional offset.
        /// </summary>
        float targetX = target.position.x + offsetX;

        /// <summary>
        /// Smoothly interpolate (lerp) only along the X axis.
        /// Keeps camera motion smooth and avoids sudden jumps.
        /// </summary>
        pos.x = Mathf.Lerp(pos.x, targetX, smooth * Time.deltaTime);

        /// <summary>
        /// Apply the updated position back to the camera.
        /// </summary>
        transform.position = pos;
    }
}
