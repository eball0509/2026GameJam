using UnityEngine;
using TMPro;

public class RebindButtonHelper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Type exactly: Forward, Left, Backward, Right, Jump, LookBehind, or Perspective")]
    [SerializeField] private string actionId;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI buttonText;

    private OptionsManager optionsManager;

    private void Start()
    {
        optionsManager = FindAnyObjectByType<OptionsManager>();
        RefreshButtonTextDisplay();
    }

    public void TriggerRebind()
    {
        if (optionsManager != null && buttonText != null)
        {
            optionsManager.StartRebindingAction(actionId, buttonText);
        }
        else
        {
            Debug.LogError("RebindButtonHelper is missing references!");
        }
    }

    public void RefreshButtonTextDisplay()
    {
        if (buttonText != null)
        {
            string savedKey = PlayerPrefs.GetString("Key_" + actionId, GetDefaultKeyString());

            if (savedKey == "None")
            {
                buttonText.text = "None";
            }
            else
            {
                buttonText.text = savedKey;
            }
        }
    }

    private string GetDefaultKeyString()
    {
        switch (actionId)
        {
            case "Forward": return "W";
            case "Left": return "A";
            case "Backward": return "S";
            case "Right": return "D";
            case "Jump": return "Space";
            case "LookBehind": return "C";
            case "Perspective": return "V";
            default: return "None";
        }
    }
}