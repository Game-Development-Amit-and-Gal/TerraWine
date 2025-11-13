using UnityEngine;
using UnityEngine.InputSystem;

public class OnClickManager : MonoBehaviour
{
    [SerializeField] private InputAction DayPassOnClick = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
    [SerializeField] private SeasonManager seasonManager;


    private void OnEnable()
    {
        DayPassOnClick.Enable();

    }

    private void OnDisable()
    {
        DayPassOnClick.Disable();
    }

    private void Update()
    {
        if (DayPassOnClick.WasPressedThisFrame())
        {
            seasonManager.AdvanceDay();
        }
    }

}
