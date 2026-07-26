using UnityEngine;

public class ProceduralSpeedPad : MonoBehaviour
{
    [Header("Visual Configurations")]
    [SerializeField] private Color padBaseColor = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Color arrowColor = Color.cyan;
    [Tooltip("Change to a negative number if arrows are moving backward!")]
    [SerializeField] private float scrollSpeed = 3f;
    [SerializeField] private int arrowDensity = 5;

    private Material padMaterial;

    private void Start()
    {
        // Finds the Unlit Shader asset asset file we created above
        Shader runtimeShader = Shader.Find("Custom/ProceduralScrollArrows");

        if (runtimeShader != null)
        {
            padMaterial = new Material(runtimeShader);
        }
        else
        {
            Debug.LogError("Custom/ProceduralScrollArrows shader file not found! Defaulting to basic fallback.");
            padMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = padMaterial;
        }
    }

    private void Update()
    {
        if (padMaterial != null)
        {
            padMaterial.SetColor("_BaseColor", padBaseColor);
            padMaterial.SetColor("_ArrowColor", arrowColor);
            padMaterial.SetFloat("_Speed", scrollSpeed);
            padMaterial.SetFloat("_Density", arrowDensity);
        }
    }
}