using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Added to check for Raycast Target

public class UIHoverTilt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tilt Settings")]
    [Range(1f, 30f)] public float maxTiltAngle = 15f;
    public float smoothSpeed = 10f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera targetCamera;
    private bool isHovered = false;
    private Quaternion targetRotation;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetRotation = Quaternion.identity;

        // Find the parent canvas to get the correct camera reference
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            // If the canvas uses a camera, use it. Otherwise, fall back to Main Camera.
            targetCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
        }
        else
        {
            targetCamera = Camera.main;
        }

        // Quick safety check: Alert if Raycast Target is turned off
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            Debug.LogWarning($"[UIHoverTilt] '{gameObject.name}' does not have 'Raycast Target' enabled! Hover events will not trigger.", gameObject);
        }
    }

    void Update()
    {
        if (isHovered)
        {
            Vector2 localMousePos;

            // FIX: Passed targetCamera instead of null so coordinate conversion works with Screen Space - Camera
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                Input.mousePosition,
                targetCamera,
                out localMousePos
            );

            // Normalize the position based on the size of the UI panel (-0.5 to 0.5 range)
            float normX = localMousePos.x / rectTransform.rect.width;
            float normY = localMousePos.y / rectTransform.rect.height;

            // Clamp values just in case the mouse moves too fast off the edge
            normX = Mathf.Clamp(normX, -0.5f, 0.5f);
            normY = Mathf.Clamp(normY, -0.5f, 0.5f);

            // Calculate rotation angles (Invert X/Y axes appropriately for the lean look)
            float tiltX = -normY * maxTiltAngle;
            float tiltY = normX * maxTiltAngle;

            targetRotation = Quaternion.Euler(tiltX, tiltY, 0f);
        }
        else
        {
            // Return back to flat when the mouse leaves
            targetRotation = Quaternion.identity;
        }

        // Smoothly rotate toward the target angle
        rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, smoothSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}