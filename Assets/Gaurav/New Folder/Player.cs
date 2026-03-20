using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public float health = 100f;
    private float healthRem;
    public Canvas damageScreenCanvas; // Renamed for clarity
    public float displayDuration = 0.5f; // New: How long the damage screen stays on (e.g., half a second)
    public Image img;
    public TextMeshProUGUI playerHealth;
    // Start is called once before the first execution of Update
    void Start()
    {
        healthRem = health;
        playerHealth.text=health.ToString();
        // Ensure the damage screen is off when the game starts
        if (damageScreenCanvas != null)
        {
            damageScreenCanvas.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Remove the line: canvas.enabled = false; 
        // We will now control the canvas state from the TakeDamage function.
    }

    public void TakeDamage(float damage)
    {
        // 1. Apply Damage
        healthRem -= damage;
        playerHealth.text = healthRem.ToString();
        img.fillAmount = healthRem/health;
        health = Mathf.Max(0, health); // Ensure health doesn't go below 0
        Debug.Log("ON");
        // 2. Show the Damage Screen
        if (damageScreenCanvas != null)
        {
           
            // Stop any pending Invoke calls to prevent them from interfering
            CancelInvoke("HideDamageScreen");

            // Turn the canvas ON
            damageScreenCanvas.enabled = true;

            // 3. Schedule the canvas to turn OFF after the displayDuration
            Invoke("HideDamageScreen", displayDuration);
        }

        // 4. Handle Death
        if (health <= 0)
        {
            Die();
        }
    }

    // ?? New Function: Turns the canvas off
    private void HideDamageScreen()
    {
        Debug.Log("Off");
        if (damageScreenCanvas != null)
        {
            damageScreenCanvas.enabled = false;
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // Add your player death logic here
    }
}