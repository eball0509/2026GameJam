using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{

    [Header("Look Settings")]
    public float mouseSense = 4f;
    public Transform playerBody;

    [Header("Camera Keybinds")]
    public Key lookBehindKey = Key.C;
    public Key togglePerspectiveKey = Key.V;

    [Header("Look Behind Settings")]
    public float lookBehindPanSpeed = 15f;

    [Header("Perspective")]
    public float thirdPersonDistance = 4f;
    public float perspectiveSwitchSpeed = 10f;
    public LayerMask wallClippingLayers;

    [Header("Third Person Framing")]
    public float characterTurnSpeed = 10f;
    public float thirdPersonHeightOffset = 0.45f;
    public float thirdPersonPitchOffset = 10f;
    public float minThirdPersonX = -30f;
    public float maxThirdPersonX = 60f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentCameraY = 0f;

    public bool IsThirdPerson => isThirdPerson;
    public float GetCleanYRotation => yRotation;

    private bool isThirdPerson = false;

    private float currentCameraDistance = 0f;
    private float currentHeightOffset = 0f;
    private float currentPitchOffset = 0f;

    private Vector3 defaultLocalPosition;
    private PlayerController playerMovement;

    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        defaultLocalPosition = transform.localPosition;

        if (playerBody != null)
        {
            playerMovement = playerBody.GetComponent<PlayerController>();
        }

    }

    void Update()
    {

        if (Keyboard.current[togglePerspectiveKey].wasPressedThisFrame)
        {
            isThirdPerson = !isThirdPerson;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float clampedX = Mathf.Clamp(mouseDelta.x, -100f, 100f);
        float clampedY = Mathf.Clamp(mouseDelta.y, -100f, 100f);

        xRotation -= clampedY * mouseSense * 0.01f;

        if (isThirdPerson)
        {
            xRotation = Mathf.Clamp(xRotation, minThirdPersonX, maxThirdPersonX);
        }
        else
        {
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        }

        yRotation += clampedX * mouseSense * 0.01f;

    }

    void LateUpdate()
    {

        bool isMoving = false;
        if (playerMovement != null)
        {
            isMoving = Keyboard.current[playerMovement.moveForwardKey].isPressed ||
                       Keyboard.current[playerMovement.moveLeftKey].isPressed ||
                       Keyboard.current[playerMovement.moveBackwardKey].isPressed ||
                       Keyboard.current[playerMovement.moveRightKey].isPressed;
        }

        if (!isThirdPerson)
        {
            playerBody.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        else if (isMoving)
        {
            float moveInputX = 0f;
            float moveInputZ = 0f;

            if (Keyboard.current[playerMovement.moveLeftKey].isPressed) moveInputX = -1f;
            if (Keyboard.current[playerMovement.moveRightKey].isPressed) moveInputX = 1f;
            if (Keyboard.current[playerMovement.moveBackwardKey].isPressed) moveInputZ = -1f;
            if (Keyboard.current[playerMovement.moveForwardKey].isPressed) moveInputZ = 1f;

            Vector3 camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 moveDirection = (camRight * moveInputX + camForward * moveInputZ).normalized;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetBodyRotation = Quaternion.LookRotation(moveDirection);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, characterTurnSpeed * Time.deltaTime);
            }

        }

        // Animate looking behind
        bool isLookingBehind = Keyboard.current[lookBehindKey].isPressed;
        float targetCameraY = isLookingBehind ? 180f : 0f;
        currentCameraY = Mathf.LerpAngle(currentCameraY, targetCameraY, lookBehindPanSpeed * Time.deltaTime);

        // Animate offsets
        float targetHeight = isThirdPerson ? thirdPersonHeightOffset : 0f;
        float targetPitch = isThirdPerson ? thirdPersonPitchOffset : 0f;
        currentHeightOffset = Mathf.Lerp(currentHeightOffset, targetHeight, perspectiveSwitchSpeed * Time.deltaTime);
        currentPitchOffset = Mathf.Lerp(currentPitchOffset, targetPitch, perspectiveSwitchSpeed * Time.deltaTime);

        float finalCameraY = isThirdPerson ? yRotation : playerBody.eulerAngles.y;
        transform.rotation = Quaternion.Euler(xRotation + currentPitchOffset, finalCameraY + currentCameraY, 0f);

        float targetDistance = isThirdPerson ? thirdPersonDistance : 0f;
        Vector3 worldOrigin = playerBody.TransformPoint(defaultLocalPosition) + (Vector3.up * currentHeightOffset);

        if (isThirdPerson && Physics.Raycast(worldOrigin, -transform.forward, out RaycastHit hit, thirdPersonDistance, wallClippingLayers))
        {
            targetDistance = hit.distance - 0.2f;
        }

        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetDistance, perspectiveSwitchSpeed * Time.deltaTime);
        transform.position = worldOrigin - (transform.forward * currentCameraDistance);
    }
}