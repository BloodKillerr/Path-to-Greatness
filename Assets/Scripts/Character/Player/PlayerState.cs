using UnityEngine;

public enum PlayerMovementState
{
    Idle = 0,
    Walking = 1,
    Running = 2,
    Sprinting = 3,
    Jumping = 4,
    Falling = 5,
    Strafing = 6,
}

public class PlayerState : MonoBehaviour
{
    public PlayerMovementState CurrentPlayerMovementState = PlayerMovementState.Idle;

    public void SetPlayerMovementState(PlayerMovementState playerMovementState)
    {
        CurrentPlayerMovementState = playerMovementState;
    }

    public bool InGroundedState()
    {
        return IsStateGroundedState(CurrentPlayerMovementState);
    }

    public bool IsStateGroundedState(PlayerMovementState movementState)
    {
        return movementState == PlayerMovementState.Idle ||
            movementState == PlayerMovementState.Walking ||
            movementState == PlayerMovementState.Running ||
            movementState == PlayerMovementState.Sprinting;
    }
}
