using UnityEngine;

public class BulletFly : MonoBehaviour
{
    public float bulletSpeed = 40f; // Speed of the bullet
    public float lifeTime = 3f; // Lifetime of the bullet in seconds
    public float damage = 10f; // Damage dealt by the bullet
    private Rigidbody bulletRb;

    void Awake()
    {
        bulletRb = GetComponent<Rigidbody>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bulletRb.linearVelocity = transform.forward * bulletSpeed; // Set the initial velocity of the bullet
        Destroy(gameObject, lifeTime); // Destroy the bullet after its lifetime expires
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }
        
        if (other.CompareTag("Enemy"))
        {
            // Grab the EnemyController component and trigger its safe Die() method
            if (other.gameObject.TryGetComponent(out EnemyController enemy))
            {
                enemy.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
