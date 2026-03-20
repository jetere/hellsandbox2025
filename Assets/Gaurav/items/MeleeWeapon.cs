using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // <--- You MUST include this!
public class MeleeWeapon : MonoBehaviour
{
    [Header("Combat Settings")]
    public float damage = 25f;
    public float attackRange = 2f;
    public float attackRadius = 0.5f; // The "thickness" of the swing
    public float attackDelay = 0.15f; // Time before the hit actually connects
    public float attackCooldown = 0.6f;
    public Slider slider;
    private float currentCoolDown;
    [Header("References")]
    public LayerMask enemyLayer;
    public Animator animator; // Can be the player's animator or the weapon's
    public bool isMeleeAttack=false;
    private bool isAttacking = false;
    private List<GameObject> hitEnemies = new List<GameObject>();


    // Inside MeleeWeapon.cs
    public void Initialize(Animator playerAnim) // Renamed from Setup
    {
        animator = playerAnim;
    }
    public void Attack()
    {
        Debug.Log("attack");
        if (isAttacking) return;
        StartCoroutine(PerformAttack());
    }

    void Update() // Note: Fixed 'update' to 'Update' (Capital U is required)
    {
        if (slider != null)
        {
            if (isAttacking)
            {
                // Calculate how much time is left in the cooldown
                // This gives a value between 0 and 1
                float remaining = (nextAttackTime - Time.time) / attackCooldown;
                slider.value = Mathf.Clamp01(remaining);
            }
            else
            {
                // Cooldown is finished
                slider.value = 0;
            }
        }
        if (isMeleeAttack && !isAttacking)
        {
            Attack();
        }
    }
    private float nextAttackTime;
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown; // Set the finish line
        hitEnemies.Clear();

        // 1. Play Animation
        float calculatedSpeed = 1f / attackCooldown;
        animator.speed = calculatedSpeed;
        if (animator != null) animator.SetTrigger("MeleeAttack");

        // 2. Wait for the "sweet spot"
        yield return new WaitForSeconds(attackDelay);

        // 3. Detect Hits (using CompareTag for better performance!)
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, attackRadius, transform.forward, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
            GameObject target = hit.collider.gameObject;
            if (!hitEnemies.Contains(target) && target.CompareTag("Enemy"))
            {
                hitEnemies.Add(target);
                ApplyDamage(target);
            }
        }

        // 4. Wait for the rest of the cooldown
        yield return new WaitForSeconds(attackCooldown - attackDelay);
        animator.speed = 1;
        isAttacking = false;
    }
    private void ApplyDamage(GameObject enemy)
    {
        Debug.Log("Hit " + enemy.name + " for " + damage + " damage!");
        // If you have an Enemy script, call it here:
        enemy.GetComponent<Enemy>().takeDamage(damage);
    }

    // This helps you see the attack range in the Scene view while setting up
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 endPoint = transform.position + transform.forward * attackRange;
        Gizmos.DrawLine(transform.position, endPoint);
        Gizmos.DrawWireSphere(endPoint, attackRadius);
    }
}