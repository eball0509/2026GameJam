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

    private void Awake()
    {
        // Force the warning text hidden immediately when the scene loads
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }

        // FORCE the timer to be turned off when the scene starts
        isTimerRunning = false;
    }

    private void Start()
    {
        elapsedTime = 0f;
        if (warningText != null) warningText.gameObject.SetActive(false);

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || markerParent == null || markerPrefab == null) return;

            for (int i = markerParent.childCount - 1; i >= 0; i--)
            {
                Transform child = markerParent.GetChild(i);
                if (child.gameObject != markerPrefab.gameObject)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            GenerateMarkers();
        };
    }
#endif

    private void UpdateTimer(float displayedTime)
    {
        int minutes = Mathf.FloorToInt(displayedTime / 60f);
        int seconds = Mathf.FloorToInt(displayedTime % 60f);
        float milliseconds = Mathf.FloorToInt(Mathf.Repeat(displayedTime, 1f) * 100f);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
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

    private void GenerateMarkers()
    {
        if (markerPrefab == null || markerParent == null) return;

        if (Application.isPlaying && markerParent.childCount > 1)
        {
            bool hasTicks = false;
            foreach (Transform child in markerParent)
            {
                if (child.gameObject != markerPrefab.gameObject) { hasTicks = true; break; }
            }
            if (hasTicks) return;
        }

        float totalSpeedRange = maxMarkerSpeed;
        float totalAngleSpan = zeroSpeedAngle - maxSpeedAngle;

        for (float speed = 0f; speed <= maxMarkerSpeed; speed += smallTickInterval)
        {
            RectTransform newMarker = Instantiate(markerPrefab, markerParent);
            newMarker.gameObject.SetActive(true);

            float speedRatio = speed / totalSpeedRange;
            float angle = Mathf.Lerp(zeroSpeedAngle, maxSpeedAngle, speedRatio);

            float rad = (angle + 90f) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(rad) * markerRadius, Mathf.Sin(rad) * markerRadius);

            newMarker.anchoredPosition = position;
            newMarker.localRotation = Quaternion.Euler(0f, 0f, angle);

            bool isBigTick = Mathf.Abs(speed % bigTickInterval) < 0.01f;

            if (isBigTick)
            {
                newMarker.localScale = new Vector3(1.5f, 1.5f, 1f);

                TextMeshProUGUI label = newMarker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = speed.ToString("F0");
                    label.gameObject.SetActive(true);
                    label.rectTransform.anchoredPosition = Vector2.zero;

                    Vector2 localInwardDirection = Vector2.down;
                    label.rectTransform.anchoredPosition = localInwardDirection * textInwardOffset;
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


    public void SetTimerActive(bool active)
    {
        isTimerRunning = active;

        // This will print the exact script and line number that called this method
        Debug.LogWarning($"SetTimerActive({active}) was just triggered! Called from:\n{System.Environment.StackTrace}");
    }
}