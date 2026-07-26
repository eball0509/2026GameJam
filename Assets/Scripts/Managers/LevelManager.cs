using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    public string levelName = "Tutorial";
    public string nextLevelToUnlock = "Easy";

    [Header("Medal Targets (Clear Time in Seconds)")]
    public float goldTimeTarget = 15f;
    public float silverTimeTarget = 30f;
    public float bronzeTimeTarget = 45f;

    [Header("MainMenu System Mapping")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // Tracks which specific item indices were collected during this runtime session
    private HashSet<int> collectedIndices = new HashSet<int>();
    private bool isLevelFinished = false;

    private void Start()
    {
        collectedIndices.Clear();
        isLevelFinished = false;
    }

    public void TrackSpecificCollectible(int index)
    {
        if (!collectedIndices.Contains(index))
        {
            collectedIndices.Add(index);
            Debug.Log($"Collected unique item index: {index}");
        }
    }

    // Deprecated but kept to prevent compilation errors if called elsewhere
    public void IncrementCollectibleCount() { }

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

        // SAVE EACH SPECIFIC ITEM PERMANENTLY
        foreach (int index in collectedIndices)
        {
            PlayerPrefs.SetInt($"{levelName}_Collectible_{index}", 1);
        }

        int totalUniqueSaved = 0;
        for (int i = 0; i < 20; i++)
        {
            if (PlayerPrefs.GetInt($"{levelName}_Collectible_{i}", 0) == 1) totalUniqueSaved++;
        }
        PlayerPrefs.SetInt(levelName + "_Collectibles", totalUniqueSaved);

        int currentBestMedal = PlayerPrefs.GetInt(levelName + "_MedalRank", 0);
        if (earnedMedalRank > currentBestMedal)
        {
            PlayerPrefs.SetInt(levelName + "_MedalRank", earnedMedalRank);
        }

        if (!string.IsNullOrEmpty(nextLevelToUnlock))
        {
            PlayerPrefs.SetInt("Unlocked_" + nextLevelToUnlock, 1);
        }

        PlayerPrefs.SetString("LastPlayedLevel", mainMenuSceneName);
        PlayerPrefs.Save();

        // --- REPLACE SceneManager.LoadScene WITH THIS ---
        FindAnyObjectByType<GameEndUIManager>()?.TriggerWinState();
    }
}