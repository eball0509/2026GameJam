using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [Tooltip("MUST exactly match the levelName string used on the main menu's LevelMapNode!")]
    public string levelName = "Tutorial";

    [Tooltip("The exact levelName identifier of the NEXT level to unlock when beaten.")]
    public string nextLevelToUnlock = "Easy";

    [Header("Medal Targets (Clear Time in Seconds)")]
    public float goldTimeTarget = 15f;
    public float silverTimeTarget = 30f;
    public float bronzeTimeTarget = 45f;

    [Header("MainMenu System Mapping")]
    [SerializeField] private string mainMenuSceneName = "MainMenuExample";

    private int collectedCount = 0;
    private bool isLevelFinished = false;

    private void Start()
    {
        collectedCount = 0;
        isLevelFinished = false;

        GameplayUIManager uiManager = FindAnyObjectByType<GameplayUIManager>();
        if (uiManager != null)
        {
            uiManager.SetTimerActive(true);
        }
    }

    public void IncrementCollectibleCount()
    {
        collectedCount++;
        Debug.Log($"Collected: {collectedCount}");
    }

    public void CompleteLevel()
    {
        if (isLevelFinished) return;
        isLevelFinished = true;

        float finalTime = 0f;

        GameplayUIManager uiManager = FindAnyObjectByType<GameplayUIManager>();
        if (uiManager != null)
        {
            uiManager.StopTimer();
            finalTime = uiManager.elapsedTime;
        }

        Debug.Log($"Level Completed in: {finalTime:F2}s with {collectedCount} items!");

        int earnedMedalRank = 0;
        if (finalTime <= goldTimeTarget) earnedMedalRank = 3;
        else if (finalTime <= silverTimeTarget) earnedMedalRank = 2;
        else if (finalTime <= bronzeTimeTarget) earnedMedalRank = 1;

        // Save progress stats
        float currentBestTime = PlayerPrefs.GetFloat(levelName + "_BestTime", 0f);
        if (currentBestTime <= 0f || finalTime < currentBestTime)
        {
            PlayerPrefs.SetFloat(levelName + "_BestTime", finalTime);
        }

        int currentMaxItems = PlayerPrefs.GetInt(levelName + "_Collectibles", 0);
        if (collectedCount > currentMaxItems)
        {
            PlayerPrefs.SetInt(levelName + "_Collectibles", collectedCount);
        }

        int currentBestMedal = PlayerPrefs.GetInt(levelName + "_MedalRank", 0);
        if (earnedMedalRank > currentBestMedal)
        {
            PlayerPrefs.SetInt(levelName + "_MedalRank", earnedMedalRank);
        }

        if (!string.IsNullOrEmpty(nextLevelToUnlock))
        {
            PlayerPrefs.SetInt("Unlocked_" + nextLevelToUnlock, 1);
        }

        // Tag this level name so the Main Menu knows to focus back onto it instantly
        PlayerPrefs.SetString("LastPlayedLevel", levelName);
        PlayerPrefs.Save();

        // Load back to menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
}