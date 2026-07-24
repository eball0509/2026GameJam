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
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.ApplySpeedOverboost(peakMaxSpeed, peakAcceleration, decayDuration);

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
                    launchDir = rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : player.transform.forward;
                }

                launchDir.y = 0f;
                launchDir.Normalize();

                rb.linearVelocity = new Vector3(launchDir.x * peakMaxSpeed, rb.linearVelocity.y, launchDir.z * peakMaxSpeed);
            }
        }
    }
}