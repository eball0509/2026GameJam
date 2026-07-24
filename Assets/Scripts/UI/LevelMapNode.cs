using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelMapNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Level Data Profiles")]
    public string levelName = "Tutorial";
    [TextArea(2, 4)] public string levelSummary = "A short jump through the block valley.";
    public int totalCollectiblesCount = 3;

    [Tooltip("The exact name of the Unity Scene file to load for this specific level.")]
    [SerializeField] private string sceneTargetName = "Scene_Tutorial";

    [Header("Visual Configs (Unique to this Level)")]
    [SerializeField] private Sprite fullLevelImage;
    [SerializeField] private Sprite[] uniqueCollectibleSprites;
    [SerializeField] private Sprite[] performanceMedals;

    [Header("Placement Positioning")]
    [SerializeField] private Transform panelAnchor;

    [SerializeField] private float hoverScaleFactor = 1.15f;
    [SerializeField] private float scalingSpeed = 12f;

    [Header("Scene Node Links")]
    [SerializeField] private LevelDetailsPanel detailsPanel;
    [SerializeField] private LevelSelectManager levelSelectManager;

    private RectTransform rectTransform;
    private Button buttonComponent;
    private Vector3 initialBaseScale;
    private Vector3 targetedActiveScale;
    private bool isUnlocked = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonComponent = GetComponent<Button>();
    }

    private void Start()
    {
        initialBaseScale = rectTransform.localScale;
        targetedActiveScale = initialBaseScale;

        CheckUnlockProgression();

        if (buttonComponent != null)
        {
            buttonComponent.onClick.AddListener(OnNodeClicked);
        }
    }

    private void CheckUnlockProgression()
    {
        if (levelName == "Tutorial")
        {
            PlayerPrefs.SetInt("Unlocked_" + levelName, 1);
        }
        isUnlocked = PlayerPrefs.GetInt("Unlocked_" + levelName, 0) == 1;

        Image nodeImage = GetComponent<Image>();
        if (!isUnlocked)
        {
            if (nodeImage != null) nodeImage.enabled = false;
            if (buttonComponent != null) buttonComponent.interactable = false;
        }
        else
        {
            if (nodeImage != null) nodeImage.enabled = true;
            if (buttonComponent != null) buttonComponent.interactable = true;
        }
    }

    private void Update()
    {
        if (!isUnlocked) return;
        rectTransform.localScale = Vector3.MoveTowards(rectTransform.localScale, targetedActiveScale, scalingSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isUnlocked) return;
        targetedActiveScale = initialBaseScale * hoverScaleFactor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isUnlocked) return;
        targetedActiveScale = initialBaseScale;
    }

    private void OnNodeClicked()
    {
        if (!isUnlocked || detailsPanel == null) return;

        int gemsFound = PlayerPrefs.GetInt(levelName + "_Collectibles", 0);
        float bestTime = PlayerPrefs.GetFloat(levelName + "_BestTime", 0.0f);

        int medalRank = PlayerPrefs.GetInt(levelName + "_MedalRank", 0);
        Sprite earnedMedalSprite = null;
        if (medalRank > 0 && medalRank <= performanceMedals.Length)
        {
            earnedMedalSprite = performanceMedals[medalRank - 1];
        }

        Vector3 targetPanelPos = panelAnchor != null ? panelAnchor.position : transform.position;

        // Passing sceneTargetName to the OpenPanel function setup
        detailsPanel.OpenPanel(
            levelName,
            levelSummary,
            fullLevelImage,
            earnedMedalSprite,
            uniqueCollectibleSprites,
            gemsFound,
            totalCollectiblesCount,
            bestTime,
            targetPanelPos,
            sceneTargetName
        );

        if (levelSelectManager != null)
        {
            Vector3 focusCenterPoint = (transform.position + targetPanelPos) * 0.5f;
            levelSelectManager.ZoomIntoLevelNode(focusCenterPoint);
        }
    }

    // ==========================================
    // 🛠️ DEBUG / DEV CHEAT BUTTONS FOR TESTING
    // ==========================================

    [ContextMenu("DEBUG: Force Unlock This Level")]
    public void DebugUnlockLevel()
    {
        PlayerPrefs.SetInt("Unlocked_" + levelName, 1);
        PlayerPrefs.Save();
        CheckUnlockProgression();
        Debug.Log($"<color=green>DevCheat:</color> Unlocked {levelName}!");
    }

    [ContextMenu("DEBUG: Simulate 100% Beat Level (All Gems + Gold)")]
    public void DebugBeatLevelWithMaxStats()
    {
        PlayerPrefs.SetInt("Unlocked_" + levelName, 1);
        PlayerPrefs.SetInt(levelName + "_Collectibles", totalCollectiblesCount);
        PlayerPrefs.SetFloat(levelName + "_BestTime", 12.34f);
        PlayerPrefs.SetInt(levelName + "_MedalRank", 3);
    }

    [ContextMenu("DEBUG: Reset ALL Game Saves")]
    public void DebugResetAllSaves()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        CheckUnlockProgression();
        Debug.Log("<color=red>DevCheat:</color> Completely wiped all PlayerPrefs data!");
    }
}