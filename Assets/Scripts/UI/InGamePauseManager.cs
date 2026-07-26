using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class InGamePauseManager : MonoBehaviour
{
    [Header("UI System References")]
    [SerializeField] private GameObject pauseMenuParentPanel;
    [SerializeField] private OptionsManager optionsManager;

    [Header("Camera Control Settings")]
    [Tooltip("The position and rotation the camera should hold when looking at the menu.")]
    [SerializeField] private Transform menuCameraAnchor;
    [SerializeField] private float cameraPanSpeed = 8f;
    [Tooltip("The exact FOV the camera should use while looking at the menu layout.")]
    [SerializeField] private float menuFOV = 60f;

    private PlayerCameraController playerCamController;
    private Transform mainCameraTransform;
    private Camera targetCameraComponent;

    private bool isPaused = false;
    private Coroutine cameraTransitionCoroutine;

    private void Start()
    {
        playerCamController = FindAnyObjectByType<PlayerCameraController>();
        if (playerCamController != null)
        {
            mainCameraTransform = playerCamController.transform;
            targetCameraComponent = mainCameraTransform.GetComponent<Camera>();
        }

        if (pauseMenuParentPanel != null) pauseMenuParentPanel.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        if (playerCamController != null) playerCamController.enabled = false;
        Time.timeScale = 0f;

        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        cameraTransitionCoroutine = StartCoroutine(TransitionCameraToAnchor());

        if (pauseMenuParentPanel != null) pauseMenuParentPanel.SetActive(true);
        if (optionsManager != null) optionsManager.OpenGameTab();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        if (pauseMenuParentPanel != null) pauseMenuParentPanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransitionCoroutine != null) StopCoroutine(cameraTransitionCoroutine);
        if (playerCamController != null) playerCamController.enabled = true;
    }

    private IEnumerator TransitionCameraToAnchor()
    {
        Vector3 targetWorldPos = menuCameraAnchor.position;
        Quaternion targetWorldRot = menuCameraAnchor.rotation;

        while (isPaused && menuCameraAnchor != null && mainCameraTransform != null)
        {
            // Lerp the position and rotation cleanly
            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetWorldPos, cameraPanSpeed * Time.unscaledDeltaTime);
            mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetWorldRot, cameraPanSpeed * Time.unscaledDeltaTime);

            // FORCE the Field of View to transition cleanly to the menu settings as well!
            if (targetCameraComponent != null)
            {
                targetCameraComponent.fieldOfView = Mathf.Lerp(targetCameraComponent.fieldOfView, menuFOV, cameraPanSpeed * Time.unscaledDeltaTime);
            }

            if (Vector3.Distance(mainCameraTransform.position, targetWorldPos) < 0.005f)
            {
                mainCameraTransform.position = targetWorldPos;
                mainCameraTransform.rotation = targetWorldRot;
                if (targetCameraComponent != null) targetCameraComponent.fieldOfView = menuFOV;
                yield break;
            }

            yield return null;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu(string mainMenuSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}