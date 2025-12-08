using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detects mouse clicks using the new Input System,
/// and tells the SeasonManager to move to the next day when clicked.
/// </summary>
public class OnClickManager : MonoBehaviour
{
    /// <summary>
    /// Input action that listens for a left mouse button click.
    /// Set as "Button" type and bound to "<Mouse>/leftButton".
    /// </summary>
    [SerializeField]
    private InputAction DayPassOnClick = new InputAction(
        type: InputActionType.Button,
        binding: "<Mouse>/leftButton"
    );

    /// <summary>
    /// Reference to the SeasonManager that controls time progression.
    /// </summary>
    [SerializeField]
    private SeasonManager seasonManager;

    private void OnEnable()
    {
        // Must enable InputActions manually or they won't work
        DayPassOnClick.Enable();
    }

    private void OnDisable()
    {
        // Disable actions when object is not active to avoid ghost input
        DayPassOnClick.Disable();
    }

    private void Update()
    {
        // Check if the left mouse button was pressed this frame
        if (DayPassOnClick.WasPressedThisFrame())
        {
            // Progress to the next day in the game world
            seasonManager.AdvanceDay();
        }
    }
}
