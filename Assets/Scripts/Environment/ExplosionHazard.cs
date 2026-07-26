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

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded && collision.gameObject.CompareTag(playerTag))
        {
            TriggerExplosion(collision.gameObject);
        }
    }

    // Use this instead if your hazard is a Trigger (IsTrigger checked)
    /*
    private void OnTriggerEnter(Collider other)
    {
        if (!hasExploded && other.CompareTag(playerTag))
        {
            TriggerExplosion(other.gameObject);
        }
    }
    */

    private void TriggerExplosion(GameObject targetHit)
    {
        hasExploded = true;

        // 1. Spawn visual effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Find the player and deal damage first so the ragdoll activates
        PlayerController hitPlayer = targetHit.GetComponentInParent<PlayerController>();
        if (hitPlayer != null)
        {
            hitPlayer.TakeDamage(explosionDamage);

            // 3. Immediately apply force directly to the newly activated ragdoll parts.
            // (We do this directly in case Unity's OverlapSphere needs a physics frame to update)
            Rigidbody[] playerRbs = hitPlayer.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in playerRbs)
            {
                if (!rb.isKinematic) // Ensure we are only applying force to active ragdoll limbs
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
                }
            }
        }

        // 4. Blast any other physical objects in the area (like boxes or barrels)
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitCollider in colliders)
        {
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();

            // Ensure we don't double-apply force to the player we just blasted
            if (rb != null)
            {
                PlayerController potentialPlayer = hitCollider.GetComponentInParent<PlayerController>();
                if (potentialPlayer != hitPlayer)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
                }
            }
        }

        // 5. (Optional) Destroy the hazard object so it's gone
        // Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}