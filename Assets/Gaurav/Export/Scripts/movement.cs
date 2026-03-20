using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Current Equipment")]
    public Gun currentGun;
    public MeleeWeapon currentMelee;
    public Grenade currentThrowable;
    public GameObject grenadePrefab;

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float sprintingSpeed = 8f;
    public float jumpHeight = 1.0f;
    public float gravity = -9.81f;
    private float baseSpeed;

    [Header("Camera Settings")]
    public CinemachineFreeLook cam;
    public float normalFov = 60f;
    public float sprintFov = 60f;
    public float scopeFov = 15f;
    public float hipFireXOffset = 0.5f;

    [Header("States")]
    public bool isFiring = false;
    public bool isMoving = false;
    public bool isSprinting = false;
    public bool isScoping = false;
    private bool isPaused = false;
    public bool isMeleeAttack = false;
    public bool isAiming = false;

    [Header("References")]
    public Transform mainCamera;
    public Animator animator;
    public LineRenderer lineRenderer;
    public GunManager gunManager;

    private CharacterController controller;
    private Vector2 movementInput;
    private Vector3 playerVelocity;
    private PlayerInput playerInput;
    private InputActionMap actionMap;

    // FIX: Cache the middle rig's CinemachineOrbitalTransposer for FollowOffset access
    private CinemachineOrbitalTransposer middleRigTransposer;

    void Start()
    {
        if (!lineRenderer)
            lineRenderer = transform.Find("LINE RENDERER").GetComponent<LineRenderer>();

        baseSpeed = moveSpeed;
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        actionMap = playerInput.actions.FindActionMap("Player");

        // FIX: Access the middle rig transposer correctly for Unity 2022 / Cinemachine 2.x
        if (cam != null)
        {
            CinemachineVirtualCamera middleRig = cam.GetRig(1); // 0=Top, 1=Middle, 2=Bottom
            if (middleRig != null)
                middleRigTransposer = middleRig.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!isPaused)
        {
            HandleGravity();
            HandleMovementAndRotation();
            HandleCameraState();
            isMoving = movementInput.sqrMagnitude > 0.01f;
            HandleAnimation();

            if (isFiring || isMeleeAttack || isAiming)
            {
                Vector3 cameraRotation = mainCamera.eulerAngles;
                transform.rotation = Quaternion.Euler(0, cameraRotation.y, 0);
            }
        }
    }

    public void SetMovementState(bool canMove)
    {
        isPaused = !canMove;
        if (canMove) actionMap.Enable();
        else actionMap.Disable();
    }

    void HandleMovementAndRotation()
    {
        Vector3 inputDir = (isMeleeAttack || isAiming)
            ? Vector3.zero
            : new Vector3(movementInput.x, 0, movementInput.y);

        Vector3 forward = mainCamera.forward; forward.y = 0; forward.Normalize();
        Vector3 right = mainCamera.right; right.y = 0; right.Normalize();
        Vector3 desiredMove = (forward * inputDir.z) + (right * inputDir.x);

        float speed = isScoping
            ? baseSpeed * 0.5f
            : (isSprinting && isMoving ? sprintingSpeed : baseSpeed);
        speed = isFiring ? 1f : speed;

        controller.Move(desiredMove * speed * Time.deltaTime);
        isMoving = inputDir.sqrMagnitude > 0.01f;

        if (isScoping)
        {
            Vector3 camForward = mainCamera.forward; camForward.y = 0;
            if (camForward != Vector3.zero)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(camForward),
                    Time.deltaTime * 20f);
        }
        else if (desiredMove != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(desiredMove),
                Time.deltaTime * 10f);
        }
    }

    void HandleGravity()
    {
        if (controller.isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void HandleCameraState()
    {
        if (cam == null) return;

        // FIX: Lerp FOV correctly via m_Lens (valid in Cinemachine 2.x / Unity 2022)
        float targetFov = isScoping
            ? scopeFov
            : (isSprinting && !isFiring ? sprintFov : normalFov);
        cam.m_Lens.FieldOfView = Mathf.Lerp(cam.m_Lens.FieldOfView, targetFov, Time.deltaTime * 10f);

        // FIX: Use the cached middle rig transposer to adjust the follow offset X
        if (middleRigTransposer != null)
        {
            float targetOffset = isScoping ? 0f : hipFireXOffset;
            Vector3 offset = middleRigTransposer.m_FollowOffset;
            offset.x = Mathf.Lerp(offset.x, targetOffset, Time.deltaTime * 10f);
            middleRigTransposer.m_FollowOffset = offset;
        }
    }

    void HandleAnimation()
    {
        if (animator == null) return;
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isFiring", isFiring);
        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isScoping", isScoping);
        animator.SetBool("isMeleeAttack", isMeleeAttack);
        animator.SetBool("isAiming", isAiming);
    }

    // --- INPUT CALLBACKS ---

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        // FIX: Only set baseSpeed to 1 inside the gun branch, not globally before branching
        if (gunManager != null && gunManager.currentGun != null)
        {
            isFiring = context.performed || context.started;
            baseSpeed = isFiring ? 1f : moveSpeed;
            gunManager.ToggleFiring(isFiring);
        }
        else if (currentMelee != null)
        {
            isMeleeAttack = context.performed || context.started;
            currentMelee.isMeleeAttack = isMeleeAttack;
            if (context.started) currentMelee.Attack();
        }
        else if (currentThrowable != null)
        {
            currentThrowable.camPos = mainCamera.transform;
            if (context.started)
            {
                isAiming = true;
                currentThrowable.isAiming = true;
            }
            else if (context.canceled)
            {
                isAiming = false;
                currentThrowable.isAiming = false;
                currentThrowable.throwItem();
            }
        }
    }

    public void onScope(InputAction.CallbackContext context)
    {
        // FIX: Added proper null checks — currentGun could be null even if gunManager isn't
        if (currentMelee == null && gunManager != null && gunManager.currentGun != null)
        {
            isScoping = context.performed || context.started;

            // FIX: Don't set animator.speed = 0 — that freezes all animations globally.
            // Instead, drive scoping speed via the Animator parameter "isScoping" already set
            // in HandleAnimation(), or add a dedicated float parameter if needed.
            // animator.speed = isScoping ? 0 : 1;  <-- REMOVED (breaks all anim layers)

            gunManager.ToggleScoping(isScoping);
        }
        else
        {
            isScoping = false;
            if (gunManager != null) gunManager.ToggleScoping(false);
        }
    }

    public void Sprint(InputAction.CallbackContext context) => isSprinting = !context.canceled;

    public void OnJump()
    {
        if (controller.isGrounded)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
    }
}