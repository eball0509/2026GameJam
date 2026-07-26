using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Sub-Panels")]
    [SerializeField] private GameObject gameSubPanel;
    [SerializeField] private GameObject graphicsSubPanel;
    [SerializeField] private GameObject keybindsSubPanel;
    [SerializeField] private GameObject audioSubPanel;

    [Header("Graphics UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Game Settings UI")]
    [SerializeField] private Slider firstPersonFovSlider;
    [SerializeField] private Toggle screenShakeToggle;

    // --- STATIC GLOBAL KEYBINDS ---
    public static Key MoveForward { get; private set; } = Key.W;
    public static Key MoveLeft { get; private set; } = Key.A;
    public static Key MoveBackward { get; private set; } = Key.S;
    public static Key MoveRight { get; private set; } = Key.D;
    public static Key Jump { get; private set; } = Key.Space;
    public static Key Roll { get; private set; } = Key.LeftCtrl;
    public static Key LookBehind { get; private set; } = Key.C;
    public static Key TogglePerspective { get; private set; } = Key.V;

    public static float FirstPersonFOV { get; private set; } = 75f;
    public static bool IsScreenShakeEnabled { get; private set; } = true;

    private Resolution[] availableResolutions;
    private string activeBindingActionName = null;
    private TextMeshProUGUI activeBindingButtonText = null;

    private void Awake()
    {
        LoadSettings();
    }

    private void Start()
    {
        SetupResolutionDropdown();
        SetupGraphicsOptionsDefaults();
        SetupGameOptionsDefaults();
        OpenGameTab();
    }

    private void OnGUI()
    {
        if (activeBindingActionName != null && Event.current.isKey && Event.current.type == EventType.KeyDown)
        {
            KeyCode code = Event.current.keyCode;
            Key pressedInputKey = ConvertKeyCodeToInputSystemKey(code);

            if (pressedInputKey != Key.None)
            {
                ApplyAndSaveKeybind(activeBindingActionName, pressedInputKey);

                // Clear state
                activeBindingActionName = null;
                activeBindingButtonText = null;
            }
        }
    }

    public void OpenGameTab() { OpenTab(gameSubPanel); }
    public void OpenGraphicsTab() { OpenTab(graphicsSubPanel); }
    public void OpenKeybindsTab() { OpenTab(keybindsSubPanel); }
    public void OpenAudioTab() { OpenTab(audioSubPanel); }

    private void OpenTab(GameObject panelToOpen)
    {
        if (gameSubPanel != null) gameSubPanel.SetActive(false);
        if (graphicsSubPanel != null) graphicsSubPanel.SetActive(false);
        if (keybindsSubPanel != null) keybindsSubPanel.SetActive(false);
        if (audioSubPanel != null) audioSubPanel.SetActive(false);

        if (panelToOpen != null) panelToOpen.SetActive(true);
    }

    public void StartRebindingAction(string actionId, TextMeshProUGUI layoutLabelText)
    {
        activeBindingActionName = actionId;
        activeBindingButtonText = layoutLabelText;
        layoutLabelText.text = "...";
    }

    private void ApplyAndSaveKeybind(string actionId, Key newBoundKey)
    {
        CheckAndClearConflict(newBoundKey, actionId);

        switch (actionId)
        {
            case "Forward": MoveForward = newBoundKey; break;
            case "Left": MoveLeft = newBoundKey; break;
            case "Backward": MoveBackward = newBoundKey; break;
            case "Right": MoveRight = newBoundKey; break;
            case "Jump": Jump = newBoundKey; break;
            case "Roll": Roll = newBoundKey; break;
            case "LookBehind": LookBehind = newBoundKey; break;
            case "Perspective": TogglePerspective = newBoundKey; break;
        }

        PlayerPrefs.SetString("Key_" + actionId, newBoundKey.ToString());
        PlayerPrefs.Save();
        Debug.Log($"Rebind saved: {actionId} mapped to {newBoundKey}");

        RefreshAllRebindButtonsUI();
    }

    private void CheckAndClearConflict(Key targetKey, string clearExceptionActionId)
    {
        if (targetKey == Key.None) return;

        if (MoveForward == targetKey && clearExceptionActionId != "Forward") { MoveForward = Key.None; SaveUnboundAction("Forward"); }
        if (MoveLeft == targetKey && clearExceptionActionId != "Left") { MoveLeft = Key.None; SaveUnboundAction("Left"); }
        if (MoveBackward == targetKey && clearExceptionActionId != "Backward") { MoveBackward = Key.None; SaveUnboundAction("Backward"); }
        if (MoveRight == targetKey && clearExceptionActionId != "Right") { MoveRight = Key.None; SaveUnboundAction("Right"); }
        if (Jump == targetKey && clearExceptionActionId != "Jump") { Jump = Key.None; SaveUnboundAction("Jump"); }
        if (Roll == targetKey && clearExceptionActionId != "Roll") { Roll = Key.None; SaveUnboundAction("Roll"); }
        if (LookBehind == targetKey && clearExceptionActionId != "LookBehind") { LookBehind = Key.None; SaveUnboundAction("LookBehind"); }
        if (TogglePerspective == targetKey && clearExceptionActionId != "Perspective") { TogglePerspective = Key.None; SaveUnboundAction("Perspective"); }
    }

    private void SaveUnboundAction(string actionId)
    {
        PlayerPrefs.SetString("Key_" + actionId, Key.None.ToString());
        PlayerPrefs.Save();
        Debug.Log($"Conflict resolved: {actionId} has been cleared/unbound.");
    }

    private void RefreshAllRebindButtonsUI()
    {
        RebindButtonHelper[] helpers = UnityEngine.Object.FindObjectsByType<RebindButtonHelper>();

        foreach (RebindButtonHelper helper in helpers)
        {
            helper.RefreshButtonTextDisplay();
        }

        // Inside OptionsManager.cs -> RefreshAllRebindButtonsUI()
        DynamicControlSign[] signs = Object.FindObjectsByType<DynamicControlSign>();
        foreach (DynamicControlSign sign in signs) sign.UpdateSignText();
    }

    private Key ConvertKeyCodeToInputSystemKey(KeyCode legacyCode)
    {
        // Fix legacy names to match the modern Input System names
        string enumNormalizedName = legacyCode.ToString();

        if (legacyCode == KeyCode.LeftControl) enumNormalizedName = "LeftCtrl";
        if (legacyCode == KeyCode.RightControl) enumNormalizedName = "RightCtrl";
        if (legacyCode == KeyCode.LeftShift) enumNormalizedName = "LeftShift";
        if (legacyCode == KeyCode.RightShift) enumNormalizedName = "RightShift";
        if (legacyCode == KeyCode.LeftAlt) enumNormalizedName = "LeftAlt";
        if (legacyCode == KeyCode.RightAlt) enumNormalizedName = "RightAlt";

        if (System.Enum.TryParse(enumNormalizedName, true, out Key nativeKey))
        {
            return nativeKey;
        }
        return Key.None;
    }

    private void SetupGameOptionsDefaults()
    {
        if (firstPersonFovSlider != null)
        {
            firstPersonFovSlider.minValue = 60f;
            firstPersonFovSlider.maxValue = 110f;
            firstPersonFovSlider.value = FirstPersonFOV;
            firstPersonFovSlider.onValueChanged.AddListener(SetFirstPersonFOV);
        }

        if (screenShakeToggle != null)
        {
            screenShakeToggle.isOn = IsScreenShakeEnabled;
            screenShakeToggle.onValueChanged.AddListener(SetScreenShake);
        }
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> optionsList = new List<string>();
        int savedWidth = PlayerPrefs.GetInt("ResWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResHeight", Screen.currentResolution.height);
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = availableResolutions[i].width + " x " + availableResolutions[i].height;
            optionsList.Add(option);
            if (availableResolutions[i].width == savedWidth && availableResolutions[i].height == savedHeight)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(optionsList);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void SetupGraphicsOptionsDefaults()
    {
        if (screenModeDropdown != null)
        {
            screenModeDropdown.ClearOptions();
            List<string> modes = new List<string> { "Fullscreen", "Windowed" };
            screenModeDropdown.AddOptions(modes);
            screenModeDropdown.value = Screen.fullScreen ? 0 : 1;
            screenModeDropdown.RefreshShownValue();
            screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> names = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(names);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(SetQualityPreset);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length) return;
        Resolution selectedRes = availableResolutions[resolutionIndex];
        Screen.SetResolution(selectedRes.width, selectedRes.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResWidth", selectedRes.width);
        PlayerPrefs.SetInt("ResHeight", selectedRes.height);
        PlayerPrefs.Save();
    }

    public void SetScreenMode(int index) { Screen.fullScreen = (index == 0); PlayerPrefs.SetInt("ScreenMode", index); PlayerPrefs.Save(); }
    public void SetVSync(bool isEnabled) { QualitySettings.vSyncCount = isEnabled ? 1 : 0; PlayerPrefs.SetInt("VSync", QualitySettings.vSyncCount); PlayerPrefs.Save(); }
    public void SetQualityPreset(int presetIndex) { QualitySettings.SetQualityLevel(presetIndex, true); PlayerPrefs.SetInt("QualityPreset", presetIndex); PlayerPrefs.Save(); }
    public void SetFirstPersonFOV(float value) { FirstPersonFOV = value; PlayerPrefs.SetFloat("FirstPersonFOV", value); PlayerPrefs.Save(); }
    public void SetScreenShake(bool isEnabled) { IsScreenShakeEnabled = isEnabled; PlayerPrefs.SetInt("ScreenShake", isEnabled ? 1 : 0); PlayerPrefs.Save(); }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("ScreenMode")) Screen.fullScreen = (PlayerPrefs.GetInt("ScreenMode") == 0);
        if (PlayerPrefs.HasKey("VSync")) QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync");
        if (PlayerPrefs.HasKey("QualityPreset")) QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualityPreset"), true);
        if (PlayerPrefs.HasKey("ResWidth") && PlayerPrefs.HasKey("ResHeight")) Screen.SetResolution(PlayerPrefs.GetInt("ResWidth"), PlayerPrefs.GetInt("ResHeight"), Screen.fullScreen);

        FirstPersonFOV = PlayerPrefs.GetFloat("FirstPersonFOV", 75f);
        IsScreenShakeEnabled = (PlayerPrefs.GetInt("ScreenShake", 1) == 1);

        MoveForward = LoadKeybindFromStorage("Forward", Key.W);
        MoveLeft = LoadKeybindFromStorage("Left", Key.A);
        MoveBackward = LoadKeybindFromStorage("Backward", Key.S);
        MoveRight = LoadKeybindFromStorage("Right", Key.D);
        Jump = LoadKeybindFromStorage("Jump", Key.Space);
        Roll = LoadKeybindFromStorage("Roll", Key.LeftCtrl);
        LookBehind = LoadKeybindFromStorage("LookBehind", Key.C);
        TogglePerspective = LoadKeybindFromStorage("Perspective", Key.V);
    }

    private Key LoadKeybindFromStorage(string actionId, Key fallbackDefaultKey)
    {
        string rawSavedValue = PlayerPrefs.GetString("Key_" + actionId, fallbackDefaultKey.ToString());
        if (System.Enum.TryParse(rawSavedValue, out Key runtimeKey))
        {
            return runtimeKey;
        }
        return fallbackDefaultKey;
    }
}