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

    // Tracks if we need to run the UI fade routine
    private bool comingFromMainMenu = false;

    private void Start()
    {
        transform.position = titlePosition;
        transform.rotation = Quaternion.Euler(titleRotation);
        currentState = CameraState.AtTitle;
    }

    private void Update()
    {
        if (currentState == CameraState.ZoomingToMap)
        {
            ExecuteCamLerp(mapPosition, mapRotation, CameraState.AtMap, () =>
            {
                if (mainMenuManager != null)
                {
                    // If we came from the main menu, do a fresh fade. Otherwise, skip the fade!
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
        comingFromMainMenu = true; // Yes, we want the nice intro fade!
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

        comingFromMainMenu = false; // False! We are just backing away from a level node, skip the fade.
        specificTargetPosition = mapPosition;
        currentState = CameraState.ZoomingToMap;
    }
}