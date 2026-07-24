using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Speedometer Physical UI Elements")]
    [SerializeField] private RectTransform needleTransform;
    [SerializeField] private Image dangerZoneImage;
    [SerializeField] private Image overboostZoneImage;

    [Header("Speedometer Procedural Markers")]
    [Tooltip("A simple UI image/text object that represents one little hash mark.")]
    [SerializeField] private RectTransform markerPrefab;
    [Tooltip("The parent GameObject to hold all the markers (like DialBackground).")]
    [SerializeField] private RectTransform markerParent;
    [Tooltip("Distance from the pivot point to spawn the markers.")]
    [SerializeField] private float markerRadius = 33.3f;
    [Tooltip("The peak speed that has a visual marker (e.g., 30f). The dial will go past this during overboost.")]
    [SerializeField] private float maxMarkerSpeed = 30f;
    [Tooltip("Distance between marked large intervals (0, 15, 30).")]
    [SerializeField] private float bigTickInterval = 15f;
    [Tooltip("Distance between marked small intervals (5, 10, 20, 25).")]
    [SerializeField] private float smallTickInterval = 5f;
    [SerializeField] float textInwardOffset = 12f;

    [Header("Speedometer Angle Settings")]
    public float zeroSpeedAngle = 135f;
    public float maxSpeedAngle = -135f;

    [Header("Speedometer Shaking Settings")]
    public float shakeIntensity = 8f;
    public float shakeSpeed = 55f;

    [Header("Timer Settings")]
    [SerializeField] private bool isTimerRunning;

    private float currentNeedleRotation;
    public float elapsedTime { get; private set; }

    private void Start()
    {
        elapsedTime = 0f;
        if (warningText != null) warningText.gameObject.SetActive(false);

        // Run the automatic marker generator
        GenerateMarkers();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimer(elapsedTime);
        }
    }

    private void OnValidate()
    {
        // Only run this if the game is actively running in the editor
        if (Application.isPlaying && markerParent != null && markerPrefab != null)
        {
            // Clear out the old ticks so they don't stack up
            foreach (Transform child in markerParent)
            {
                // Don't destroy the original template prefab if it's hiding in there
                if (child.gameObject != markerPrefab.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }

            // Re-run the generation with your new radius value
            GenerateMarkers();
        }
    }

    private void UpdateTimer(float displayedTime)
    {
        int minutes = Mathf.FloorToInt(displayedTime / 60f);
        int seconds = Mathf.FloorToInt(displayedTime % 60f);
        float milliseconds = Mathf.FloorToInt(Mathf.Repeat(displayedTime, 1f) * 100f);

        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    public void UpdateWarningUI(bool showWarning, float countdownValue)
    {
        if (warningText == null) return;

        if (showWarning)
        {
            if (!warningText.gameObject.activeSelf) warningText.gameObject.SetActive(true);
            warningText.text = string.Format("EXPLOSION IMMINENT\n<size=75%>{0:F1}s", countdownValue);
        }
        else
        {
            if (warningText.gameObject.activeSelf) warningText.gameObject.SetActive(false);
        }
    }

    // --- NEW: THE AUTOMATIC MARKER GENERATOR ---
    private void GenerateMarkers()
    {
        if (markerPrefab == null || markerParent == null)
        {
            Debug.LogError("GameplayUIManager: Please assign 'Marker Prefab' and 'Marker Parent' in the inspector!");
            return;
        }

        float totalSpeedRange = maxMarkerSpeed; // 30f
        float totalAngleSpan = zeroSpeedAngle - maxSpeedAngle; // 270 degrees

        for (float speed = 0f; speed <= maxMarkerSpeed; speed += smallTickInterval)
        {
            // 1. Instantiate the marker from the prefab
            RectTransform newMarker = Instantiate(markerPrefab, markerParent);
            newMarker.gameObject.SetActive(true);

            // 2. Calculate the specific angle for this speed mark
            float speedRatio = speed / totalSpeedRange;
            float angle = Mathf.Lerp(zeroSpeedAngle, maxSpeedAngle, speedRatio);

            // 3. Position the marker using trigonometry
            float rad = (angle + 90f) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(rad) * markerRadius, Mathf.Sin(rad) * markerRadius);

            newMarker.anchoredPosition = position;
            newMarker.localRotation = Quaternion.Euler(0f, 0f, angle);

            // 4. Style based on intervals
            bool isBigTick = Mathf.Abs(speed % bigTickInterval) < 0.01f;

            if (isBigTick)
            {
                newMarker.localScale = new Vector3(1.5f, 1.5f, 1f);

                TextMeshProUGUI label = newMarker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = speed.ToString("F0");
                    label.gameObject.SetActive(true);

                    // FIX: Reset anchored position back to (0,0) first
                    label.rectTransform.anchoredPosition = Vector2.zero;

                    // FIX: We calculate the inward direction relative to the MARKER CONTAINER'S rotated space.
                    // Since the marker container is turned towards the center, moving along the local -Y axis 
                    // always pulls the text directly down the line toward the absolute center of the circle!
                    Vector2 localInwardDirection = Vector2.down;
                    label.rectTransform.anchoredPosition = localInwardDirection * textInwardOffset;

                    // Counter-rotate the text AFTER setting the position so it stays right-side up without breaking the path!
                    label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
            }
            else
            {
                newMarker.localScale = Vector3.one;

                TextMeshProUGUI label = newMarker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }
            }
        }
    }

    public void UpdateSpeedometerUI(float currentSpeed, float minRequiredSpeed, float baseMaxSpeed)
    {
        if (needleTransform == null) return;

        float totalAngleSpan = zeroSpeedAngle - maxSpeedAngle;
        float speedRatio = Mathf.Clamp01(currentSpeed / baseMaxSpeed);
        float targetAngle = Mathf.Lerp(zeroSpeedAngle, maxSpeedAngle, speedRatio);

        if (dangerZoneImage != null)
        {
            float dangerRatio = minRequiredSpeed / baseMaxSpeed;
            dangerZoneImage.fillAmount = dangerRatio * (totalAngleSpan / 360f);
        }

        if (overboostZoneImage != null)
        {
            overboostZoneImage.fillAmount = (360f - totalAngleSpan) / 360f;
        }

        // We use totalAngleSpan here to continue moving the needle PAST -135 correctly
        if (currentSpeed > baseMaxSpeed)
        {
            float overboostExcess = (currentSpeed - baseMaxSpeed) / baseMaxSpeed;
            targetAngle += overboostExcess * (-totalAngleSpan) * 0.25f;

            float shakeOffset = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * shakeIntensity;
            targetAngle += shakeOffset;
        }

        currentNeedleRotation = Mathf.Lerp(currentNeedleRotation, targetAngle, 15f * Time.deltaTime);
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, currentNeedleRotation);
    }

    public void StopTimer() { isTimerRunning = false; }
    public void SetTimerActive(bool active) { isTimerRunning = active; }
}