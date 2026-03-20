using UnityEngine;
using Cinemachine;

public class GunManager : MonoBehaviour
{
    [Header("Current Active Gun")]
    public Gun currentGun;

    [Header("Global References")]
    public CinemachineVirtualCamera cineCam;
    public GameObject bulletTrailPrefab;
    public GameObject impactPrefab;
    public AudioSource audioSource;
    public LayerMask hitLayerMask;

    [Header("Scope UI & Settings")]
    public GameObject scopeOverlayUI;
    public float scopeZoomFOV = 15f;
    public float normalFOV = 60f;
    public float hipFireXOffset = 0.5f;
    public float trailOffset;

    [Header("Culling Layers")]
    public LayerMask normalMask;
    public LayerMask scopeMask;

    private Camera mainCamera;
    private CinemachineOrbitalTransposer orbitalBody;
    private float nextTimeToFire = 0f;
    private bool isFiring = false;
    private bool isScoping = false;

    void Start()
    {
        mainCamera = Camera.main;

        if (cineCam != null)
        {
            // In 2022, you use GetCinemachineComponent, not GetComponent
            orbitalBody = cineCam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        }

        if (scopeOverlayUI != null) scopeOverlayUI.SetActive(false);
    }

    public void ToggleFiring(bool state) => isFiring = state;

    public void ToggleScoping(bool state)
    {
        isScoping = state;

        if (scopeOverlayUI != null) scopeOverlayUI.SetActive(isScoping);

        if (mainCamera != null) mainCamera.cullingMask = isScoping ? scopeMask : normalMask;
    }

    void Update()
    {
        if (currentGun == null) return;

        if (isFiring && Time.time >= nextTimeToFire)
        {
            Shoot();
            if (!currentGun.data.isAutomatic) isFiring = false;
            nextTimeToFire = Time.time + (1f / currentGun.data.fireRate);
        }

        HandleCameraState();
    }

    void Shoot()
    {
        GunData data = currentGun.data;
        if (data.fireSound != null) audioSource.PlayOneShot(data.fireSound);

        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;
        Vector3 destination = rayOrigin + (rayDirection * data.firingRange);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, data.firingRange, hitLayerMask))
        {
            destination = hit.point;
            if (impactPrefab != null)
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            Enemy target = hit.collider.GetComponent<Enemy>();
            if (target != null) target.takeDamage(data.damage);
        }

        VisualizeTrail(destination);
    }

    void VisualizeTrail(Vector3 destination)
    {
        Vector3 origin;
        if (isScoping)
        {
            origin = mainCamera.transform.position + (mainCamera.transform.up * -0.2f) + (mainCamera.transform.forward * 0.5f);
        }
        else
        {
            origin = (currentGun.firingPoint != null) ? currentGun.firingPoint.position + currentGun.firingPoint.forward * trailOffset : currentGun.transform.position + currentGun.transform.forward * trailOffset;
        }

        GameObject trailObject = Instantiate(bulletTrailPrefab, origin, Quaternion.identity);
        var visualizer = trailObject.GetComponent<TrailVisualizer>();
        if (visualizer != null) visualizer.SetTarget(destination);
    }

    void HandleCameraState()
    {
        if (cineCam == null || orbitalBody == null) return;

        if (isScoping)
        {
            // Fixed: m_Lens for FOV
            cineCam.m_Lens.FieldOfView = Mathf.Lerp(cineCam.m_Lens.FieldOfView, scopeZoomFOV, Time.deltaTime * 15f);

            // Fixed: FollowOffset for position
            orbitalBody.m_FollowOffset.x = 0; // Usually 0 when scoping for center view

            // Fixed: Removed TrackerSettings. Access damping directly on orbitalBody
            orbitalBody.m_XDamping = 0;
            orbitalBody.m_YDamping = 0;
            orbitalBody.m_ZDamping = 0;
        }
        else
        {
            // Fixed: m_Lens for FOV
            cineCam.m_Lens.FieldOfView = Mathf.Lerp(cineCam.m_Lens.FieldOfView, normalFOV, Time.deltaTime * 5f);

            // Fixed: m_FollowOffset
            orbitalBody.m_FollowOffset.x = hipFireXOffset;

            // Fixed: Manual damping values
            orbitalBody.m_XDamping = 0.1f;
            orbitalBody.m_YDamping = 0.1f;
            orbitalBody.m_ZDamping = 0.1f;
        }
    }

    void HandleGunRotation()
    {
        Quaternion targetGunRot = mainCamera.transform.rotation * Quaternion.Euler(0, 180f, 0);
        float lerpSpeed = isScoping ? 100f : 20f;
        currentGun.transform.rotation = Quaternion.Slerp(currentGun.transform.rotation, targetGunRot, lerpSpeed * Time.deltaTime);
    }
}