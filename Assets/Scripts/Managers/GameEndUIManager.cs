using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameEndUIManager : MonoBehaviour
{
    [Header("UI Panels (Must have CanvasGroup)")]
    [SerializeField] private CanvasGroup deathPanel;
    [SerializeField] private CanvasGroup winPanel;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Gameplay HUD Settings")]
    [Tooltip("Drop your main gameplay HUD GameObject here to hide it during transitions.")]
    [SerializeField] private GameObject normalGameplayHUD;

    [Header("Buttons")]
    [SerializeField] private Button deathRetryButton;
    [SerializeField] private Button deathMenuButton;
    [SerializeField] private Button winPlayAgainButton;
    [SerializeField] private Button winMenuButton;

    [Header("Win Camera Orbit Settings")]
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float orbitHeight = 2f;
    [SerializeField] private float orbitSpeed = 20f;

    private PlayerController player;
    private Transform mainCameraTransform;
    private bool isOrbiting = false;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        SetupPanelInitialState(deathPanel);
        SetupPanelInitialState(winPanel);

        deathRetryButton.onClick.AddListener(RestartLevel);
        deathMenuButton.onClick.AddListener(QuitToMainMenu);
        winPlayAgainButton.onClick.AddListener(RestartLevel);
        winMenuButton.onClick.AddListener(QuitToMainMenu);
    }

    private void Update()
    {
        if (isOrbiting && player != null && mainCameraTransform != null)
        {
            mainCameraTransform.RotateAround(player.transform.position, Vector3.up, orbitSpeed * Time.deltaTime);
            Vector3 targetPosition = mainCameraTransform.position;
            targetPosition.y = player.transform.position.y + orbitHeight;
            mainCameraTransform.position = targetPosition;
            mainCameraTransform.LookAt(player.transform.position + Vector3.up * 1f);
        }
    }

    public void TriggerDeathState()
    {
        // Turn off the trippy script instantly so death menus are clickable
        DisableTrippyWarpSystem();

        if (normalGameplayHUD != null) normalGameplayHUD.SetActive(false);

        StartCoroutine(FadeInPanelRoutine(deathPanel));
        UnlockMouse();
    }

    public void TriggerWinState()
    {
        // Turn off the trippy script instantly so victory/options menus are clickable
        DisableTrippyWarpSystem();

        if (normalGameplayHUD != null) normalGameplayHUD.SetActive(false);

        if (player != null)
        {
            player.SetInvincible(true);
            player.enabled = false;

            PlayerCameraController camControl = player.GetComponentInChildren<PlayerCameraController>();
            if (camControl != null) camControl.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            player.TriggerVictoryDance();
        }

        isOrbiting = true;
        StartCoroutine(FadeInPanelRoutine(winPanel));
        UnlockMouse();
    }

    // --- NEW HELPER METHOD TO FIND AND DISABLE THE TRIPPY CONTROLLER ---
    private void DisableTrippyWarpSystem()
    {
        TrippyEffectController trippyController = FindAnyObjectByType<TrippyEffectController>();
        if (trippyController != null)
        {
            trippyController.enabled = false;
        }
    }

    private void SetupPanelInitialState(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        panel.interactable = false;
    }

    private IEnumerator FadeInPanelRoutine(CanvasGroup panel)
    {
        if (panel == null) yield break;

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        panel.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        panel.alpha = 1f;
        panel.interactable = true;
    }

    private void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        LevelManager lm = FindAnyObjectByType<LevelManager>();
        string menuName = lm != null ? PlayerPrefs.GetString("LastPlayedLevel", "MainMenu") : "MainMenu";
        SceneManager.LoadScene(menuName);
    }
}