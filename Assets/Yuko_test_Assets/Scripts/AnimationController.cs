using UnityEngine;

/// <summary>
/// Attach to the same GameObject as Animator.
///
/// Drives two Animator float parameters:
///   velocityZ  — forward(+) / backward(-)
///   velocityX  — right(+)   / left(-)
///
/// Reads from the shared PlayerControls Input Action Asset.
/// Works with keyboard, gamepad, and On Screen Stick (joystick).
/// </summary>
public class AnimationController : MonoBehaviour
{
    // ── inspector ────────────────────────────────────────────────────────
    [Header("Blend Speeds")]
    public float acceleration = 2.0f;
    public float deceleration = 2.0f;

    [Header("Velocity Limits")]
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;

    // ── private references ───────────────────────────────────────────────
    private Animator animator;
    private PlayerControls playerControls;
    private PlayerControls.PlayerActions playerInput;

    // ── animator parameter hashes ────────────────────────────────────────
    private int velocityZHash;
    private int velocityXHash;

    // ── smoothed velocity state ───────────────────────────────────────────
    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        playerControls = new PlayerControls();
        playerInput = playerControls.Player;
    }

    void OnEnable() => playerControls.Enable();
    void OnDisable() => playerControls.Disable();

    void Start()
    {
        animator = GetComponent<Animator>();
        velocityZHash = Animator.StringToHash("velocityZ");
        velocityXHash = Animator.StringToHash("velocityX");
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        Vector2 moveInput = playerInput.Move.ReadValue<Vector2>();
        bool runPressed = playerInput.Run.IsPressed();

        float currentMaxVelocity = runPressed ? maximumRunVelocity : maximumWalkVelocity;
        bool isJumping = playerInput.Jump.IsPressed();

        // Map raw joystick axes to target velocities scaled by max speed.
        // moveInput.y > 0 → forward,  moveInput.y < 0 → backward
        // moveInput.x > 0 → right,    moveInput.x < 0 → left
        float targetZ = moveInput.y * currentMaxVelocity ;
        float targetX = moveInput.x * currentMaxVelocity ;

        SmoothVelocity(ref velocityZ, targetZ, currentMaxVelocity);
        SmoothVelocity(ref velocityX, targetX, currentMaxVelocity);

        animator.SetFloat(velocityZHash, velocityZ);
        animator.SetFloat(velocityXHash, velocityX);
        animator.SetBool("isJumping", isJumping);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// Smoothly moves <current> toward <target> using acceleration /
    /// deceleration, then snaps to zero or ±max when close enough.
    // ─────────────────────────────────────────────────────────────────────
    void SmoothVelocity(ref float current, float target, float maxVelocity)
    {
        float snapThreshold = 0.05f;

        if (Mathf.Abs(target) > snapThreshold)
        {
            // Accelerate toward target
            float step = acceleration * Time.deltaTime;
            current = Mathf.MoveTowards(current, target, step);

            // Snap to exact max when very close
            if (Mathf.Abs(current - target) < snapThreshold)
                current = target;
        }
        else
        {
            // No input — decelerate toward zero
            float step = deceleration * Time.deltaTime;
            current = Mathf.MoveTowards(current, 0f, step);

            // Snap to zero when very close
            if (Mathf.Abs(current) < snapThreshold)
                current = 0.0f;
        }
    }
}