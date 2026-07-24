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
    public float moveSpeed = 15f;
    public float jumpForce = 7f;
    public float maxRunSpeed = 30f;

    [Header("Momentum & Physics")]
    public float acceleration = 2f;
    public float inputDeceleration = 8f;
    public float groundFriction = 0.5f;
    public float brakeSpeed = 6f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    private Rigidbody rb;
    private bool isGrounded;

    private float currentMoveX = 0f;
    private float currentMoveZ = 0f;

    private float originalMaxRunSpeed;
    private float originalAcceleration;
    private float currentMaxSpeed;
    private Coroutine boostCoroutine;

    private PlayerCameraController camController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        camController = GetComponentInChildren<PlayerCameraController>();
        if (camController == null)
        {
            camController = Camera.main.GetComponent<PlayerCameraController>();
        }

        // Cache original base limits
        originalMaxRunSpeed = maxRunSpeed;
        originalAcceleration = acceleration;
        currentMaxSpeed = maxRunSpeed;
    }

    private void Update()
    {

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (Keyboard.current[jumpKey].wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        // --- MOMENTUM CALCULATIONS ---
        float targetX = 0f;
        float targetZ = 0f;

        if (Keyboard.current[moveLeftKey].isPressed) targetX = -1f;
        if (Keyboard.current[moveRightKey].isPressed) targetX = 1f;
        if (Keyboard.current[moveBackwardKey].isPressed) targetZ = -1f;
        if (Keyboard.current[moveForwardKey].isPressed) targetZ = 1f;

        // Process Z-Axis
        if (targetZ == 0f)
        {
            currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, inputDeceleration * Time.deltaTime);
        }
        else if (targetZ < 0f && currentMoveZ > 0.05f)
        {
            currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, brakeSpeed * Time.deltaTime);
        }
        else
        {
            currentMoveZ = Mathf.MoveTowards(currentMoveZ, targetZ, acceleration * Time.deltaTime);
        }

        // Process X-Axis
        if (targetX == 0f)
        {
            currentMoveX = Mathf.MoveTowards(currentMoveX, 0f, inputDeceleration * Time.deltaTime);
        }
        else
        {
            currentMoveX = Mathf.MoveTowards(currentMoveX, targetX, acceleration * Time.deltaTime);
        }

    }

    private void FixedUpdate()
    {
        Vector3 direction;

        if (camController != null && camController.IsThirdPerson)
        {
            Quaternion baseCamRotation = Quaternion.Euler(0f, camController.GetCleanYRotation, 0f);
            Vector3 camForward = baseCamRotation * Vector3.forward;
            Vector3 camRight = baseCamRotation * Vector3.right;

            direction = (camRight * currentMoveX) + (camForward * currentMoveZ);
        }
        else
        {
            direction = (transform.right * currentMoveX) + (transform.forward * currentMoveZ);
        }

        if (direction.magnitude > 1f)
        {
            direction.Normalize();
        }

        Vector3 desiredHorizontalVelocity = new Vector3(direction.x * moveSpeed, 0f, direction.z * moveSpeed);

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        bool isMovingInputActive = (Keyboard.current[moveForwardKey].isPressed ||
                                    Keyboard.current[moveBackwardKey].isPressed ||
                                    Keyboard.current[moveLeftKey].isPressed ||
                                    Keyboard.current[moveRightKey].isPressed);

        bool isBraking = Keyboard.current[moveBackwardKey].isPressed && currentMoveZ > 0.05f;

        float blendRate;
        if (isBraking)
        {
            blendRate = brakeSpeed;
        }
        else if (isMovingInputActive)
        {
            blendRate = acceleration;
        }
        else
        {
            blendRate = groundFriction;
        }

        Vector3 finalHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredHorizontalVelocity, blendRate * moveSpeed * Time.fixedDeltaTime);

        if (finalHorizontalVelocity.magnitude > currentMaxSpeed)
        {
            float dot = Vector3.Dot(desiredHorizontalVelocity.normalized, finalHorizontalVelocity.normalized);
            if (dot > 0)
            {
                finalHorizontalVelocity = currentHorizontalVelocity;
            }
        }
        else
        {
            finalHorizontalVelocity = Vector3.ClampMagnitude(finalHorizontalVelocity, currentMaxSpeed);
        }

        rb.linearVelocity = new Vector3(finalHorizontalVelocity.x, rb.linearVelocity.y, finalHorizontalVelocity.z);

    }

    public void ApplySpeedOverboost(float boostedMaxSpeed, float boostedAccel, float decayDuration)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
        }

        boostCoroutine = StartCoroutine(DecayBoostRoutine(boostedMaxSpeed, boostedAccel, decayDuration));
    }

    private System.Collections.IEnumerator DecayBoostRoutine(float startMaxSpeed, float startAccel, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            currentMaxSpeed = Mathf.Lerp(startMaxSpeed, originalMaxRunSpeed, t);
            acceleration = Mathf.Lerp(startAccel, originalAcceleration, t);

            yield return null;
        }

        currentMaxSpeed = originalMaxRunSpeed;
        acceleration = originalAcceleration;
        boostCoroutine = null;
    }

    public float GetCurrentHorizontalSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        return horizontalVelocity.magnitude;
    }

}