using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    private PlayerState playerState;

    private float sprintMaxBlendValue = 1.5f;
    private float runMaxBlendValue = 1f;
    private float walkMaxBlendValue = .5f;

    private static int inputXHash = Animator.StringToHash("InputX");
    private static int inputYHash = Animator.StringToHash("InputY");
    private static int inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private static int rotationMismatchHash = Animator.StringToHash("RotationMismatch");
    private static int isIdleHash = Animator.StringToHash("IsIdle");
    private static int isGroundedHash = Animator.StringToHash("IsGrounded");
    private static int isFallingHash = Animator.StringToHash("IsFalling");
    private static int isJumpingHash = Animator.StringToHash("IsJumping");
    private static int isRotatingToTargetHash = Animator.StringToHash("IsRotatingToTarget");

    private Vector3 currentBlendInput = Vector3.zero;

    [SerializeField] private float LocomotionBlendSpeed = 7f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        bool isIdle = playerState.CurrentPlayerMovementState == PlayerMovementState.Idle;
        bool isRunning = playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
        bool isSprinting = playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isJumping = playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
        bool isFalling = playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
        bool isGrounded = playerState.InGroundedState();

        bool isRunBlendValue = isRunning || isJumping || isFalling;
        Vector2 inputTarget = isSprinting ? playerController.MovementInput * sprintMaxBlendValue :
            isRunBlendValue ? playerController.MovementInput * runMaxBlendValue : playerController.MovementInput * walkMaxBlendValue;
        currentBlendInput = Vector3.Lerp(currentBlendInput, inputTarget, LocomotionBlendSpeed * Time.deltaTime);

        animator.SetBool(isIdleHash, isIdle);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isFallingHash, isFalling);
        animator.SetBool(isJumpingHash, isJumping);
        animator.SetBool(isRotatingToTargetHash, playerController.IsRotatingToTarget);

        animator.SetFloat(inputXHash, currentBlendInput.x);
        animator.SetFloat(inputYHash, currentBlendInput.y);
        animator.SetFloat(inputMagnitudeHash, currentBlendInput.magnitude);
        animator.SetFloat(rotationMismatchHash, playerController.RotationMismatch);
    }
}
