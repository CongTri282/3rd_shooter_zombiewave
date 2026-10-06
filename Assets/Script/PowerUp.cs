using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public float duration = 5f; // Duration of the power-up effect in seconds
    public float lifetime = 10f; // Lifetime of the power-up object before it disappears

    void Awake()
    {
        // Destroy the power-up object after its lifetime expires
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Activate the power-up effect on the player
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ActivatePowerUp(duration);
            }

            // Destroy the power-up object after being collected
            Destroy(gameObject);
        }
    }
}
