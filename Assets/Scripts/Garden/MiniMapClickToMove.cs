using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Allows clicking on the mini-map (rendered by a separate camera)
/// to ask the pathfinder for a path and send it to PlayerMovement.
/// The actual movement and animation are handled by PlayerMovement.
/// </summary>
public class MiniMapClickToMove : MonoBehaviour
{
    [Header("References")]

    [Tooltip("Camera that renders the mini-map in a small region of the screen.")]
    [SerializeField] private Camera miniMapCamera;

    [Tooltip("PlayerMovement component that will receive the path to follow.")]
    [SerializeField] private PlayerMovement playerMovement;


    [Tooltip("Pathfinder that knows how to find a path on the grass area.")]
    [SerializeField] private TilemapPathfinder2D pathfinder;

    private void Update()
    {
        if (TutorialManager.tutorialIsRunning)
        {
            Debug.Log("Tutorial is still running due to bool value");
            return;
        }
        // Use the new Input System. If there is no mouse, do nothing.
        if (Mouse.current == null)
            return;

        // Check for left mouse button click.
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (miniMapCamera == null || playerMovement == null)
        {
            Debug.LogWarning("[MiniMapClickToMove] Missing miniMapCamera or playerMovement reference.");
            return;
        }



        // Mouse position in screen pixels.
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // Convert pixel position to normalized screen coordinates (0..1).
        Vector2 viewportPosition = new Vector2(
            mouseScreenPosition.x / Screen.width,
            mouseScreenPosition.y / Screen.height
        );

        // Only react if the click is inside the mini-map camera's viewport rectangle.
        if (!miniMapCamera.rect.Contains(viewportPosition))
            return;

        // Compute distance on Z axis between mini-map camera and player plane.
        float cameraToPlayerDistance = Mathf.Abs(
            miniMapCamera.transform.position.z - playerMovement.transform.position.z
        );

        // Build screen point including the correct depth.
        Vector3 screenPoint = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            cameraToPlayerDistance
        );

        // Convert to world space using the mini-map camera.
        Vector3 worldTarget = miniMapCamera.ScreenToWorldPoint(screenPoint);

        // Keep the player's original Z so depth does not change.
        worldTarget.z = playerMovement.transform.position.z;

        // ---------------- PATHFINDING PART ----------------

        if (pathfinder == null)
        {
            Debug.LogWarning("[MiniMapClickToMove] Pathfinder reference is missing.");
            return;
        }

        // Ask the pathfinder for a path from the player's FEET to the clicked target.
        List<Vector3> path = pathfinder.FindPath(
            playerMovement.FeetPosition,   // start from feet
            worldTarget                    // target position
        );

        if (path == null)
        {
            Debug.Log("[MiniMapClickToMove] FindPath returned NULL for target: " + worldTarget);
            return;
        }

        if (path.Count == 0)
        {
            Debug.Log("[MiniMapClickToMove] FindPath returned EMPTY path for target: " + worldTarget);
            return;
        }

        Debug.Log("[MiniMapClickToMove] Path found with " + path.Count +
                  " points. First=" + path[0] + ", Last=" + path[path.Count - 1]);

        // Give the full path to PlayerMovement (it will walk point by point).
        playerMovement.SetPath(path);
    }
}
