using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    private Rigidbody enemyRb;
    private Collider enemyCollider;
    private Animator enemyAnim;
    private GameObject player;
    private bool dead = false;

    [Header("Stats")]
    public float contactDamage = 10f;
    public float attackCooldown = 1f;
    private float nextAttackTime = 0f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float maxHealth = 30f;
    public float deathDestroyDelay = 3f; // Time to wait for death animation before destroying object
    private float currentHealth;

    [Header("Health Bar")]
    public Slider healthBar;
    private Transform camTransform;

    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        enemyAnim = GetComponentInChildren<Animator>();

        // Freeze ALL physics rotation so bumping into objects never spins the 3D model off-axis
        if (enemyRb != null)
        {
            enemyRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Disable Root Motion so the Animator doesn't fight Rigidbody.MovePosition
        if (enemyAnim != null)
        {
            enemyAnim.applyRootMotion = false;
        }

        player = GameObject.Find("Player");
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    void FixedUpdate()
    {
        if (player == null || dead) return;

        // Calculate direction to player, keeping Y at 0 so enemies don't float upward when the player jumps
        Vector3 direction = player.transform.position - enemyRb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            // 1. Smoothly rotate the 3D enemy to face the player
            Quaternion targetRot = Quaternion.LookRotation(direction);
            enemyRb.MoveRotation(Quaternion.Slerp(enemyRb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));

            // 2. Move toward the player
            enemyRb.MovePosition(enemyRb.position + direction * moveSpeed * Time.fixedDeltaTime);

            // 3. Drive walking animation
            if (enemyAnim != null)
            {
                enemyAnim.SetBool("IsWalking", true);
            }
        }
        else if (enemyAnim != null)
        {
            enemyAnim.SetBool("IsWalking", false);
        }
    }

    void Update()
    {
        if (!dead && transform.position.y < -10f)
        {
            Die();
        }
    }

    void LateUpdate()
    {
        if (healthBar != null && camTransform != null && !dead)
        {
            healthBar.transform.parent.rotation = camTransform.rotation;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (dead) return;

        currentHealth -= damageAmount;
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        // Guard clause: ensures OnEnemyKilled() is only ever called ONCE per enemy
        if (dead) return;
        dead = true;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.OnEnemyKilled();
        }

        // 1. Trigger the Death animation
        if (enemyAnim != null)
        {
            enemyAnim.SetBool("IsWalking", false);
            enemyAnim.SetTrigger("Die");
        }

        // 2. Hide the health bar immediately
        if (healthBar != null)
        {
            healthBar.transform.parent.gameObject.SetActive(false);
        }

        // 3. Disable Collider & Rigidbody physics so the corpse doesn't block bullets or push the player
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        if (enemyRb != null)
        {
            enemyRb.linearVelocity = Vector3.zero;
            enemyRb.isKinematic = true;
        }

        // 4. Destroy the GameObject after the death animation finishes
        Destroy(gameObject, deathDestroyDelay);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (dead) return;

        if (collision.gameObject.CompareTag("Player") && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            PlayerController playerCtrl = collision.gameObject.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.TakeDamage(contactDamage);
            }
        }
    }
}