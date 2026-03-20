using UnityEngine;

public class EnemyTracker : MonoBehaviour
{
    // Assign your Player's Transform in the Inspector
    public Transform playerTarget;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f; // For smoother turning

    void Start()
    {
        // Find the player automatically if the reference is not set (Assumes Player has the tag "Player")
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    void Update()
    {
        if (playerTarget != null)
        {
            // 1. Calculate the direction vector to the player
            Vector3 direction = playerTarget.position - transform.position;

            // Ensure the enemy doesn't move vertically unless needed (common for 3D platformers)
            direction.y = 0;

            // 2. Rotate to face the target smoothly
            if (direction != Vector3.zero)
            {
                // Create a rotation looking in the direction vector
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // Smoothly transition from the current rotation to the target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // 3. Move the enemy forward
            // The enemy's forward direction (transform.forward) is now facing the player
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }
}