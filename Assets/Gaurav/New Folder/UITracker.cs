using UnityEngine;

public class UITracker : MonoBehaviour
{
    public Transform target; // Drag your Enemy's Transform here
    public Vector3 offset = new Vector3(0f, 2.0f, 0f); // 2 units above the enemy

    void Update()
    {
        if (target != null)
        {
            // Set the Canvas position to the target position plus the offset
            transform.position = target.position + offset;
        }
    }
}