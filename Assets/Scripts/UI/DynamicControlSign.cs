using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class DynamicControlSign : MonoBehaviour
{
    [TextArea(3, 6)]
    [Tooltip("Use specific numbers for specific keys:\n{0} = Forward\n{1} = Left\n{2} = Backward\n{3} = Right\n{4} = Jump\n{5} = Roll\n{6} = LookBehind\n{7} = Perspective")]
    [SerializeField] private string textTemplate = "Move: {0}{1}{2}{3} | Jump: {4} | Roll: {5}";

    private TextMeshPro textMeshComponent;

    private void Awake()
    {
        textMeshComponent = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        UpdateSignText();
    }

    public void UpdateSignText()
    {
        if (textMeshComponent == null) return;

        // Gather all key strings into an array matching the tooltip index order
        string[] keyStrings = new string[8];
        keyStrings[0] = CleanKeyName(OptionsManager.MoveForward.ToString());
        keyStrings[1] = CleanKeyName(OptionsManager.MoveLeft.ToString());
        keyStrings[2] = CleanKeyName(OptionsManager.MoveBackward.ToString());
        keyStrings[3] = CleanKeyName(OptionsManager.MoveRight.ToString());
        keyStrings[4] = CleanKeyName(OptionsManager.Jump.ToString());
        keyStrings[5] = CleanKeyName(OptionsManager.Roll.ToString());
        keyStrings[6] = CleanKeyName(OptionsManager.LookBehind.ToString());
        keyStrings[7] = CleanKeyName(OptionsManager.TogglePerspective.ToString());

        // string.Format now receives the entire array of keys safely!
        textMeshComponent.text = string.Format(textTemplate, keyStrings);
    }

    private string CleanKeyName(string rawKeyName)
    {
        if (rawKeyName == "LeftCtrl") return "Left Ctrl";
        if (rawKeyName == "RightCtrl") return "Right Ctrl";
        if (rawKeyName == "LeftShift") return "Left Shift";
        if (rawKeyName == "RightShift") return "Right Shift";
        return rawKeyName;
    }
}