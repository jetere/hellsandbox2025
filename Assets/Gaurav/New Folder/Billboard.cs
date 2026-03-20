using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Cache the camera reference
        mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Calculate the vector pointing from the health bar TO the camera.
        Vector3 directionToCamera = mainCameraTransform.position - transform.position;

        // Use Quaternion.LookRotation to create a rotation that faces 
        // the direction *opposite* of the camera (i.e., making the UI face the camera).
        // Pass Vector3.up as the worldUp vector to prevent rolling/tilting.
        transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up); 
    }
}