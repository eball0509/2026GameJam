using UnityEngine;

public class LevelStartTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your GameplayUIManager from the hierarchy here.")]
    [SerializeField] private GameplayUIManager gameplayUIManager;

    [Header("Settings")]
    [SerializeField] private bool disableAfterTrigger = true;

    private float spawnTime;

    private void Start()
    {
        // Record the exact time the scene loaded
        spawnTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        // SAFETY FIX: Ignore triggers that fire instantly on the very first frame (prevents spawn-overlap bugs)
        if (Time.time - spawnTime < 0.2f) return;

        if (other.CompareTag("Player"))
        {
            if (gameplayUIManager == null)
            {
                gameplayUIManager = Object.FindAnyObjectByType<GameplayUIManager>();
            }

            if (gameplayUIManager != null)
            {
                // 1. Activate the UI elapsed time timer
                gameplayUIManager.SetTimerActive(true);
                gameplayUIManager.UpdateWarningUI(false, 0f);
                Debug.Log("UI Timer successfully activated by trigger!");
            }

            // 2. Activate the Move-or-Die speed tracker timer on the player
            PlayerSpeedTracker speedTracker = other.GetComponent<PlayerSpeedTracker>();
            if (speedTracker != null)
            {
                speedTracker.SetSpeedTrackingActive(true);
                Debug.Log("PlayerSpeedTracker activated successfully by trigger!");
            }

            // Disable trigger so it only fires once
            if (disableAfterTrigger)
            {
                gameObject.SetActive(false);
            }
            else
            {
                enabled = false;
            }
        }
    }
}