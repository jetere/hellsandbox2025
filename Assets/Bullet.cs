using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime); // Auto-destroy after 3 seconds
    }

    void OnCollisionEnter(Collision collision)
    {
        // Add damage logic here later
        Destroy(gameObject);
    }
}
