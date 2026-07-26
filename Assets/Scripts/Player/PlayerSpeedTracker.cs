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

    [Header("Activation Settings")]
    public bool isTrackingActive = false;

    private float countdown;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        countdown = explodeTimer;
        gameplayUIManager = FindAnyObjectByType<GameplayUIManager>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
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
        isTrackingActive = active;
        countdown = explodeTimer; // Reset the timer so it starts fresh from the trigger line

        if (!active && gameplayUIManager != null)
        {
            gameplayUIManager.UpdateWarningUI(false, 0f);
        }
    }

    private void Explode()
    {
        Debug.Log("You suh and died");
    }
}