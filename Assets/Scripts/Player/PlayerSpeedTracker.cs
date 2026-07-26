using UnityEngine;

public class PlayerSpeedTracker : MonoBehaviour
{
    private Rigidbody rb;
    private GameplayUIManager gameplayUIManager;
    private PlayerController playerController;

    [Header("Speed Settings")]
    public float currentSpeed;
    public float minimumRequiredSpeed = 5f;
    public float explodeTimer = 3f;

    [Header("Explosion Settings")]
    public GameObject explosionEffectPrefab;
    public float explosionForce = 20f;
    public float explosionRadius = 10f;
    public float upwardModifier = 3f;

    [Header("Activation Settings")]
    public bool isTrackingActive = false;

    private float countdown;
    private bool hasExploded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        countdown = explodeTimer;
        gameplayUIManager = FindAnyObjectByType<GameplayUIManager>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (hasExploded) return;

        // If the player is already dead from another source, stop tracking entirely
        if (playerController != null && playerController.currentHealth <= 0)
        {
            if (gameplayUIManager != null)
            {
                gameplayUIManager.UpdateWarningUI(false, 0f);
            }
            return;
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        currentSpeed = horizontalVelocity.magnitude;

        // Always update the speedometer UI
        if (gameplayUIManager != null && playerController != null)
        {
            gameplayUIManager.UpdateSpeedometerUI(currentSpeed, minimumRequiredSpeed, playerController.maxRunSpeed);
        }

        // Stop here if the trigger hasn't been activated yet
        if (!isTrackingActive) return;

        if (currentSpeed < minimumRequiredSpeed)
        {
            countdown -= Time.deltaTime;

            if (gameplayUIManager != null)
            {
                gameplayUIManager.UpdateWarningUI(true, countdown);
            }

            if (countdown <= 0)
            {
                Explode();
            }
        }
        else
        {
            // Reset countdown and hide warning text when speed is fine
            countdown = explodeTimer;

            if (gameplayUIManager != null)
            {
                gameplayUIManager.UpdateWarningUI(false, 0f);
            }
        }
    }

    // Called by the trigger script
    public void SetSpeedTrackingActive(bool active)
    {
        if (hasExploded || (playerController != null && playerController.currentHealth <= 0)) return;

        isTrackingActive = active;
        countdown = explodeTimer;

        if (!active && gameplayUIManager != null)
        {
            gameplayUIManager.UpdateWarningUI(false, 0f);
        }
    }

    private void Explode()
    {
        if (hasExploded || (playerController != null && playerController.currentHealth <= 0)) return;
        hasExploded = true;
        isTrackingActive = false;

        // Hide the warning UI immediately on death
        if (gameplayUIManager != null)
        {
            gameplayUIManager.UpdateWarningUI(false, 0f);
        }

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }

        if (playerController != null)
        {
            playerController.Die();

            // Apply explosion force to launch the player's ragdoll bones
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            foreach (var body in rbs)
            {
                if (!body.isKinematic)
                {
                    body.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse);
                }
            }
        }
    }
}