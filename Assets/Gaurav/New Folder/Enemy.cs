using System.Collections; // Needed for Coroutines
using TMPro;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 100;
    public TextMeshProUGUI txt;
    public Material damageMaterial;

    private Renderer enemyRenderer;
    private Material originalMaterial;

    void Start()
    {
        // Cache the renderer and the starting material
        enemyRenderer = GetComponent<Renderer>();
        originalMaterial = enemyRenderer.material;
    }

    void Update()
    {
        if (txt != null) txt.text = health.ToString();
    }

    public void takeDamage(float damage)
    {
        health -= damage;

        // Stop any flash already running so they don't overlap
        StopCoroutine("FlashRed");
        StartCoroutine("FlashRed");

        if (health <= 0)
        {
            if (txt != null) Destroy(txt.gameObject);
            Destroy(gameObject);
        }
    }

    // This is the "Timer" that handles the color swap
    IEnumerator FlashRed()
    {
        // Change to red material
        enemyRenderer.material = damageMaterial;

        // Wait for 0.1 seconds
        yield return new WaitForSeconds(0.1f);

        // Change back to original
        enemyRenderer.material = originalMaterial;
    }
}