using UnityEngine;

public class CameraFollowX : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera will follow (usually the Player).")]
    public Transform target;

    [Header("Follow")]
    [Tooltip("Horizontal offset from the player (player appears slightly left/right).")]
    public float offsetX = 0f;

    [Tooltip("Smoothness of the camera movement. Higher = smoother, 0 = snap.")]
    public float smooth = 5f;

    [Header("Limits (Optional)")]
    [Tooltip("Enable X limits for the camera movement.")]
    public bool useLimits = false;

    [Tooltip("Minimum X value the camera can reach.")]
    public float minX = -10f;

    [Tooltip("Maximum X value the camera can reach.")]
    public float maxX = 10f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 pos = transform.position;

        // Desired X follow position
        float targetX = target.position.x + offsetX;

        // Optional clamp to limits
        if (useLimits)
            targetX = Mathf.Clamp(targetX, minX, maxX);

        // Smooth follow only on X
        if (smooth <= 0f)
            pos.x = targetX; // snap
        else
            pos.x = Mathf.Lerp(pos.x, targetX, smooth * Time.deltaTime);

        transform.position = pos;
    }

#if UNITY_EDITOR
    // Nice: show limits in Scene view when selected
    private void OnDrawGizmosSelected()
    {
        if (!useLimits) return;

        Gizmos.color = Color.yellow;

        // Draw two vertical lines at minX and maxX at camera's current Y
        float y = transform.position.y;
        float z = transform.position.z;

        Gizmos.DrawLine(new Vector3(minX, y - 50f, z), new Vector3(minX, y + 50f, z));
        Gizmos.DrawLine(new Vector3(maxX, y - 50f, z), new Vector3(maxX, y + 50f, z));
    }
#endif
}
