using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerController playerController;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
    }

    public void MoveEvent(InputAction.CallbackContext context)
    {
        playerController.MoveInput = context.ReadValue<Vector2>();
    }

    public void LookEvent(InputAction.CallbackContext context)
    {
        playerController.LookInput = context.ReadValue<Vector2>();
    }

    public void SprintEvent(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            playerController.SprintInput = true;
        }
        else if(context.canceled)
        {
            playerController.SprintInput = false;
        }
    }

    public void WalkEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerController.WalkInput = true;
        }
        else if (context.canceled)
        {
            playerController.WalkInput = false;
        }
    }
}
