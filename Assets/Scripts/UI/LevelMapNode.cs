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

    [Header("Medal Targets (Clear Time in Seconds)")]
    [Tooltip("Ensure these targets match the ones set in your LevelManager script exactly!")]
    public float goldTimeTarget = 15f;
    public float silverTimeTarget = 30f;
    public float bronzeTimeTarget = 45f;

    [Header("Visual Configs (Unique to this Level)")]
    [SerializeField] private Sprite fullLevelImage;
    [SerializeField] private Sprite[] uniqueCollectibleSprites;
    [Tooltip("Ensure array layout is: Element 0 = Bronze, Element 1 = Silver, Element 2 = Gold")]
    [SerializeField] private Sprite[] performanceMedals;

    [Header("Node Icon Medal Badge")]
    [Tooltip("Drag the child Image object here that will show the medal on the map node.")]
    [SerializeField] private Image nodeMedalImage; // Added Reference for the Node Medal Display

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
        if (levelName == "The Boring Green Starting Place")
        {
            PlayerPrefs.SetInt("Unlocked_" + levelName, 1);
        }
        isUnlocked = PlayerPrefs.GetInt("Unlocked_" + levelName, 0) == 1;

        Image nodeImage = GetComponent<Image>();
        if (!isUnlocked)
        {
            if (nodeImage != null) nodeImage.enabled = false;
            if (buttonComponent != null) buttonComponent.interactable = false;
            if (nodeMedalImage != null) nodeMedalImage.gameObject.SetActive(false); // Hide medal if locked
        }
        else
        {
            if (nodeImage != null) nodeImage.enabled = true;
            if (buttonComponent != null) buttonComponent.interactable = true;

            // Show medal on the node if the level is unlocked and beaten
            DisplayEarnedMedalOnNode();
        }
    }

    private void DisplayEarnedMedalOnNode()
    {
        if (nodeMedalImage == null) return;

        float bestTime = PlayerPrefs.GetFloat(levelName + "_BestTime", 0.0f);
        Sprite earnedMedalSprite = null;

        // Calculate which medal was earned based on bestTime records
        if (bestTime > 0f && performanceMedals != null && performanceMedals.Length >= 3)
        {
            if (bestTime <= goldTimeTarget)
            {
                earnedMedalSprite = performanceMedals[2]; // Gold (Index 2)
            }
            else if (bestTime <= silverTimeTarget)
            {
                earnedMedalSprite = performanceMedals[1]; // Silver (Index 1)
            }
            else if (bestTime <= bronzeTimeTarget)
            {
                earnedMedalSprite = performanceMedals[0]; // Bronze (Index 0)
            }
        }

        // Apply visual adjustments to the badge slot
        if (earnedMedalSprite != null)
        {
            nodeMedalImage.gameObject.SetActive(true);
            nodeMedalImage.sprite = earnedMedalSprite;
        }
        else
        {
            // Turn it off if the user unlocked the level but hasn't beaten it yet
            nodeMedalImage.gameObject.SetActive(false);
        }
    }

    public void RefreshUnlockState()
    {
        CheckUnlockProgression();
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
        TriggerNodeSelection();
    }

    public void TriggerNodeSelection()
    {
        if (!isUnlocked || detailsPanel == null) return;

        int gemsFound = PlayerPrefs.GetInt(levelName + "_Collectibles", 0);
        float bestTime = PlayerPrefs.GetFloat(levelName + "_BestTime", 0.0f);

        Sprite earnedMedalSprite = null;
        if (bestTime > 0f && performanceMedals != null && performanceMedals.Length >= 3)
        {
            if (bestTime <= goldTimeTarget) earnedMedalSprite = performanceMedals[2];
            else if (bestTime <= silverTimeTarget) earnedMedalSprite = performanceMedals[1];
            else if (bestTime <= bronzeTimeTarget) earnedMedalSprite = performanceMedals[0];
        }

        Vector3 targetPanelPos = panelAnchor != null ? panelAnchor.position : transform.position;

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
        PlayerPrefs.SetFloat(levelName + "_BestTime", goldTimeTarget - 1f);
        PlayerPrefs.Save();
        CheckUnlockProgression(); // Updated to dynamically refresh layout instantly upon execution
        Debug.Log("<color=green>DevCheat:</color> Injected Max Stats Data Profile!");
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