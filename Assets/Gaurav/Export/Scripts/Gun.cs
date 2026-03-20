using UnityEngine;

public class Gun : MonoBehaviour
{
    public GunData data;

    [Header("Prefab Specific References")]
    public Transform firingPoint;
    // You can add unique muzzle flash prefabs here too

    void Awake()
    {
        // Automatically find firing point if not assigned
        if (firingPoint == null) firingPoint = transform.Find("FIRING POINT");
    }
}