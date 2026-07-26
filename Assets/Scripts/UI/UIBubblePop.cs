using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIBubblePop : MonoBehaviour, IPointerClickHandler
{
    [Header("Effects & Sizing")]
    [SerializeField] private GameObject popParticlePrefab;
    [SerializeField] private Vector3 popEffectScale = Vector3.one;
    [SerializeField] private float particleSystemLifetime = 0.5f;

    [Header("Cleanup Options")]
    [Tooltip("If true, the entire GameObject is removed from the scene.")]
    [SerializeField] private bool shouldDestroyButton = true;

    [Tooltip("If true, the button's image will disappear when clicked. Set to false if you want it to stay visible.")]
    [SerializeField] private bool shouldHideVisuals = true;

    [SerializeField] private Graphic uiGraphicToHide;

    private Canvas parentCanvas;
    private Camera targetCamera;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            targetCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
        }
        else
        {
            targetCamera = Camera.main;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector3 spawnPosition = transform.position;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera && targetCamera != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                GetComponent<RectTransform>(),
                eventData.position,
                targetCamera,
                out spawnPosition
            );
        }
        else
        {
            spawnPosition = Input.mousePosition;
            if (targetCamera != null)
            {
                spawnPosition.z = parentCanvas != null ? parentCanvas.planeDistance : 10f;
                spawnPosition = targetCamera.ScreenToWorldPoint(spawnPosition);
            }
        }

        // 1. Spawn the pop particles at the exact click point
        if (popParticlePrefab != null)
        {
            GameObject particles = Instantiate(popParticlePrefab, spawnPosition, Quaternion.identity);
            particles.transform.localScale = Vector3.Scale(transform.lossyScale, popEffectScale);
            Destroy(particles, particleSystemLifetime);
        }

        // 2. Handle button disappearance based on your new settings
        if (shouldHideVisuals)
        {
            if (uiGraphicToHide != null)
            {
                uiGraphicToHide.enabled = false;
            }
            else
            {
                Graphic currentGraphic = GetComponent<Graphic>();
                if (currentGraphic != null) currentGraphic.enabled = false;
            }
        }

        // 3. Handle total destruction
        if (shouldDestroyButton)
        {
            Destroy(gameObject);
        }
    }
}