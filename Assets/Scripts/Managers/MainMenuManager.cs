using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject titleMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject levelSelectCanvasUI;

    [Header("Transition Settings")]
    [Tooltip("Reference to the script or camera system that triggers the 3D room camera zoom.")]
    [SerializeField] private LevelSelectManager levelSelectManager;

    private void Start()
    {
        // Ensure only the Main Title Screen layout is active on startup
        if (titleMenuPanel != null) titleMenuPanel.SetActive(true);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (levelSelectCanvasUI != null) levelSelectCanvasUI.SetActive(false);
    }

    public void OnPlayClicked()
    {
        if (titleMenuPanel != null) titleMenuPanel.SetActive(false);

        if (levelSelectManager != null)
        {
            //levelSelectManager.StartMenuZoomTransition();
        }
        else
        {
            EnableLevelSelectUI();
        }
    }

    public void EnableLevelSelectUI()
    {
        if (levelSelectCanvasUI != null) levelSelectCanvasUI.SetActive(true);
    }

    public void OnOptionsClicked()
    {
        if (titleMenuPanel != null) titleMenuPanel.SetActive(false);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(true);
    }

    public void OnBackFromOptionsClicked()
    {
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (titleMenuPanel != null) titleMenuPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Exiting Game Application...");
        Application.Quit();
    }
}
