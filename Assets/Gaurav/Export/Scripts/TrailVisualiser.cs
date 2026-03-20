using UnityEngine;

public class TrailVisualizer : MonoBehaviour
{
    public float speed = 100f;
    private Vector3 target;
    private bool initialized = false;

    public void SetTarget(Vector3 destination)
    {
        target = destination;
        initialized = true;
        // Optional: Make the trail look toward the target
        transform.LookAt(destination);
    }

    void Update()
    {
        if (!initialized) return;

        // Move towards the target
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        // Destroy when it reaches the destination
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}