using System.Collections;
using UnityEngine;

public class LevelSelectManager : MonoBehaviour
{
    private enum CameraState { AtTitle, ZoomingToMap, AtMap, ZoomingToTitle, ZoomedInOnLevel, ZoomingToOptions, AtOptions }

    [Header("Camera Positions")]
    [SerializeField] private Vector3 titlePosition;
    [SerializeField] private Vector3 titleRotation;
    [SerializeField] private Vector3 mapPosition;
    [SerializeField] private Vector3 mapRotation;
    [SerializeField] private Vector3 optionsPosition; // Added Options Position
    [SerializeField] private Vector3 optionsRotation; // Added Options Rotation

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
            PlayerPrefs.SetString("LastPlayedLevel", "");
            PlayerPrefs.Save();

            transform.position = mapPosition;
            transform.rotation = Quaternion.Euler(mapRotation);
            currentState = CameraState.AtMap;

            if (mainMenuManager != null)
            {
                mainMenuManager.DisableTitlePanel();
                mainMenuManager.EnableLevelSelectUI(false);
            }

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
        yield return null;
        yield return null;

        LevelMapNode[] allNodes = FindObjectsByType<LevelMapNode>();

        foreach (LevelMapNode node in allNodes)
        {
            node.RefreshUnlockState();
        }

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
        else if (currentState == CameraState.ZoomingToOptions) // Added Options Zoom Update
        {
            ExecuteCamLerp(optionsPosition, optionsRotation, CameraState.AtOptions, () => mainMenuManager?.EnableOptionsUI());
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

    public void StartOptionsZoomTransition() // Added Call to zoom into Options
    {
        currentState = CameraState.ZoomingToOptions;
    }

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