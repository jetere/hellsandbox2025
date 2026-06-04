using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the same GameObject as Animator + PlayerMovement.
///
/// Drives Animator parameters:
///   velocityZ   — forward(+) / backward(-)
///   velocityX   — right(+)   / left(-)
///   isJumping   — true while airborne, false once grounded again
///   isGrounded  — mirrors PlayerMovement.IsGrounded each frame
///
/// Reads from the shared PlayerControls Input Action Asset.
/// Works with keyboard, gamepad, and On Screen Stick (joystick).
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
public class AnimationController : MonoBehaviour
{
    // ── inspector ────────────────────────────────────────────────────────
    [Header("Blend Speeds")]
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 10.0f;

    [Header("Velocity Limits")]
    [SerializeField] private float maximumWalkVelocity = 0.5f;
    [SerializeField] private float maximumRunVelocity = 2.0f;

    [Header("Snap Threshold")]
    [SerializeField] private float snapThreshold = 0.05f;

    // ── private references ───────────────────────────────────────────────
    private Animator animator;
    private PlayerMovement playerMovement;     // source of IsGrounded truth
    private PlayerControls playerControls;
    private PlayerControls.PlayerActions playerInput;

    // ── animator parameter hashes ────────────────────────────────────────
    private int velocityZHash;
    private int velocityXHash;
    private int isJumpingHash;
    private int isGroundedHash;

    // ── smoothed velocity state ───────────────────────────────────────────
    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;

    // ── previous values for dirty-checking ───────────────────────────────
    private float prevVelocityZ = float.MaxValue;
    private float prevVelocityX = float.MaxValue;
    private bool prevIsGrounded = true;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();

        velocityZHash = Animator.StringToHash("velocityZ");
        velocityXHash = Animator.StringToHash("velocityX");
        isJumpingHash = Animator.StringToHash("isJumping");
        isGroundedHash = Animator.StringToHash("isGrounded");

        playerControls = new PlayerControls();
        playerInput = playerControls.Player;

        // Jump fires instantly via callback — no Update() frame gap.
        playerInput.Jump.performed += OnJump;
    }

    void OnDestroy()
    {
        playerInput.Jump.performed -= OnJump;
    }

    // Fires the moment the jump button is detected.
    // Guards with PlayerMovement.IsGrounded so we never set isJumping
    // while already airborne (matches the same guard in PlayerMovement.HandleJump).
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (playerMovement.IsGrounded)
            animator.SetBool(isJumpingHash, true);
    }

    void OnEnable() => playerControls.Enable();
    void OnDisable() => playerControls.Disable();

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        // ── grounded check ───────────────────────────────────────────────
        // Reads directly from PlayerMovement.IsGrounded, which is driven
        // by the same raycast CheckGround() that controls the physics jump.
        // This keeps animation and physics perfectly in sync.
        bool isGrounded = playerMovement.IsGrounded;

        // Landing: isGrounded just flipped true → clear isJumping so the
        // Animator transitions back to the locomotion state.
        if (isGrounded && !prevIsGrounded)
            animator.SetBool(isJumpingHash, false);

        if (isGrounded != prevIsGrounded)
        {
            animator.SetBool(isGroundedHash, isGrounded);
            prevIsGrounded = isGrounded;
        }

        // ── movement input ───────────────────────────────────────────────
        Vector2 moveInput = Vector2.ClampMagnitude(playerInput.Move.ReadValue<Vector2>(), 1f);

        // Use PlayerMovement.IsRunning so run state stays consistent
        // between the physics layer and the animation layer.
        float currentMaxVelocity = playerMovement.IsRunning ? maximumRunVelocity : maximumWalkVelocity;

        float targetZ = moveInput.y * currentMaxVelocity;
        float targetX = moveInput.x * currentMaxVelocity;

        SmoothVelocity(ref velocityZ, targetZ, currentMaxVelocity);
        SmoothVelocity(ref velocityX, targetX, currentMaxVelocity);

        if (velocityZ != prevVelocityZ)
        {
            animator.SetFloat(velocityZHash, velocityZ);
            prevVelocityZ = velocityZ;
        }

        if (velocityX != prevVelocityX)
        {
            animator.SetFloat(velocityXHash, velocityX);
            prevVelocityX = velocityX;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// Smoothly moves <current> toward <target> using acceleration /
    /// deceleration, then snaps to zero when close enough.
    // ─────────────────────────────────────────────────────────────────────
    void SmoothVelocity(ref float current, float target, float maxVelocity)
    {
        if (Mathf.Abs(target) > snapThreshold)
        {
            current = Mathf.MoveTowards(current, target, acceleration * Time.deltaTime);
        }
        else
        {
            current = Mathf.MoveTowards(current, 0f, deceleration * Time.deltaTime);

            if (Mathf.Abs(current) < snapThreshold)
                current = 0.0f;
        }
    }
}