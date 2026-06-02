using UnityEngine;

/// <summary>
/// Attach to CameraPivot (root level, NOT child of Player).
///
/// Scene Hierarchy:
///   Player          <- Rigidbody, PlayerMovement
///   CameraPivot     <- THIS script
///       Main Camera <- child, zeroed local transform
///
/// Input sources (all via shared PlayerControls asset):
///   PC      — Mouse delta
///   Gamepad — Right stick  (also works with On Screen Stick bound to <Gamepad>/rightStick)
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Header("Target")]
    public Transform playerBody;
    public Vector3 pivotOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Sensitivity")]
    public float mouseSensitivityX = 0.2f;
    public float mouseSensitivityY = 0.2f;
    public float stickSensitivityX = 120f;   // degrees per second at full deflection
    public float stickSensitivityY = 120f;

    [Header("Vertical Clamp")]
    public float minPitch = -25.0f;
    public float maxPitch = 50.0f;

    [Header("Distance")]
    public float distance = 5.0f;

    [Header("Camera Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;

    // ── state ─────────────────────────────────────────────────────────────
    private float _yaw;
    private float _pitch = 10f;

    // Shared Input Action Asset — same instance as PlayerMovement & AnimationController
    private PlayerControls playerControls;
    private PlayerControls.PlayerActions playerInput;

    public float Yaw => _yaw;

    void Awake()
    {
        playerControls = new PlayerControls();
        playerInput = playerControls.Player;
    }

    void OnEnable() => playerControls.Enable();
    void OnDisable() => playerControls.Disable();

    void Start()
    {
        // Lock cursor on PC; on Android this has no effect
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody != null)
            _yaw = playerBody.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        HandleLook();
        PositionCamera();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Look — Mouse delta (PC) + Right Stick / On Screen Stick (Android)
    //
    // On Android:
    //   - Place a transparent UI Panel on the RIGHT half of the screen
    //   - Add Unity's "On Screen Stick" component to it
    //   - Set its Control Path to <Gamepad>/rightStick
    //   This drives the stickDelta branch below automatically.
    // ─────────────────────────────────────────────────────────────────────
    void HandleLook()
    {
        // Mouse (PC only — returns zero on Android)
        Vector2 mouseDelta = playerInput.Look.ReadValue<Vector2>();
        _yaw += mouseDelta.x * mouseSensitivityX;
        _pitch -= mouseDelta.y * mouseSensitivityY;

        // Right stick / On Screen Stick (Gamepad / Android)
        Vector2 stickDelta = playerInput.LookStick.ReadValue<Vector2>();
        _yaw += stickDelta.x * stickSensitivityX * Time.deltaTime;
        _pitch -= stickDelta.y * stickSensitivityY * Time.deltaTime;

        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Position & rotate the camera pivot with collision push-back
    // ─────────────────────────────────────────────────────────────────────
    void PositionCamera()
    {
        Vector3 pivot = playerBody.position + pivotOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desired = pivot + rotation * new Vector3(0f, 0f, -distance);

        float safeDistance = distance;
        Vector3 dir = (desired - pivot).normalized;

        if (Physics.SphereCast(pivot, collisionRadius, dir,
                               out RaycastHit hit, distance, collisionLayers))
        {
            safeDistance = Mathf.Max(hit.distance - collisionRadius, 0.5f);
        }

        transform.position = pivot + rotation * new Vector3(0f, 0f, -safeDistance);
        transform.rotation = rotation;
    }
}