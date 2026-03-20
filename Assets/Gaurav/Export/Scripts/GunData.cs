using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Weapon/Gun Data")]
public class GunData : ScriptableObject
{
    public string gunName;
    public float damage;
    public float fireRate;
    public bool isAutomatic; // Semi-auto vs Full-auto
    public int maxAmmo;
    public float firingRange;
    public AudioClip fireSound;
}