using UnityEngine;

public class destroy : MonoBehaviour
{
    public AudioClip impactSound;
    private AudioSource audioSource;
    public float lifetime = 1.0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // This tells Unity: "Wait X seconds, then delete this object"
        Destroy(gameObject, lifetime);
        audioSource.PlayOneShot(impactSound);
    }
}
