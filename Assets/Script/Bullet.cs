using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 75f;
    public float damage = 10f;
    public float maxLifetime = 3f;
    private float boundary = 30f;

    [Header("Collision Settings")]
    public LayerMask hitLayers = ~0;
    public GameObject hitEffectPrefab;

    void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        float moveDistance = speed * Time.deltaTime;

        // 1. Raycast ahead this frame so fast lasers never pass through thin enemies or walls
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance, hitLayers, QueryTriggerInteraction.Collide))
        {
            HandleHit(hit.collider, hit.point, hit.normal);
            return;
        }

        // 2. Move forward along +Z
        transform.Translate(Vector3.forward * moveDistance);

        // 3. Destroy if out of map bounds
        if (Mathf.Abs(transform.position.x) > boundary || Mathf.Abs(transform.position.z) > boundary)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player") || other.gameObject.layer == LayerMask.NameToLayer("Bullet"))
            return;

        HandleHit(other, transform.position, -transform.forward);
    }

    void HandleHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player") || other.gameObject.layer == LayerMask.NameToLayer("Bullet"))
            return;

        if (other.CompareTag("Enemy"))
        {
            EnemyController enemyController = other.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(damage);
            }
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        Destroy(gameObject);
    }
}