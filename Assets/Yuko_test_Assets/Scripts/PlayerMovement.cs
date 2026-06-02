using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera")]
    public PlayerLook playerLook;               // drag CameraPivot here

    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;

    [Header("Rotation")]
    public float rotationSpeed = 10.0f;         // how fast body turns to face camera yaw

    [Header("Jump")]
    public float jumpForce = 5.0f;
    public float jumpCooldown = 0.25f;
    public float airControlMultiplier = 0.4f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.15f;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45.0f;
    public float slopeSpeedMultiplier = 1.1f;

    [Header("Step Handling")]
    public float stepHeight = 0.35f;
    public float stepSearchDepth = 0.3f;

    // ── private references ───────────────────────────────────────────────
    private Rigidbody rb;
    private PlayerControls playerControls;
    private PlayerControls.PlayerActions playerInput;

    // ── private state ────────────────────────────────────────────────────
    private Vector2 moveInput;
    private bool runPressed;
    private bool jumpPressed;

    private bool isGrounded;
    private bool isOnSlope;
    private Vector3 slopeNormal;
    private float jumpCooldownTimer = 0.0f;

    private Vector3 stepLowerOriginOffset = new Vector3(0f, 0.05f, 0f);
    private Vector3 stepUpperOriginOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerControls = new PlayerControls();
        playerInput = playerControls.Player;
        rb.freezeRotation = true;
    }

    void OnEnable() => playerControls.Enable();
    void OnDisable() => playerControls.Disable();

    void Start()
    {
        stepUpperOriginOffset = new Vector3(0f, stepHeight + 0.05f, 0f);
        playerInput.Jump.performed += _ => jumpPressed = true;
    }

    void OnDestroy()
    {
        playerInput.Jump.performed -= _ => jumpPressed = true;
    }

    void Update()
    {
        moveInput = playerInput.Move.ReadValue<Vector2>();
        runPressed = playerInput.Run.IsPressed();

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

        RotateToCamera();
    }

    void FixedUpdate()
    {
        CheckGround();
        StepClimb();
        HandleMovement();
        HandleJump();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Rotate player body yaw to match camera yaw ONLY when move input given.
    // ─────────────────────────────────────────────────────────────────────
    void RotateToCamera()
    {
        if (playerLook == null) return;
        if (moveInput.magnitude < 0.1f) return;

        Quaternion target = Quaternion.Euler(0f, playerLook.Yaw, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Ground & Slope Detection
    // ─────────────────────────────────────────────────────────────────────
    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        isGrounded = false;
        isOnSlope = false;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            groundCheckDistance + 0.05f, groundLayer))
        {
            isGrounded = true;
            slopeNormal = hit.normal;

            float angle = Vector3.Angle(Vector3.up, slopeNormal);
            if (angle > 0.1f && angle <= maxSlopeAngle)
                isOnSlope = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Step Climbing
    // ─────────────────────────────────────────────────────────────────────
    void StepClimb()
    {
        if (!isGrounded) return;

        Vector3 moveDir = (transform.forward * moveInput.y +
                           transform.right * moveInput.x).normalized;

        if (moveDir.magnitude < 0.1f) return;

        Vector3 lowerOrigin = transform.position + stepLowerOriginOffset;
        Vector3 upperOrigin = transform.position + stepUpperOriginOffset;

        bool lowerHit = Physics.Raycast(lowerOrigin, moveDir, out RaycastHit _,
                                        stepSearchDepth, groundLayer);
        bool upperHit = Physics.Raycast(upperOrigin, moveDir,
                                        stepSearchDepth, groundLayer);

        if (lowerHit && !upperHit)
            rb.position += new Vector3(0f, stepHeight * Time.fixedDeltaTime * 10f, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Movement
    // ─────────────────────────────────────────────────────────────────────
    void HandleMovement()
    {
        float speed = runPressed ? runSpeed : walkSpeed;
        Vector3 moveDir = (transform.forward * moveInput.y +
                           transform.right * moveInput.x).normalized;

        float controlMultiplier = isGrounded ? 1.0f : airControlMultiplier;

        Vector3 targetVelocity;

        if (isOnSlope)
        {
            Vector3 slopeMoveDir = Vector3.ProjectOnPlane(moveDir, slopeNormal).normalized;
            targetVelocity = slopeMoveDir * speed * slopeSpeedMultiplier;
        }
        else
        {
            targetVelocity = moveDir * speed;
        }

        targetVelocity.y = rb.velocity.y;

        Vector3 velocityChange = (targetVelocity - rb.velocity) * controlMultiplier;
        velocityChange.y = 0f;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Jumping
    // ─────────────────────────────────────────────────────────────────────
    void HandleJump()
    {
        if (!jumpPressed) return;
        jumpPressed = false;

        if (isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldown;
            isGrounded = false;
        }
    }

    // ── public helpers ───────────────────────────────────────────────────
    public bool IsGrounded => isGrounded;
    public bool IsRunning => runPressed && moveInput.magnitude > 0.1f;
}