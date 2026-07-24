using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    [Header("Overboost Settings")]
    [Tooltip("Peak max speed when stepping on pad (goes past 30 to redline speedometer).")]
    public float peakMaxSpeed = 45f;

    [Tooltip("Peak acceleration while boosted.")]
    public float peakAcceleration = 15f;

    [Tooltip("Time in seconds to bleed back down to 30 speed.")]
    public float decayDuration = 2.5f;

    [Header("Launch Direction")]
    [Tooltip("If true, launches player along pad's forward direction. If false, maintains player movement direction.")]
    public bool launchInPadDirection = true;

    private void OnTriggerEnter(Collider other)
    {
        // Try getting PlayerController from entering object
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            // 1. Trigger acceleration & max speed decay coroutine on player
            player.ApplySpeedOverboost(peakMaxSpeed, peakAcceleration, decayDuration);

            // 2. Instantly launch player's velocity past 30 speed
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir;

                if (launchInPadDirection)
                {
                    launchDir = transform.forward;
                }
                else
                {
                    // Use player's current velocity direction, or player's facing direction if stationary
                    launchDir = rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : player.transform.forward;
                }

                // Flatten vertical component to prevent flying up
                launchDir.y = 0f;
                launchDir.Normalize();

                // Instantly set velocity to the boosted speed
                rb.linearVelocity = new Vector3(launchDir.x * peakMaxSpeed, rb.linearVelocity.y, launchDir.z * peakMaxSpeed);
            }
        }
    }
}