using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    void Update()
    {
        // Left mouse click (Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire1 pressed!");
            Shoot();
        }

        // L key
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("L key pressed!");
            Shoot();
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.velocity = firePoint.forward * bulletSpeed;
    }
}
