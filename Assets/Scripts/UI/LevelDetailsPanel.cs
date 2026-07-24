using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <-- Required for loading scenes!
using TMPro;

public class LevelDetailsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Image levelImage;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image medalImage;
    [SerializeField] private Button playButton; // Reference to your Play UI Button

    [Tooltip("The placeholder UI Images inside your Horizontal Layout Group.")]
    [SerializeField] private Image[] UICollectibleSlots;

    [Header("Visual Configurations")]
    [SerializeField] private float transitionSpeed = 5f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine currentTransition;

    private Vector3 nativeActiveScale;
    private string sceneToLoad; // Keeps track of the scene name passed from the active node

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        nativeActiveScale = rectTransform.localScale;

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        // Hook up the button click logic via code to keep it clean
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayLevelPressed);
        }
    }

    public void OpenPanel(string levelName, string summary, Sprite levelSprite, Sprite medalSprite, Sprite[] levelItemSprites, int collectiblesFound, int totalCollectibles, float bestTime, Vector3 targetScreenPosition, string targetSceneName)
    {
        transform.position = targetScreenPosition;
        sceneToLoad = targetSceneName; // Save the scene name for when they hit play!

        gameObject.SetActive(true);

        nameText.text = levelName;
        summaryText.text = summary;
        levelImage.sprite = levelSprite;

        string timeDisplay = bestTime > 0 ? $"{bestTime:F2}s" : "N/A";
        statsText.text = $"Best Time: {timeDisplay}\nCollectibles: {collectiblesFound} / {totalCollectibles}";

        if (medalSprite != null)
        {
            medalImage.gameObject.SetActive(true);
            medalImage.sprite = medalSprite;
        }
        else
        {
            medalImage.gameObject.SetActive(false);
        }

        for (int i = 0; i < UICollectibleSlots.Length; i++)
        {
            if (i < totalCollectibles)
            {
                UICollectibleSlots[i].gameObject.SetActive(true);
                if (levelItemSprites != null && i < levelItemSprites.Length && levelItemSprites[i] != null)
                {
                    UICollectibleSlots[i].sprite = levelItemSprites[i];
                }

                if (i < collectiblesFound)
                {
                    UICollectibleSlots[i].color = Color.white;
                }
                else
                {
                    UICollectibleSlots[i].color = new Color(0.1f, 0.1f, 0.1f, 0.4f);
                }
            }
            else
            {
                UICollectibleSlots[i].gameObject.SetActive(false);
            }
        }

        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionRoutine(1f, nativeActiveScale));
    }

    public void ClosePanel()
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionRoutine(0f, Vector3.zero, () => gameObject.SetActive(false)));
    }

    private void OnPlayLevelPressed()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"Loading Scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No scene name assigned to this level node!");
        }
    }

    private IEnumerator TransitionRoutine(float targetAlpha, Vector3 targetScale, System.Action onComplete = null)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.001f || Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, transitionSpeed * Time.deltaTime);
            rectTransform.localScale = Vector3.MoveTowards(rectTransform.localScale, targetScale, transitionSpeed * Time.deltaTime);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        rectTransform.localScale = targetScale;
        onComplete?.Invoke();
    }
}