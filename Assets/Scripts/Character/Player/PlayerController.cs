using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;

    private Vector2 movementInput;
    private Vector2 lookInput;
    private bool sprintInput;
    private bool jumpInput;
    private bool walkInput;

    private Vector2 cameraRotation = Vector2.zero;
    private Vector2 playerTargetRotation = Vector2.zero;

    private float verticalVelocity = 0f;
    private float antiBump;
    private bool jumpedLastFrame = false;
    private float stepOffset;

    private float rotationMismatch = 0f;
    private bool isRotatingToTarget = false;
    private float rotatingToTargetTimer = 0f;
    private bool isRotatingClockwise = false;

    private PlayerState playerState;

    private PlayerMovementState lastMovementState = PlayerMovementState.Falling;

    [Header("Movement")]
    public float WalkAcceleration = 25f;
    public float WalkSpeed = 2f;
    public float RunAcceleration = 35f;
    public float RunSpeed = 4f;
    public float SprintAcceleration = 50f;
    public float SprintSpeed = 6f;
    public float InAirAcceleration = 15f;
    public float InAirDrag = 10f;
    public float Drag = 20f;
    public float MovingThreshold = .01f;
    public float Gravity = 15f;
    public float TerminalVelocity = 50f;
    public float JumpSpeed = .8f;

    [Header("Camera")]
    public float LookSensitivityHorizontal = 1.3f;
    public float LookSensitivityVertical = 1.3f;
    public float PositiveLookLimitVertical = 30f;
    public float NegativeLookLimitVertical = -20f;
    public Camera PlayerCamera;

    [Header("Animation")]
    public float PlayerModelRotationSpeed = 10f;
    public float RotateToTargetTime = .67f;

    [Header("Environment Details")]
    public LayerMask groundLayers;

    public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
    public Vector2 LookInput { get => lookInput; set => lookInput = value; }
    public bool SprintInput { get => sprintInput; set => sprintInput = value; }
    public bool JumpInput { get => jumpInput; set => jumpInput = value; }
    public float RotationMismatch { get => rotationMismatch; set => rotationMismatch = value; }
    public bool IsRotatingToTarget { get => isRotatingToTarget; set => isRotatingToTarget = value; }
    public bool WalkInput { get => walkInput; set => walkInput = value; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
        antiBump = SprintSpeed;
        stepOffset = characterController.stepOffset;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        UpdateMovementState();
        HandleVerticalMovement();
        HandleLateralMovement();
    }

    private void LateUpdate()
    {
        UpdateCameraRotation();
    }

    private void UpdateCameraRotation()
    {
        cameraRotation.x += LookSensitivityHorizontal * lookInput.x;
        cameraRotation.y = Mathf.Clamp(cameraRotation.y - LookSensitivityVertical * lookInput.y, NegativeLookLimitVertical, PositiveLookLimitVertical);

        playerTargetRotation.x += transform.eulerAngles.x + LookSensitivityHorizontal * lookInput.x;

        float rotationTolerance = 90f;
        bool isIdle = playerState.CurrentPlayerMovementState == PlayerMovementState.Idle;
        isRotatingToTarget = rotatingToTargetTimer > 0f;

        if (!isIdle)
        {
            RotatePlayerToTarget();
        }
        else if (Mathf.Abs(rotationMismatch) > rotationTolerance || isRotatingToTarget)
        {
            UpdateIdleRotation(rotationTolerance);
        }


        PlayerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0f);

        Vector3 cameraForwardProjectedXZ = new Vector3(PlayerCamera.transform.forward.x, 0f, PlayerCamera.transform.forward.z).normalized;
        Vector3 crossProduct = Vector3.Cross(transform.forward, cameraForwardProjectedXZ);
        float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
        rotationMismatch = sign * Vector3.Angle(transform.forward, cameraForwardProjectedXZ);
    }

    private void UpdateIdleRotation(float rotationTolerance)
    {
        if (Mathf.Abs(rotationMismatch) > rotationTolerance)
        {
            rotatingToTargetTimer = RotateToTargetTime;
            isRotatingClockwise = rotationMismatch > rotationTolerance;
        }
        rotatingToTargetTimer -= Time.deltaTime;

        if (isRotatingClockwise && rotationMismatch > 0f ||
            !isRotatingClockwise && rotationMismatch < 0f)
        {
            RotatePlayerToTarget();
        }
    }

    private void RotatePlayerToTarget()
    {
        Quaternion targetRotationX = Quaternion.Euler(0f, playerTargetRotation.x, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, PlayerModelRotationSpeed * Time.deltaTime);
    }

    private void UpdateMovementState()
    {
        lastMovementState = playerState.CurrentPlayerMovementState;

        bool canRun = CanRun();
        bool isMovementInput = movementInput != Vector2.zero;
        bool isMovingLaterally = IsMovingLaterally();
        bool isSprinting = sprintInput && isMovingLaterally;
        bool isWalking = isMovingLaterally && (!canRun || walkInput);
        bool isGrounded = IsGrounded();

        PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking : 
            isSprinting ? PlayerMovementState.Sprinting :
            isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idle;

        playerState.SetPlayerMovementState(lateralState);

        if((!isGrounded || jumpedLastFrame) && characterController.velocity.y > 0f)
        {
            playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
            jumpedLastFrame = false;
            characterController.stepOffset = 0f;
        }
        else if((!isGrounded || jumpedLastFrame) && characterController.velocity.y <= 0f)
        {
            playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            jumpedLastFrame = false;
            characterController.stepOffset = 0f;
        }
        else
        {
            characterController.stepOffset = stepOffset;
        }
    }

    private bool IsMovingLaterally()
    {
        Vector3 lateralVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);

        return lateralVelocity.magnitude > MovingThreshold;
    }

    private bool IsGrounded()
    {
        bool grounded = playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

        return grounded;
    }

    private bool IsGroundedWhileGrounded()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - characterController.radius, transform.position.z);

        bool grounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayers, QueryTriggerInteraction.Ignore);

        return grounded;
    }

    private bool IsGroundedWhileAirborne()
    {
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        bool validAngle = angle <= characterController.slopeLimit;

        return characterController.isGrounded && validAngle;
    }

    private bool CanRun()
    {
        return movementInput.y >= Mathf.Abs(movementInput.x);
    }

    private void HandleLateralMovement()
    {
        bool isSprinting = playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isGrounded = playerState.InGroundedState();
        bool isWalking = playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;

        float lateralAcceleration = !isGrounded ? InAirAcceleration :
            isWalking ? WalkAcceleration :
            isSprinting ? SprintAcceleration : RunAcceleration;
        float clampLateralMagnitude = !isGrounded ? SprintSpeed :
            isWalking ? WalkSpeed :
            isSprinting ? SprintSpeed : RunSpeed;

        Vector3 cameraForwardXZ = new Vector3(PlayerCamera.transform.forward.x, 0f, PlayerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(PlayerCamera.transform.right.x, 0f, PlayerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * movementInput.x + cameraForwardXZ * movementInput.y;

        Vector3 movementDelta = movementDirection * lateralAcceleration;
        Vector3 newVelocity = characterController.velocity + movementDelta;

        float dragMagnitude = isGrounded ? Drag : InAirDrag;
        Vector3 currentDrag = newVelocity.normalized * dragMagnitude * Time.deltaTime;
        newVelocity = (newVelocity.magnitude > dragMagnitude * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude);
        newVelocity.y += verticalVelocity;
        newVelocity = !isGrounded ? HandleSteepWalls(newVelocity) : newVelocity;

        characterController.Move(newVelocity * Time.deltaTime);
    }

    private void HandleVerticalMovement()
    {
        bool isGrounded = playerState.InGroundedState();

        verticalVelocity -= Gravity * Time.deltaTime;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -antiBump;
        }

        if(jumpInput && isGrounded)
        {
            verticalVelocity += Mathf.Sqrt(JumpSpeed * 3 * Gravity);
            jumpedLastFrame = true;
        }

        if (playerState.IsStateGroundedState(lastMovementState) && !isGrounded)
        {
            verticalVelocity += antiBump;
        }

        if(Mathf.Abs(verticalVelocity) > Mathf.Abs(TerminalVelocity))
        {
            verticalVelocity = -1f * Mathf.Abs(TerminalVelocity);
        }
    }

    private Vector3 HandleSteepWalls(Vector3 velocity)
    {
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        bool validAngle = angle <= characterController.slopeLimit;

        if(!validAngle && verticalVelocity < 0f)
        {
            velocity = Vector3.ProjectOnPlane(velocity, normal);
        }

        return velocity;
    }
}
