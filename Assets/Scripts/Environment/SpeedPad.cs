using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    [Header("Overboost Settings")]
    public float peakMaxSpeed = 45f;
    public float peakAcceleration = 15f;
    [Tooltip("Time in seconds that the speed stays flat before beginning to drop.")]
    public float holdDuration = 0.6f;
    [Tooltip("Time in seconds to bleed back down to base running limits.")]
    public float decayDuration = 2.0f;

    [Header("Launch Direction")]
    public bool launchInPadDirection = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            // Pass both values down to control retention windows
            player.ApplySpeedOverboost(peakMaxSpeed, peakAcceleration, decayDuration, holdDuration);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir = launchInPadDirection ? transform.forward :
                    (rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : player.transform.forward);

                launchDir.y = 0f;
                launchDir.Normalize();

                rb.linearVelocity = new Vector3(launchDir.x * peakMaxSpeed, rb.linearVelocity.y, launchDir.z * peakMaxSpeed);
            }
        }
    }
}