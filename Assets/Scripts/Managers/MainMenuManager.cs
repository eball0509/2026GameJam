using System.Collections;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject titleMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject levelSelectCanvasUI;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Transition Settings")]
    [SerializeField] private LevelSelectManager levelSelectManager;

    private Coroutine activeFadeCoroutine;

    private void Start()
    {
        if (titleMenuPanel != null)
        {
            titleMenuPanel.SetActive(true);
            TriggerFadeIn(titleMenuPanel);
        }
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (levelSelectCanvasUI != null) levelSelectCanvasUI.SetActive(false);
    }

    public void DisableTitlePanel()
    {
        if (titleMenuPanel != null) titleMenuPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        if (titleMenuPanel != null) titleMenuPanel.SetActive(false);

        if (levelSelectManager != null)
        {
            levelSelectManager.StartMenuZoomTransition();
        }
        else
        {
            EnableLevelSelectUI(true);
        }
    }

    public void OnBackFromLevelSelectClicked()
    {
        if (levelSelectCanvasUI != null) levelSelectCanvasUI.SetActive(false);

        if (levelSelectManager != null)
        {
            levelSelectManager.StartMenuZoomOutTransition();
        }
        else
        {
            EnableTitleMenuUI();
        }
    }

    public void EnableLevelSelectUI(bool shouldFade = true)
    {
        if (levelSelectCanvasUI != null)
        {
            levelSelectCanvasUI.SetActive(true);

            if (shouldFade)
            {
                TriggerFadeIn(levelSelectCanvasUI);
            }
            else
            {
                CanvasGroup group = levelSelectCanvasUI.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
            }
        }
    }

    public void EnableTitleMenuUI()
    {
        if (titleMenuPanel != null)
        {
            titleMenuPanel.SetActive(true);
            TriggerFadeIn(titleMenuPanel);
        }
    }

    public void OnOptionsClicked()
    {
        if (titleMenuPanel != null) titleMenuPanel.SetActive(false);

        if (levelSelectManager != null)
        {
            // Camera takes over and drives to the options view pane position
            levelSelectManager.StartOptionsZoomTransition();
        }
        else
        {
            EnableOptionsUI();
        }
    }

    public void EnableOptionsUI() // Split into separate method for the camera callback hook
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(true);
            TriggerFadeIn(optionsMenuPanel);
        }
    }

    public void OnBackFromOptionsClicked()
    {
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);

        if (levelSelectManager != null)
        {
            // Zooms camera smoothly back to title setup position
            levelSelectManager.StartMenuZoomOutTransition();
        }
        else
        {
            EnableTitleMenuUI();
        }
    }

    public void OnQuitClicked()
    {
        Debug.Log("Exiting Game Application...");
        Application.Quit();
    }

    private void TriggerFadeIn(GameObject targetPanel)
    {
        CanvasGroup canvasGroup = targetPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = targetPanel.AddComponent<CanvasGroup>();
        }

        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }

        activeFadeCoroutine = StartCoroutine(FadeInRoutine(canvasGroup));
    }

    private IEnumerator FadeInRoutine(CanvasGroup group)
    {
        group.alpha = 0f;
        while (group.alpha < 1f)
        {
            group.alpha += fadeSpeed * Time.deltaTime;
            yield return null;
        }
        group.alpha = 1f;
    }
}