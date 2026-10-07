using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    private Rigidbody enemyRb;
    private Collider enemyCollider;
    private Animator enemyAnim;
    private GameObject player;
    private PlayerController playerCtrl;
    private NavMeshAgent agent;
    private bool dead = false;

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float maxHealth = 30f;
    private float currentHealth;
    public float deathDestroyDelay = 3f; // Time to wait for death animation before destroying object
    public int pointsPerKill = 100;

    [Header("Combat & Melee Attack")]
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    public float attackHitDelay = 0.5f;
    private float nextAttackTime = 0f;

    [Header("Health Bar")]
    public Slider healthBar;
    private Transform camTransform;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange * 0.85f;
        }

        player = GameObject.Find("Player");
        if (player != null)
        {
            playerCtrl = player.GetComponent<PlayerController>();
        }

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
        if (dead) return;

        if (!dead && transform.position.y < -10f)
        {
            Die();
            return;
        }

        if (player == null || (GameManager.Instance != null && GameManager.Instance.isGameOver))
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (enemyAnim != null) enemyAnim.SetBool("IsWalking", false);
            return;
        }

        // Flat horizontal distance to the player
        Vector3 flatPlayerPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);

        if (distanceToPlayer <= attackRange)
        {
            // 1. Inside Attack Range: Stop moving, face the player, and attack on cooldown
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            if (enemyAnim != null)
            {
                enemyAnim.SetBool("IsWalking", false);
            }

            // Smoothly face the player while attacking
            Vector3 lookDir = (flatPlayerPos - transform.position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                if (enemyAnim != null)
                {
                    enemyAnim.SetTrigger("Attack");
                }
                StartCoroutine(DealDamageRoutine(attackHitDelay));
            }
        }
        else
        {
            // 2. Outside Attack Range: Pathfind around obstacles toward the player
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
            }

            if (enemyAnim != null)
            {
                enemyAnim.SetBool("IsWalking", true);
            }
        }
    }

    void LateUpdate()
    {
        if (healthBar != null && camTransform != null && !dead)
        {
            healthBar.transform.parent.rotation = camTransform.rotation;
        }
    }

    private IEnumerator DealDamageRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 1. Cancel the hit if the enemy was killed or the game ended during the swing
        if (dead || player == null || playerCtrl == null || (GameManager.Instance != null && GameManager.Instance.isGameOver))
        {
            yield break;
        }

        // 2. Allow the player to dodge! Only apply damage if they are still inside the attack range
        // (Added a 0.5f buffer so stepping back a tiny millimeter doesn't unfairly miss)
        Vector3 flatPlayerPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);

        if (distanceToPlayer <= attackRange + 0.5f)
        {
            playerCtrl.TakeDamage(attackDamage);
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
            SpawnManager.Instance.OnEnemyKilled(pointsPerKill);
        }

        // 1. Trigger the Death animation
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
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

        if (enemyAnim != null)
        {
            enemyAnim.SetBool("IsWalking", false);
            enemyAnim.SetTrigger("Die");
        }

        // 4. Destroy the GameObject after the death animation finishes
        Destroy(gameObject, deathDestroyDelay);
    }
}