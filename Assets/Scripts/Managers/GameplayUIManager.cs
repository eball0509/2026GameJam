using TMPro;
using UnityEngine;

public class GameplayUIManager : MonoBehaviour
{

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Timer Settings")]
    [SerializeField] private bool isTimerRunning;

    public float elapsedTime {  get; private set; }

    private void Start() 
    { 

        elapsedTime = 0f;

        if (warningText != null) warningText.gameObject.SetActive(false);

    }

    private void Update()
    {

        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimer(elapsedTime);
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

            warningText.text = string.Format("EXPLOSION IMMENENT\n<size=75%>{0:F1}s", countdownValue);
        }
        else
        {
            if (warningText.gameObject.activeSelf) warningText.gameObject.SetActive(false);
        }

    }

    public void StopTimer() { isTimerRunning = false; }

    public void SetTimerActive(bool active) { isTimerRunning = active; }

}
