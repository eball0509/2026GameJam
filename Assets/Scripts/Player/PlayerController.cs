using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
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

    [Header("Roll Settings")]
    public float rollSpeedBoost = 35f;
    public float rollDuration = 0.55f;
    public float rollCooldown = 0.4f;
    public float rollColliderHeight = 1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    public bool IsGrounded => isGrounded;
    public bool IsRolling => isRolling;

    private Rigidbody rb;
    private CapsuleCollider col;
    private bool isGrounded;
    private bool isRolling;

    private float currentMoveX = 0f;
    private float currentMoveZ = 0f;
    private float originalMaxRunSpeed;
    private float originalAcceleration;
    private float originalColHeight;
    private Vector3 originalColCenter;

    public float currentMaxSpeed { get; private set; }

    private Coroutine boostCoroutine;
    private PlayerCameraController camController;
    private PlayerMovementModifiers movementModifiers;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        movementModifiers = GetComponent<PlayerMovementModifiers>();
        camController = GetComponentInChildren<PlayerCameraController>();

        if (camController == null && Camera.main != null)
        {
            camController = Camera.main.GetComponent<PlayerCameraController>();
        }

        // Store original states
        currentHealth = maxHealth;
        originalMaxRunSpeed = maxRunSpeed;
        originalAcceleration = acceleration;
        currentMaxSpeed = maxRunSpeed;

        if (col != null)
        {
            originalColHeight = col.height;
            originalColCenter = col.center;
        }
    }

    private void Update()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // Prevent jumping while rolling
        if (Keyboard.current[OptionsManager.Jump].wasPressedThisFrame && !isRolling)
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

        // --- ROLL INPUT CHECK ---
        // Replace OptionsManager.Roll with your actual roll keybind reference
        if (Keyboard.current[OptionsManager.Roll].wasPressedThisFrame && isGrounded && !isRolling)
        {
            StartCoroutine(RollRoutine());
        }

        float targetX = 0f;
        float targetZ = 0f;

        // Only accept WASD movement target inputs if we are NOT rolling
        if (!isRolling)
        {
            if (Keyboard.current[OptionsManager.MoveLeft].isPressed) targetX = -1f;
            if (Keyboard.current[OptionsManager.MoveRight].isPressed) targetX = 1f;
            if (Keyboard.current[OptionsManager.MoveBackward].isPressed) targetZ = -1f;
            if (Keyboard.current[OptionsManager.MoveForward].isPressed) targetZ = 1f;
        }

        if (targetZ == 0f) currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, inputDeceleration * Time.deltaTime);
        else if (targetZ < 0f && currentMoveZ > 0.05f) currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, brakeSpeed * Time.deltaTime);
        else currentMoveZ = Mathf.MoveTowards(currentMoveZ, targetZ, acceleration * Time.deltaTime);

        if (targetX == 0f) currentMoveX = Mathf.MoveTowards(currentMoveX, 0f, inputDeceleration * Time.deltaTime);
        else currentMoveX = Mathf.MoveTowards(currentMoveX, targetX, acceleration * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Vector3 direction;

        // 1. Force the direction straight forward relative to the character if rolling
        if (isRolling)
        {
            direction = transform.forward;
        }
        else if (camController != null && camController.IsThirdPerson)
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

        // 2. Treat the player as "actively moving" during a roll so friction doesn't stop them
        bool isMovingInputActive = isRolling || (
                                   Keyboard.current[OptionsManager.MoveForward].isPressed ||
                                   Keyboard.current[OptionsManager.MoveBackward].isPressed ||
                                   Keyboard.current[OptionsManager.MoveLeft].isPressed ||
                                   Keyboard.current[OptionsManager.MoveRight].isPressed);

        bool isBraking = !isRolling && Keyboard.current[OptionsManager.MoveBackward].isPressed && currentMoveZ > 0.05f;

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

    // --- NEW ROLL ROUTINE ---
    private System.Collections.IEnumerator RollRoutine()
    {
        isRolling = true;

        // 1. Shrink hitbox
        if (col != null)
        {
            col.height = rollColliderHeight;
            // Lower the center to keep the bottom of the capsule flush with the ground
            col.center = new Vector3(originalColCenter.x, originalColCenter.y - (originalColHeight - rollColliderHeight) / 2f, originalColCenter.z);
        }

        // 2. Apply Speed Boost (Re-using your existing smooth decay logic)
        ApplySpeedOverboost(rollSpeedBoost, acceleration * 2f, rollDuration, 0.1f);

        // 3. Wait for the roll animation/duration to complete
        yield return new WaitForSeconds(rollDuration);

        // 4. Overhead check: Prevent standing up while stuck under an obstacle
        if (col != null)
        {
            Vector3 rayStart = transform.position + (Vector3.up * (rollColliderHeight / 2f));
            float checkDistance = originalColHeight - (rollColliderHeight / 2f) + 0.15f;

            // Loop until the space above the player is clear of 'groundMask'
            while (Physics.Raycast(rayStart, Vector3.up, checkDistance, groundMask))
            {
                yield return null;
            }

            // Restore hitbox
            col.height = originalColHeight;
            col.center = originalColCenter;
        }

        // 5. Apply Cooldown
        yield return new WaitForSeconds(rollCooldown);
        isRolling = false;
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