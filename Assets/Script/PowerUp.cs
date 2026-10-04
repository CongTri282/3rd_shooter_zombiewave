using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public float duration = 5f; // Duration of the power-up effect in seconds

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
