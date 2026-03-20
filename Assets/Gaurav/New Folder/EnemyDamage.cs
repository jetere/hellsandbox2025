using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public float contactDamage = 10f;

    // Assumes the Player GameObject has the tag "Player"
    private const string PlayerTag = "Player";

    // This function is called when two colliders first touch
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the object we hit is the player
        if (collision.gameObject.CompareTag(PlayerTag))
        {
            // Get the Player's health script component
            // NOTE: You must have a script on your player (e.g., PlayerHealth) 
            // that has a function called TakeDamage(float damageAmount)
            Player playerHealth = collision.gameObject.GetComponent<Player>();

            if (playerHealth != null)
            {
                // Apply damage to the player
                playerHealth.TakeDamage(contactDamage);
                Debug.Log($"Player hit! Damage dealt: {contactDamage}");
            }
        }
    }
}