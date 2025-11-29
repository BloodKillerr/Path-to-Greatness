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

        if(GameManager.Instance.IsGameStatePaused)
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

    public void InteractEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!GameManager.Instance.IsGameStatePaused && !Player.Instance.IsDead)
            {
                Player.Instance.InteractEvent.Invoke();
            }
        }
    }

    public void PauseResumeEvent(InputAction.CallbackContext context)
    {
        if (context.performed && !Player.Instance.IsDead && !PauseMenu.Instance.RebindingUI.activeSelf && !UIManager.Instance.MessageOpen)
        {
            UIManager.Instance.ToogleMenu(MenuType.PAUSE);
        }
    }

    public void CharacterEvent(InputAction.CallbackContext context)
    {
        if (context.performed && !Player.Instance.IsDead && !UIManager.Instance.MessageOpen)
        {
            switch(UIManager.Instance.CurrentMenuType)
            {
                case MenuType.STATUS:
                    UIManager.Instance.ToogleMenu(MenuType.STATUS);
                    break;
                case MenuType.ABILITIES:
                    UIManager.Instance.ToogleMenu(MenuType.ABILITIES);
                    break;
                case MenuType.QUESTS:
                    UIManager.Instance.ToogleMenu(MenuType.QUESTS);
                    break;
                case MenuType.NONE:
                    UIManager.Instance.ToogleMenu(MenuType.STATUS);
                    break;
            }
        }
    }

    public void FirstAbilityEvent(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            if (GameManager.Instance.IsGameStatePaused)
            {
                return;
            }

            AbilityManager.Instance.ActivateBoundAbility(0);
        }
    }

    public void SecondAbilityEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameManager.Instance.IsGameStatePaused)
            {
                return;
            }

            AbilityManager.Instance.ActivateBoundAbility(1);
        }
    }

    public void ThirdAbilityEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameManager.Instance.IsGameStatePaused)
            {
                return;
            }

            AbilityManager.Instance.ActivateBoundAbility(2);
        }
    }

    public void FourthAbilityEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameManager.Instance.IsGameStatePaused)
            {
                return;
            }

            AbilityManager.Instance.ActivateBoundAbility(3);
        }
    }
}
