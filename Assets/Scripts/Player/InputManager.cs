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

    public void SprintEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerController.IsSprinting = true;
        }
        else if(context.canceled)
        {
            playerController.IsSprinting = false;
        }
    }

    public void WalkEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerController.IsWalking = true;
        }
        else if (context.canceled)
        {
            playerController.IsWalking = false;
        }
    }
}
