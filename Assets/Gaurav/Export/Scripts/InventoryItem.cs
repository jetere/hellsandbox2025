using UnityEngine;
[CreateAssetMenu(fileName = "New InventoryItem", menuName = "Inventory/Weapon")]
public class InventoryItem : ScriptableObject
{

    public string weaponName;
    public int weaponID; // Unique ID for each gun (0 = Pistol, 1 = SMG, etc.)
    public Sprite icon;
    public GameObject weaponPrefab;
  // Does the player have this?
    public ItemCategory category; // NEW: Firearm, Melee, or Throwable
                                  // Start is called once before the first execution of Update after the MonoBehaviour is created

}

// This defines the "list" of types available
public enum ItemCategory
{
    Firearm,
    Melee,
    Throwable,
    Consumable
}