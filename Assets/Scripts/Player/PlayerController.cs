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

    [Header("Health")]
    public int maxHealth = 1;
    public int currentHealth;

    [Header("Movement Settings")]
    public float moveSpeed = 15f;
    public float jumpForce = 12f; // Boosted slightly to match snappier gravity
    public float maxRunSpeed = 30f;

    [Header("Momentum & Physics")]
    public float acceleration = 2f;
    public float inputDeceleration = 8f;
    public float groundFriction = 0.5f;
    public float brakeSpeed = 6f;

    [Header("Air Control & Gravity (Snappiness)")]
    [Tooltip("Extra gravity applied while rising up in a jump.")]
    public float gravityMultiplier = 2f;
    [Tooltip("Extra gravity applied while falling down. Higher = snappy fast drops.")]
    public float fallGravityMultiplier = 3.5f;
    [Tooltip("How much directional control the player has in mid-air (0 = full momentum lock, 1 = same as ground).")]
    [Range(0.05f, 1f)]
    public float airControlFactor = 0.4f;

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
        // Ground Check
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // Single Master Jump Handler
        if (Keyboard.current[jumpKey].wasPressedThisFrame)
        {
            // 1. Priority: Wall Bounce if touching wall mid-air
            if (!isGrounded && movementModifiers != null && movementModifiers.IsWallSliding)
            {
                movementModifiers.PerformWallBounce();
            }
            // 2. Priority: Normal Ground Jump
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            }
        }

        // --- MOMENTUM CALCULATIONS ---
        float targetX = 0f;
        float targetZ = 0f;

        if (Keyboard.current[moveLeftKey].isPressed) targetX = -1f;
        if (Keyboard.current[moveRightKey].isPressed) targetX = 1f;
        if (Keyboard.current[moveBackwardKey].isPressed) targetZ = -1f;
        if (Keyboard.current[moveForwardKey].isPressed) targetZ = 1f;

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

        // Calculate movement direction relative to camera or player transform
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

        // Isolate the current actual physical horizontal speed
        //Vector3 desiredHorizontalVelocity = new Vector3(direction.x * moveSpeed, 0f, direction.z * moveSpeed);
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeedMagnitude = currentHorizontalVelocity.magnitude;

        // FIX: If we are actively overboosting, our target movement speed should adapt 
        // to our current high velocity so holding W doesn't drag us back down to 15!
        float activeMoveSpeed = Mathf.Max(moveSpeed, currentSpeedMagnitude);
        Vector3 desiredHorizontalVelocity = new Vector3(direction.x * activeMoveSpeed, 0f, direction.z * activeMoveSpeed);

        bool isMovingInputActive = (Keyboard.current[moveForwardKey].isPressed ||
                                    Keyboard.current[moveBackwardKey].isPressed ||
                                    Keyboard.current[moveLeftKey].isPressed ||
                                    Keyboard.current[moveRightKey].isPressed);

        bool isBraking = Keyboard.current[moveBackwardKey].isPressed && currentMoveZ > 0.05f;

        float blendRate;
        if (isBraking) blendRate = brakeSpeed;
        else if (isMovingInputActive) blendRate = acceleration;
        else blendRate = groundFriction;

        // REDUCE AIR CONTROL: If in air, dampen input reactivity so jumps preserve launch direction
        if (!isGrounded)
        {
            blendRate *= airControlFactor;
        }

        Vector3 finalHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredHorizontalVelocity, blendRate * moveSpeed * Time.fixedDeltaTime);

        // Speed clamping
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

        // --- INSTANT WALL FRICTION OVERRIDE ---
        Vector3 targetHorizontalVelocity = finalHorizontalVelocity;

        if (movementModifiers != null && movementModifiers.IsWallSliding)
        {
            Vector3 wallNormal = movementModifiers.WallNormal;

            // Check if our current velocity is pushing into the wall face
            float inwardSpeed = Vector3.Dot(targetHorizontalVelocity, -wallNormal);
            if (inwardSpeed > 0f)
            {
                targetHorizontalVelocity += wallNormal * inwardSpeed;
            }

            targetHorizontalVelocity += -wallNormal * 0.2f;
        }

        // --- CUSTOM GRAVITY MODIFIER (FIXES FLOATINESS) ---
        // Don't apply extra downward force if wall sliding
        bool isWallSliding = movementModifiers != null && movementModifiers.IsWallSliding;

        if (!isGrounded && !isWallSliding)
        {
            if (rb.linearVelocity.y < 0)
            {
                // Falling: Apply heavy downward force to snap back to ground quickly
                rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
            }
            else if (rb.linearVelocity.y > 0)
            {
                // Rising: Apply moderate extra gravity for a punchy arc
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        // Apply clean horizontal velocity, preserving gravity Y acceleration
        rb.linearVelocity = new Vector3(targetHorizontalVelocity.x, rb.linearVelocity.y, targetHorizontalVelocity.z);
    }

    public void ApplySpeedOverboost(float boostedMaxSpeed, float boostedAccel, float decayDuration, float holdDuration = 0.5f)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
        }

        boostCoroutine = StartCoroutine(DecayBoostRoutine(boostedMaxSpeed, boostedAccel, decayDuration, holdDuration));
    }

    private System.Collections.IEnumerator DecayBoostRoutine(float startMaxSpeed, float startAccel, float decayDuration, float holdDuration)
    {
        currentMaxSpeed = startMaxSpeed;
        acceleration = startAccel;

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

        currentMaxSpeed = originalMaxRunSpeed;
        acceleration = originalAcceleration;
        boostCoroutine = null;
    }

    public float GetCurrentHorizontalSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        return horizontalVelocity.magnitude;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        if (currentHealth <= 0) Die();
    }

    public void Die() { Debug.Log("You Died"); }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}