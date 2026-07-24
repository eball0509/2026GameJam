using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1;
    public int currentHealth;

    [Header("Movement Settings")]
    public float moveSpeed = 15f;
    public float jumpForce = 12f;
    public float maxRunSpeed = 30f;

    [Header("Momentum & Physics")]
    public float acceleration = 2f;
    public float inputDeceleration = 8f;
    public float groundFriction = 0.5f;
    public float brakeSpeed = 6f;

    [Header("Air Control & Gravity")]
    public float gravityMultiplier = 2f;
    public float fallGravityMultiplier = 3.5f;
    [Range(0.05f, 1f)] public float airControlFactor = 0.4f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    public bool IsGrounded => isGrounded;

    private Rigidbody rb;
    private bool isGrounded;
    private float currentMoveX = 0f;
    private float currentMoveZ = 0f;
    private float originalMaxRunSpeed;
    private float originalAcceleration;
    public float currentMaxSpeed { get; private set; }
    private Coroutine boostCoroutine;
    private PlayerCameraController camController;
    private PlayerMovementModifiers movementModifiers;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        movementModifiers = GetComponent<PlayerMovementModifiers>();
        camController = GetComponentInChildren<PlayerCameraController>();
        if (camController == null && Camera.main != null)
        {
            camController = Camera.main.GetComponent<PlayerCameraController>();
        }

        currentHealth = maxHealth;
        originalMaxRunSpeed = maxRunSpeed;
        originalAcceleration = acceleration;
        currentMaxSpeed = maxRunSpeed;
    }

    private void Update()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // LOOKUP GLOBAL BIND
        if (Keyboard.current[OptionsManager.Jump].wasPressedThisFrame)
        {
            if (!isGrounded && movementModifiers != null && movementModifiers.IsWallSliding)
            {
                movementModifiers.PerformWallBounce();
            }
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            }
        }

        float targetX = 0f;
        float targetZ = 0f;

        // LOOKUP GLOBAL BINDS
        if (Keyboard.current[OptionsManager.MoveLeft].isPressed) targetX = -1f;
        if (Keyboard.current[OptionsManager.MoveRight].isPressed) targetX = 1f;
        if (Keyboard.current[OptionsManager.MoveBackward].isPressed) targetZ = -1f;
        if (Keyboard.current[OptionsManager.MoveForward].isPressed) targetZ = 1f;

        if (targetZ == 0f) currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, inputDeceleration * Time.deltaTime);
        else if (targetZ < 0f && currentMoveZ > 0.05f) currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, brakeSpeed * Time.deltaTime);
        else currentMoveZ = Mathf.MoveTowards(currentMoveZ, targetZ, acceleration * Time.deltaTime);

        if (targetX == 0f) currentMoveX = Mathf.MoveTowards(currentMoveX, 0f, inputDeceleration * Time.deltaTime);
        else currentMoveX = Mathf.MoveTowards(currentMoveX, targetX, acceleration * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Vector3 direction;
        if (camController != null && camController.IsThirdPerson)
        {
            Quaternion baseCamRotation = Quaternion.Euler(0f, camController.GetCleanYRotation, 0f);
            direction = (baseCamRotation * Vector3.right * currentMoveX) + (baseCamRotation * Vector3.forward * currentMoveZ);
        }
        else
        {
            direction = (transform.right * currentMoveX) + (transform.forward * currentMoveZ);
        }

        if (direction.magnitude > 1f) direction.Normalize();

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float activeMoveSpeed = Mathf.Max(moveSpeed, currentHorizontalVelocity.magnitude);
        Vector3 desiredHorizontalVelocity = direction * activeMoveSpeed;

        // LOOKUP GLOBAL BINDS
        bool isMovingInputActive = (Keyboard.current[OptionsManager.MoveForward].isPressed ||
                                    Keyboard.current[OptionsManager.MoveBackward].isPressed ||
                                    Keyboard.current[OptionsManager.MoveLeft].isPressed ||
                                    Keyboard.current[OptionsManager.MoveRight].isPressed);

        bool isBraking = Keyboard.current[OptionsManager.MoveBackward].isPressed && currentMoveZ > 0.05f;

        float blendRate = isBraking ? brakeSpeed : (isMovingInputActive ? acceleration : groundFriction);
        if (!isGrounded) blendRate *= airControlFactor;

        Vector3 finalHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredHorizontalVelocity, blendRate * moveSpeed * Time.fixedDeltaTime);

        if (finalHorizontalVelocity.magnitude > currentMaxSpeed)
        {
            if (Vector3.Dot(desiredHorizontalVelocity.normalized, finalHorizontalVelocity.normalized) > 0)
                finalHorizontalVelocity = currentHorizontalVelocity;
        }
        else
        {
            finalHorizontalVelocity = Vector3.ClampMagnitude(finalHorizontalVelocity, currentMaxSpeed);
        }

        Vector3 targetHorizontalVelocity = finalHorizontalVelocity;
        if (movementModifiers != null && movementModifiers.IsWallSliding)
        {
            Vector3 wallNormal = movementModifiers.WallNormal;
            float inwardSpeed = Vector3.Dot(targetHorizontalVelocity, -wallNormal);
            if (inwardSpeed > 0f) targetHorizontalVelocity += wallNormal * inwardSpeed;
            targetHorizontalVelocity += -wallNormal * 0.2f;
        }

        if (!isGrounded && !(movementModifiers != null && movementModifiers.IsWallSliding))
        {
            rb.AddForce(Physics.gravity * ((rb.linearVelocity.y < 0 ? fallGravityMultiplier : gravityMultiplier) - 1f), ForceMode.Acceleration);
        }

        rb.linearVelocity = new Vector3(targetHorizontalVelocity.x, rb.linearVelocity.y, targetHorizontalVelocity.z);
    }

    public void ApplySpeedOverboost(float boostedMaxSpeed, float boostedAccel, float decayDuration, float holdDuration = 0.5f)
    {
        if (boostCoroutine != null) StopCoroutine(boostCoroutine);
        boostCoroutine = StartCoroutine(DecayBoostRoutine(boostedMaxSpeed, boostedAccel, decayDuration, holdDuration));
    }

    private System.Collections.IEnumerator DecayBoostRoutine(float startMaxSpeed, float startAccel, float decayDuration, float holdDuration)
    {
        currentMaxSpeed = startMaxSpeed; acceleration = startAccel;
        yield return new WaitForSeconds(holdDuration);
        float elapsed = 0f;
        while (elapsed < decayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / decayDuration;
            currentMaxSpeed = Mathf.Lerp(startMaxSpeed, originalMaxRunSpeed, t);
            acceleration = Mathf.Lerp(startAccel, originalAcceleration, t);
            yield return null;
        }
        currentMaxSpeed = originalMaxRunSpeed; acceleration = originalAcceleration; boostCoroutine = null;
    }

    public float GetCurrentHorizontalSpeed() => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
    public void TakeDamage(int damageAmount) { currentHealth = Mathf.Max(0, currentHealth - damageAmount); if (currentHealth <= 0) Die(); }
    public void Die() { Debug.Log("You Died"); }
    private void OnDrawGizmosSelected() { if (groundCheck != null) { Gizmos.color = isGrounded ? Color.green : Color.red; Gizmos.DrawWireSphere(groundCheck.position, groundDistance); } }
}