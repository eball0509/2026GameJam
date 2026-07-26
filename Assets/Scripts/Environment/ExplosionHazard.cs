using UnityEngine;

public class ExplosiveHazard : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Amount of damage dealt to the player. Set high to guarantee a kill.")]
    public int explosionDamage = 999;

    [Header("Explosion Settings")]
    public float explosionForce = 2000f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 2f;

    [Header("Visual Effects")]
    public GameObject explosionEffectPrefab;

    [Header("Targeting")]
    public string playerTag = "Player";

    private bool hasExploded = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Robust check: Finds PlayerController anywhere in parent/hierarchy 
        // OR checks the tag, so it never fails due to child colliders.
        PlayerController hitPlayer = other.GetComponentInParent<PlayerController>();
        bool isTargetValid = (hitPlayer != null) || (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag));

        if (isTargetValid)
        {
            TriggerExplosion(hitPlayer);
        }
    }

    private void TriggerExplosion(PlayerController hitPlayer)
    {
        hasExploded = true;

        // 1. Spawn visual effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Deal damage and apply force directly to the player if found
        if (hitPlayer != null)
        {
            hitPlayer.TakeDamage(explosionDamage);

            Rigidbody[] playerRbs = hitPlayer.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in playerRbs)
            {
                if (!rb.isKinematic)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
                }
            }
        }

        // 3. Blast any other physical objects in the area (boxes, barrels, etc.)
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitCollider in colliders)
        {
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();

            if (rb != null)
            {
                PlayerController potentialPlayer = hitCollider.GetComponentInParent<PlayerController>();
                if (potentialPlayer != hitPlayer)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
                }
            }
        }

        // 4. (Optional) Destroy the hazard object so it's gone
        // Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}