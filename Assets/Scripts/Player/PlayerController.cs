using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private CharacterController characterController;
    private Vector2 moveInput;
    private bool isWalking;
    private bool isSprinting;
    private float turnSmoothVelocity;
    private float verticalVelocity = 0f;
    private bool wasGrounded = true;
    private float fallStartHeight = 0f;
    private bool isLanding = false;

    public Camera MainCamera;

    public float WalkSpeed = 2f;
    public float JogSpeed = 4f;
    public float RunSpeed = 6f;

    public float TurnSmoothTime = 0.1f;

    public float Gravity = -1f;
    public float GroundStickForce = 5f;
    public float GroundCheckDistance = 0.2f;

    public float FallThreshold = 3f;

    public Vector2 MoveInput { get => moveInput; set => moveInput = value; }
    public bool IsWalking { get => isWalking; set => isWalking = value; }
    public bool IsSprinting { get => isSprinting; set => isSprinting = value; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        bool grounded = characterController.isGrounded;

        if (grounded && !wasGrounded)
        {
            float fallDistance = fallStartHeight - transform.position.y;
            if (fallDistance > FallThreshold)
            {
                isLanding = true;
                animator.SetBool("IsFalling", false);
                animator.SetTrigger("Land");
            }
        }
        else if (!grounded && wasGrounded)
        {
            fallStartHeight = transform.position.y;
        }

        wasGrounded = grounded;

        if (isLanding)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Land") && state.normalizedTime >= 1f)
            {
                isLanding = false;
            }
            return;
        }

        if (!grounded)
        {
            float fallen = fallStartHeight - transform.position.y;
            if (fallen > FallThreshold)
            {
                animator.SetBool("IsFalling", true);
            }    
        }

        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -GroundStickForce;
        }
        else
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }

        Vector3 rawMove = Vector3.zero;
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;
            float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, TurnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

            float targetSpeed = isSprinting ? RunSpeed : isWalking ? WalkSpeed : JogSpeed;

            rawMove = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, characterController.height / 2 + GroundCheckDistance))
            {
                rawMove = Vector3.ProjectOnPlane(rawMove, hit.normal).normalized;
            }

            rawMove *= targetSpeed;
            animator.SetFloat("Speed", targetSpeed / RunSpeed, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        }

        Vector3 finalMove = rawMove + Vector3.up * verticalVelocity;
        characterController.Move(finalMove * Time.deltaTime);
    }
}
