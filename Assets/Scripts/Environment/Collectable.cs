using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Level Data Profiles")]
    [Tooltip("The unique index for this item in the level layout slots (e.g., 0 for the first unique model, 1 for the second, etc.)")]
    public int collectibleIndex = 0;

    [Header("Visual Effects")]
    [SerializeField] private bool is2DCollectible = false;

    [Header("Bobbing Settings")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;

    [Header("3D Spinning Settings")]
    [SerializeField] private float spinSpeed = 100f;

    private Vector3 startPosition;
    private Camera mainCamera;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Start()
    {
        // Cache the main camera reference for optimal 2D performance
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleVisualEffects();
    }

    private void HandleVisualEffects()
    {
        // 1. Handle Bobbing Up and Down using a Sine Wave
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 2. Handle Rotation / Camera Orientation
        if (is2DCollectible)
        {
            if (mainCamera != null)
            {
                // Match the camera's current angle exactly so the sprite never looks flat
                transform.rotation = mainCamera.transform.rotation;
            }
        }
        else
        {
            // Spin regularly around the Y-axis over time
            transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager manager = FindAnyObjectByType<LevelManager>();
            if (manager != null)
            {
                manager.TrackSpecificCollectible(collectibleIndex);
            }

            // Optional: Spawn particles or play a sound effect here
            Destroy(gameObject);
        }
    }
}