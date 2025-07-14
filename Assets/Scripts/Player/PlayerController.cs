using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintInput;
    private bool walkInput;

    private float currentSpeed;
    private float speedVelocity;
    private float turnSmoothVelocity;
    private float verticalVelocity;
    private bool groundedLastFrame = true;
    private bool isLanding = false;
    private float lastGroundedY;
    private float fallStartY;
    private bool bigFall = false;

    public float WalkSpeed = 3f;
    public float JogSpeed = 6f;
    public float RunSpeed = 9f;
    public float AccelerationTime = 0.2f;
    public float DecelerationTime = 0.05f;
    public float Gravity = -15f;
    public float MinLandHeight = 1.5f;

    public Transform CameraTransform;
    public float RotationSmoothTime = 0.1f;

    public Vector2 MoveInput { get => moveInput; set => moveInput = value; }
    public Vector2 LookInput { get => lookInput; set => lookInput = value; }
    public bool SprintInput { get => sprintInput; set => sprintInput = value; }
    public bool WalkInput { get => walkInput; set => walkInput = value; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        verticalVelocity = -2f;
        groundedLastFrame = true;
        lastGroundedY = transform.position.y;
    }

    void Update()
    {
        bool justLanded = MoveAndComputeLanding();

        if (justLanded)
        {
            isLanding = bigFall;
        }

        bool realGrounded = characterController.isGrounded;
        float realVerticalVelocity = verticalVelocity;

        bool animatorGrounded;
        float animatorVerticalVelocity;

        if (!bigFall)
        {
            animatorGrounded = true;
            animatorVerticalVelocity = 0f;
        }
        else
        {
            animatorGrounded = realGrounded;
            animatorVerticalVelocity = justLanded ? 0f : realVerticalVelocity;
        }
        animator.SetBool("isGrounded", animatorGrounded);

        animator.SetFloat("verticalVelocity", animatorVerticalVelocity);

        if (isLanding)
        {
            animator.SetFloat("speed", 0f);
            return;
        }

        animator.SetFloat("speed", currentSpeed);
    }

    public void EndLanding()
    {
        isLanding = false;
    }

    bool MoveAndComputeLanding()
    {
        Vector3 horizontal = Vector3.zero;
        if (!isLanding)
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + CameraTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0, angle, 0);

                float baseSpeed;
                if (sprintInput)
                {
                    baseSpeed = RunSpeed;
                }
                else if (walkInput)
                {
                    baseSpeed = WalkSpeed;
                }
                else
                {
                    baseSpeed = (inputDirection.magnitude < 0.5f) ? WalkSpeed : JogSpeed;
                }

                float targetSpeed = baseSpeed * inputDirection.magnitude;
                float smoothTime = (targetSpeed > currentSpeed) ? AccelerationTime : DecelerationTime;
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, smoothTime);

                if (inputDirection.magnitude < 0.1f && currentSpeed < 0.1f)
                {
                    currentSpeed = 0f; speedVelocity = 0f;
                }

                horizontal = Quaternion.Euler(0, angle, 0) * Vector3.forward * currentSpeed;
            }
            else
            {
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, DecelerationTime);
                if (currentSpeed < 0.1f)
                {
                    currentSpeed = 0f; speedVelocity = 0f;
                }
            }
        }

        float previousY = transform.position.y;
        if (!characterController.isGrounded)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }  

        if (groundedLastFrame && !characterController.isGrounded)
        {
            fallStartY = previousY;
            bigFall = false;
        }

        if (!characterController.isGrounded)
        {
            if (fallStartY - previousY > MinLandHeight)
            {
                bigFall = true;
            }   
        }

        characterController.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

        bool groundedNow = characterController.isGrounded;
        bool justLandedNow = !groundedLastFrame && groundedNow;
        groundedLastFrame = groundedNow;

        if (justLandedNow)
        {
            lastGroundedY = transform.position.y;
            isLanding = false;
        }   

        if (groundedNow && !justLandedNow)
        {
            verticalVelocity = -2f;
        }   

        return justLandedNow;
    }
}
