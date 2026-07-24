using UnityEngine;

public class Hazards : MonoBehaviour
{
    [Header("Hazard Settings")]
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null) player.TakeDamage(damage);
    }
}
