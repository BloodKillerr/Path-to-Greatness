using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerController playerController;

    [SerializeField] private bool HoldToSprint = true;
    [SerializeField] private bool HoldToWalk = true;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        playerController.JumpInput = false;
    }

    public void MoveEvent(InputAction.CallbackContext context)
    {
        playerController.MovementInput = context.ReadValue<Vector2>();
    }

    public void LookEvent(InputAction.CallbackContext context)
    {
        playerController.LookInput = context.ReadValue<Vector2>();
    }

    public void SprintEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerController.SprintInput = HoldToSprint || !playerController.SprintInput;
        }
        else if(context.canceled)
        {
            playerController.SprintInput = !HoldToSprint && playerController.SprintInput;
        }
    }

    public void JumpEvent(InputAction.CallbackContext context)
    {
        if(!context.performed)
        {
            return;
        }

        playerController.JumpInput = true;
    }

    public void WalkEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerController.WalkInput = HoldToWalk || !playerController.WalkInput;
        }
        else if (context.canceled)
        {
            playerController.WalkInput = !HoldToWalk && playerController.WalkInput;
        }
    }
}
