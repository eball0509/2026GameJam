using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Keybinds")]
    public Key moveForwardKey = Key.W;
    public Key moveLeftKey = Key.A;
    public Key moveBackwardKey = Key.S;
    public Key moveRightKey = Key.D;
    public Key jumpKey = Key.Space;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 7f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    private Rigidbody rb;
    private bool isGrounded;

    private float moveX;
    private float moveZ;

    private PlayerCameraController camController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Finds camera controller anywhere attached to children or parent setup safely
        camController = GetComponentInChildren<PlayerCameraController>();
        if (camController == null)
        {
            camController = Camera.main.GetComponent<PlayerCameraController>();
        }
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (Keyboard.current[jumpKey].wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        moveX = 0f;
        moveZ = 0f;

        if (Keyboard.current[moveLeftKey].isPressed) moveX = -1f;
        if (Keyboard.current[moveRightKey].isPressed) moveX = 1f;
        if (Keyboard.current[moveBackwardKey].isPressed) moveZ = -1f;
        if (Keyboard.current[moveForwardKey].isPressed) moveZ = 1f;
    }

    private void FixedUpdate()
    {
        Vector3 direction;

        if (camController != null && camController.IsThirdPerson)
        {
            // Calculate direction using the RAW un-flipped mouse tracking rotation
            // This guarantees that looking back completely ignores movement vectors
            Quaternion baseCamRotation = Quaternion.Euler(0f, camController.GetCleanYRotation, 0f);

            Vector3 camForward = baseCamRotation * Vector3.forward;
            Vector3 camRight = baseCamRotation * Vector3.right;

            direction = camRight * moveX + camForward * moveZ;
        }
        else
        {
            // First person fallback
            direction = transform.right * moveX + transform.forward * moveZ;
        }

        rb.linearVelocity = new Vector3(direction.normalized.x * moveSpeed, rb.linearVelocity.y, direction.normalized.z * moveSpeed);
    }
}