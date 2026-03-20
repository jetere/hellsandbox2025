using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{

    public int resolution = 30;
    public Vector3 startPos;
    public Transform camPos;
   
    public float velocityMag;
    public float stepTime = 0.05f;
    public LineRenderer lineRenderer;
    public bool isAiming = false;
    public LayerMask playerMask;
    public float throwDelay = 0.5f; // Time in seconds until the ball leaves the hand
    public GameObject grenadePrefab; // Use a prefab instead of this.gameObject to avoid recursion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMask = LayerMask.NameToLayer("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (isAiming) {
            Debug.Log("aiming");

            PredictPath();
        }
    }

    public void throwItem()
    {
        // 1. Immediately stop the trajectory line
        isAiming = false;
        lineRenderer.enabled = false;

        // 2. Start the delayed throw process
        StartCoroutine(DelayedThrow());
    }
    IEnumerator DelayedThrow()
    {
        yield return new WaitForSeconds(throwDelay);

        // Spawn at Camera position so it matches the line perfectly
        Vector3 spawnPos = camPos.position + (camPos.forward * 3f);
        GameObject projectile = Instantiate(grenadePrefab, spawnPos, camPos.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.velocity = camPos.forward * velocityMag;
    }
    void PredictPath()
    {
        lineRenderer.enabled = true;

        // 1. Start the math from the Camera, not the hand
        // We add a small forward offset so the line doesn't clip into the camera lens
        Vector3 currentPos = camPos.position + (camPos.forward * 3f);

        List<Vector3> points = new List<Vector3>();

        // 2. The velocity is now perfectly aligned with where the player looks
        Vector3 throwDir = camPos.forward;
        Vector3 currentVelocity = throwDir * velocityMag;

        points.Add(currentPos);

        for (int i = 0; i < resolution; i++)
        {
            Vector3 nextpos = currentPos + (currentVelocity * stepTime) + (0.5f * Physics.gravity * stepTime * stepTime);
            Vector3 dir = nextpos - currentPos;
            float distance = dir.magnitude;

            if (Physics.Raycast(currentPos, dir.normalized, out RaycastHit hit, distance, ~playerMask))
            {
                points.Add(hit.point);
                break;
            }

            currentVelocity += Physics.gravity * stepTime;
            currentPos = nextpos;
            points.Add(currentPos);
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
