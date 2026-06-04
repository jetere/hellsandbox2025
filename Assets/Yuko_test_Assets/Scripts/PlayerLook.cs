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
///   Mobile  — Right-half finger drag (touch delta, resolution-independent)
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Header("Target")]
    public Transform playerBody;
    public Vector3 pivotOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Sensitivity")]
    public float mouseSensitivityX = 0.2f;
    public float mouseSensitivityY = 0.2f;
    public float stickSensitivityX = 120f;
    public float stickSensitivityY = 120f;
    public float touchSensitivityX = 0.1f;   // tunable in Inspector
    public float touchSensitivityY = 0.1f;

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

    // Touch look state
    private int _lookTouchId = -1;
    private Vector2 _lastTouchPos = Vector2.zero;

    // Shared Input Action Asset — same instance as PlayerMovement & AnimationController
    private PlayerControls playerControls;
    private PlayerControls.PlayerActions playerInput;

    public float Yaw => _yaw;

    void Awake()
    {
        Input.multiTouchEnabled = true;         // allow joystick + look touch simultaneously
        playerControls = new PlayerControls();
        playerInput = playerControls.Player;
    }

    void OnEnable() => playerControls.Enable();
    void OnDisable() => playerControls.Disable();

    void Start()
    {
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
    // Look — Mouse (PC) + Right Stick / On Screen Stick + Touch right half
    // ─────────────────────────────────────────────────────────────────────
    void HandleLook()
    {
        // ── Mouse (PC) ───────────────────────────────────────────────────
        Vector2 mouseDelta = playerInput.Look.ReadValue<Vector2>();
        _yaw += mouseDelta.x * mouseSensitivityX;
        _pitch -= mouseDelta.y * mouseSensitivityY;

        // ── Right stick / On Screen Stick (Gamepad / Android) ────────────
        Vector2 stickDelta = playerInput.LookStick.ReadValue<Vector2>();
        _yaw += stickDelta.x * stickSensitivityX * Time.deltaTime;
        _pitch -= stickDelta.y * stickSensitivityY * Time.deltaTime;

        // ── Touch drag on right half of screen (Mobile) ──────────────────
        // Any finger that BEGAN on the right half is claimed as the look
        // finger. Fingers that started on the left half (on-screen joystick)
        // are ignored entirely — no bleed-over into camera movement.
        Vector2 touchDelta = ReadTouchLookDelta();
        _yaw += touchDelta.x * touchSensitivityX;
        _pitch -= touchDelta.y * touchSensitivityY;

        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Returns pixel delta of the designated look finger this frame.
    // Only a finger that STARTED on the right half can become look finger.
    // ─────────────────────────────────────────────────────────────────────
    Vector2 ReadTouchLookDelta()
    {
        float screenMidX = Screen.width * 0.5f;
        Vector2 delta = Vector2.zero;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.phase == TouchPhase.Began)
            {
                // Claim only right-half touches, and only if no look finger active yet
                if (t.position.x > screenMidX && _lookTouchId == -1)
                {
                    _lookTouchId = t.fingerId;
                    _lastTouchPos = t.position;
                }
            }
            else if (t.fingerId == _lookTouchId)
            {
                if (t.phase == TouchPhase.Moved)
                {
                    delta = t.position - _lastTouchPos;
                    _lastTouchPos = t.position;
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _lookTouchId = -1;
                }
            }
        }

        return delta;
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