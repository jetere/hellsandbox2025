
using UnityEngine;

[DisallowMultipleComponent]

public class PlayerAction : MonoBehaviour
{
    [SerializeField]
    private PlayerGunSelector GunSelector;
    /* For New input System
    private void Update()
    {
        if(Mouse.current.leftButton.isPressed && GunSelector.ActiveGun != null)
        {
            GunSelector.ActiveGun.Shoot();
        }
    }
    */



    // For old input system
    private void Update()

        if (Input.GetMouseButton (0) && GunSelector.ActiveGun != null){

        GunSelector.ActiveGun.Shoot();

}
}
