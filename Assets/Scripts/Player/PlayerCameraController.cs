using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{

    [Header("Look Settings")]
    public float mouseSense = 15f;
    public Transform playerBody;

    [Header("Look Behind Settings")]
    public Key lookBehindKey = Key.C;
    public float lookBehindPanSpeed = 15f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private float currentCameraY = 0f;

    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 

    }

    void LateUpdate()
    {

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float clampedX = Mathf.Clamp(mouseDelta.x, -100f, 100f);
        float clampedY = Mathf.Clamp(mouseDelta.y, -100f, 100f);

        float mouseX = clampedX * mouseSense * Time.deltaTime;
        float mouseY = clampedY * mouseSense * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;

        playerBody.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        bool isLookingBehind = Keyboard.current[lookBehindKey].isPressed;
        float targetCameraY = isLookingBehind ? 180f : 0f;

        currentCameraY = Mathf.LerpAngle(currentCameraY, targetCameraY, lookBehindPanSpeed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(xRotation, currentCameraY, 0f);
    }
}
