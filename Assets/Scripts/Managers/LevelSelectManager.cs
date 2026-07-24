using System.Collections;
using UnityEngine;

public class LevelSelectManager : MonoBehaviour
{
    private enum CameraState { AtTitle, ZoomingToMap, AtMap, ZoomingToTitle, ZoomedInOnLevel }

    [Header("Camera Positions")]
    [SerializeField] private Vector3 titlePosition;
    [SerializeField] private Vector3 titleRotation;
    [SerializeField] private Vector3 mapPosition;
    [SerializeField] private Vector3 mapRotation;

    [Header("Zoom settings")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float localNodeZoomHeight = 5f;

    [Header("References")]
    [SerializeField] private MainMenuManager mainMenuManager;
    [SerializeField] private LevelDetailsPanel detailsPanel;

    private CameraState currentState = CameraState.AtTitle;
    private Vector3 specificTargetPosition;
    private bool comingFromMainMenu = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string lastPlayedLevel = PlayerPrefs.GetString("LastPlayedLevel", "");

        if (!string.IsNullOrEmpty(lastPlayedLevel))
        {
            // Clear the key immediately
            PlayerPrefs.SetString("LastPlayedLevel", "");
            PlayerPrefs.Save();

            // Snap camera directly to the map position
            transform.position = mapPosition;
            transform.rotation = Quaternion.Euler(mapRotation);
            currentState = CameraState.AtMap;

            // Force Title screen OFF and Level Select canvas ON immediately
            if (mainMenuManager != null)
            {
                mainMenuManager.DisableTitlePanel();
                mainMenuManager.EnableLevelSelectUI(false);
            }

            // Start the initialization delay loop to let UI systems wake up
            StartCoroutine(ReturnToNodeRoutine(lastPlayedLevel));
        }
        else
        {
            transform.position = titlePosition;
            transform.rotation = Quaternion.Euler(titleRotation);
            currentState = CameraState.AtTitle;
        }
    }

    private IEnumerator ReturnToNodeRoutine(string lastPlayedLevelName)
    {
        // Wait two full frames to ensure the Canvas UI Hierarchy is fully turned on,
        // UI graphics elements have processed layout bounds, and Awake/Start loops have run.
        yield return null;
        yield return null;

        LevelMapNode[] allNodes = FindObjectsByType<LevelMapNode>();

        // 1. Explicitly force all nodes to read their updated PlayerPrefs
        foreach (LevelMapNode node in allNodes)
        {
            node.RefreshUnlockState();
        }

        // 2. Find the active node matching the target name and call selection panel logic
        foreach (LevelMapNode node in allNodes)
        {
            if (node.levelName.Equals(lastPlayedLevelName, System.StringComparison.OrdinalIgnoreCase))
            {
                node.TriggerNodeSelection();
                break;
            }
        }
    }

    private void Update()
    {
        if (currentState == CameraState.ZoomingToMap)
        {
            ExecuteCamLerp(mapPosition, mapRotation, CameraState.AtMap, () =>
            {
                if (mainMenuManager != null)
                {
                    mainMenuManager.EnableLevelSelectUI(comingFromMainMenu);
                }
            });
        }
        else if (currentState == CameraState.ZoomingToTitle)
        {
            ExecuteCamLerp(titlePosition, titleRotation, CameraState.AtTitle, () => mainMenuManager?.EnableTitleMenuUI());
        }
        else if (currentState == CameraState.ZoomedInOnLevel)
        {
            ExecuteCamLerp(specificTargetPosition, mapRotation, CameraState.ZoomedInOnLevel, null);
        }
    }

    private void ExecuteCamLerp(Vector3 targetPos, Vector3 targetRot, CameraState endState, System.Action callback)
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, zoomSpeed * Time.deltaTime);
        Quaternion rot = Quaternion.Euler(targetRot);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, zoomSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.05f && currentState != CameraState.ZoomedInOnLevel)
        {
            transform.position = targetPos;
            transform.rotation = rot;
            currentState = endState;
            callback?.Invoke();
        }
    }

    public void StartMenuZoomTransition()
    {
        comingFromMainMenu = true;
        currentState = CameraState.ZoomingToMap;
    }

    public void StartMenuZoomOutTransition() => currentState = CameraState.ZoomingToTitle;

    public void ZoomIntoLevelNode(Vector3 targetWorldPosition)
    {
        specificTargetPosition = new Vector3(targetWorldPosition.x, mapPosition.y - (localNodeZoomHeight * 0.3f), targetWorldPosition.z - 1.2f);
        currentState = CameraState.ZoomedInOnLevel;
    }

    public void ReturnFromLevelNode()
    {
        if (detailsPanel != null) detailsPanel.ClosePanel();

        comingFromMainMenu = false;
        specificTargetPosition = mapPosition;
        currentState = CameraState.ZoomingToMap;
    }
}