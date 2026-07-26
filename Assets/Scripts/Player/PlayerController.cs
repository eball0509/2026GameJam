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
    public float turnaroundBrakeForce = 14f;

    [Header("Air Control & Gravity")]
    public float gravityMultiplier = 2f;
    public float fallGravityMultiplier = 3.5f;
    [Range(0.05f, 1f)] public float airControlFactor = 0.4f;

    [Header("Roll Settings")]
    public float rollSpeedBoost = 35f;
    public float rollDuration = 0.55f;
    public float rollCooldown = 0.4f;
    public float rollColliderHeight = 1f;
    public float rollColliderDelay = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    [Header("Animation Reference")]
    public Animator anim;

    [Header("Ragdoll Settings")]
    [SerializeField] private Transform ragdollRoot;
    [SerializeField] private Transform ragdollCameraTarget;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isDead = false;

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

    private bool isSkiddingTurnaround = false;
    private float turnaroundTimer = 0f;
    private const float SKID_DURATION_LOCK = 0.3f; // Slightly shortened for punchier transitions

    private bool isWallJumpingMomentumActive = false;
    private bool wallJumpHadForwardMomentum = false;
    private float wallJumpMomentumTimer = 0f;
    private const float WALL_JUMP_MOMENTUM_DURATION = 0.45f;

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

        currentHealth = maxHealth;
        originalMaxRunSpeed = maxRunSpeed;
        originalAcceleration = acceleration;
        currentMaxSpeed = maxRunSpeed;

        if (col != null)
        {
            originalColHeight = col.height;
            originalColCenter = col.center;
        }

        if (ragdollRoot != null)
        {
            ragdollRigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
            ragdollColliders = ragdollRoot.GetComponentsInChildren<Collider>();
            SetRagdollState(false);
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        if (anim != null)
        {
            anim.SetBool("IsGrounded", isGrounded);
            Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            anim.SetFloat("Speed", horizVel.magnitude);

            if (movementModifiers != null)
            {
                anim.SetBool("IsWallSliding", movementModifiers.IsWallSliding);
                anim.SetBool("WallHang", movementModifiers.IsWallSliding && !movementModifiers.IsWallRunning);
                anim.SetBool("IsRightWall", movementModifiers.IsRightWall);
            }
        }

        if (Keyboard.current[OptionsManager.Jump].wasPressedThisFrame && !isRolling)
        {
            if (!isGrounded && movementModifiers != null && (movementModifiers.IsWallSliding || movementModifiers.IsWallRunning))
            {
                movementModifiers.PerformWallBounce();
                if (anim != null)
                {
                    anim.ResetTrigger("WallRunTrigger");
                    anim.SetTrigger("JumpTrigger");
                }
                isSkiddingTurnaround = false;
            }
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
                if (anim != null) anim.SetTrigger("JumpTrigger");
                isSkiddingTurnaround = false;
            }
        }

        if (Keyboard.current[OptionsManager.Roll].wasPressedThisFrame && isGrounded && !isRolling)
        {
            StartCoroutine(RollRoutine());
            if (anim != null) anim.SetTrigger("RollTrigger");
        }

        if (isSkiddingTurnaround)
        {
            turnaroundTimer -= Time.deltaTime;
            if (turnaroundTimer <= 0f) isSkiddingTurnaround = false;
            // Removed internal update return to allow real-time input gathering during skids
        }

        float targetX = 0f;
        float targetZ = 0f;

        bool strictlyWallSliding = movementModifiers != null && movementModifiers.IsWallSliding && !movementModifiers.IsWallRunning;

        if (!isRolling && !strictlyWallSliding && !isWallJumpingMomentumActive)
        {
            if (Keyboard.current[OptionsManager.MoveLeft].isPressed) targetX = -1f;
            if (Keyboard.current[OptionsManager.MoveRight].isPressed) targetX = 1f;
            if (Keyboard.current[OptionsManager.MoveBackward].isPressed) targetZ = -1f;
            if (Keyboard.current[OptionsManager.MoveForward].isPressed) targetZ = 1f;
        }

        if (!isWallJumpingMomentumActive)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (isGrounded && horizontalVelocity.magnitude > 8f && targetZ != 0f && !isSkiddingTurnaround)
            {
                if (camController != null && camController.IsThirdPerson)
                {
                    Quaternion baseCamRotation = Quaternion.Euler(0f, camController.GetCleanYRotation, 0f);
                    Vector3 targetMovingDir = (baseCamRotation * Vector3.right * targetX) + (baseCamRotation * Vector3.forward * targetZ);
                    if (Vector3.Dot(horizontalVelocity.normalized, targetMovingDir.normalized) < -0.5f)
                    {
                        TriggerTurnaroundSkid();
                    }
                }
                else
                {
                    Vector3 forwardProjection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                    float movementDirectionDot = Vector3.Dot(horizontalVelocity.normalized, forwardProjection);
                    if (movementDirectionDot > 0.5f && targetZ < 0f)
                    {
                        TriggerTurnaroundSkid();
                    }
                }
            }

            if (targetZ == 0f)
            {
                currentMoveZ = Mathf.MoveTowards(currentMoveZ, 0f, inputDeceleration * Time.deltaTime);
            }
            else
            {
                float activeRate = (targetZ < 0f && currentMoveZ > 0.05f) ? brakeSpeed : acceleration;
                currentMoveZ = Mathf.MoveTowards(currentMoveZ, targetZ, activeRate * Time.deltaTime);
            }

            if (targetX == 0f) currentMoveX = Mathf.MoveTowards(currentMoveX, 0f, inputDeceleration * Time.deltaTime);
            else currentMoveX = Mathf.MoveTowards(currentMoveX, targetX, acceleration * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (isWallJumpingMomentumActive)
        {
            wallJumpMomentumTimer -= Time.fixedDeltaTime;
            if (wallJumpMomentumTimer <= 0f) isWallJumpingMomentumActive = false;
        }

        bool isWallSliding = movementModifiers != null && movementModifiers.IsWallSliding;
        bool isWallRunning = movementModifiers != null && movementModifiers.IsWallRunning;
        bool strictlyWallSliding = isWallSliding && !isWallRunning;

        if (strictlyWallSliding)
        {
            Vector3 wallFaceDirection = -movementModifiers.WallNormal;
            if (wallFaceDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(wallFaceDirection, Vector3.up);
            }
            currentMoveX = 0f;
            currentMoveZ = 0f;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 direction;

        if (isWallRunning && movementModifiers != null)
        {
            direction = Vector3.ProjectOnPlane(transform.forward, movementModifiers.WallNormal);
            direction.y = 0f;
            direction.Normalize();
        }
        else if (isRolling || (isWallJumpingMomentumActive && wallJumpHadForwardMomentum))
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

        if (direction.magnitude > 0.1f && !isWallJumpingMomentumActive)
        {
            if (direction.magnitude > 1f) direction.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            float turnSpeed = isWallRunning ? 30f : 15f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
        }

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (isWallRunning) return;

        if (isWallJumpingMomentumActive)
        {
            if (!isGrounded)
            {
                rb.AddForce(Physics.gravity * ((rb.linearVelocity.y < 0 ? fallGravityMultiplier : gravityMultiplier) - 1f), ForceMode.Acceleration);
            }
            return;
        }

        // --- MOMENTUM PRESERVATION MECHANIC ---
        // Calculate velocity update using blended inputs rather than stopping the player outright
        float activeMoveSpeed = Mathf.Max(moveSpeed, currentHorizontalVelocity.magnitude);
        Vector3 desiredHorizontalVelocity = direction * activeMoveSpeed;

        bool isMovingInputActive = isRolling || isWallRunning || (currentMaxSpeed > originalMaxRunSpeed) || (
                                   Keyboard.current[OptionsManager.MoveForward].isPressed ||
                                   Keyboard.current[OptionsManager.MoveBackward].isPressed ||
                                   Keyboard.current[OptionsManager.MoveLeft].isPressed ||
                                   Keyboard.current[OptionsManager.MoveRight].isPressed);

        bool isBraking = !isRolling && !isWallRunning && Keyboard.current[OptionsManager.MoveBackward].isPressed && currentMoveZ > 0.05f;

        // Apply dynamic turnaround brake modifications directly to the blend calculation rather than returning early
        float blendRate = isBraking ? brakeSpeed : (isMovingInputActive ? acceleration : groundFriction);
        if (isSkiddingTurnaround) blendRate = turnaroundBrakeForce;

        if (!isGrounded && !isWallRunning) blendRate *= airControlFactor;

        Vector3 finalHorizontalVelocity;

        if (currentMaxSpeed > originalMaxRunSpeed && isMovingInputActive && !isRolling)
        {
            float preservedSpeed = Mathf.Max(currentHorizontalVelocity.magnitude, moveSpeed);
            Vector3 blendedDirection = Vector3.RotateTowards(currentHorizontalVelocity.normalized, direction, blendRate * Time.fixedDeltaTime, 0f);
            finalHorizontalVelocity = blendedDirection * preservedSpeed;
        }
        else
        {
            finalHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredHorizontalVelocity, blendRate * moveSpeed * Time.fixedDeltaTime);
        }

        if (finalHorizontalVelocity.magnitude > currentMaxSpeed)
        {
            if (Vector3.Dot(desiredHorizontalVelocity.normalized, finalHorizontalVelocity.normalized) > 0)
                finalHorizontalVelocity = currentHorizontalVelocity;
        }
        else
        {
            finalHorizontalVelocity = Vector3.ClampMagnitude(finalHorizontalVelocity, currentMaxSpeed);
        }

        if (!isGrounded)
        {
            rb.AddForce(Physics.gravity * ((rb.linearVelocity.y < 0 ? fallGravityMultiplier : gravityMultiplier) - 1f), ForceMode.Acceleration);
        }

        rb.linearVelocity = new Vector3(finalHorizontalVelocity.x, rb.linearVelocity.y, finalHorizontalVelocity.z);
    }

    public void InjectWallJumpInput(Vector3 jumpDirection, bool hadForwardMomentum)
    {
        isSkiddingTurnaround = false;
        isWallJumpingMomentumActive = true;
        wallJumpHadForwardMomentum = hadForwardMomentum;
        wallJumpMomentumTimer = WALL_JUMP_MOMENTUM_DURATION;
        currentMoveX = 0f;
        currentMoveZ = 0f;
    }

    private void TriggerTurnaroundSkid()
    {
        isSkiddingTurnaround = true;
        turnaroundTimer = SKID_DURATION_LOCK;
        // REMOVED: currentMoveX = 0f; currentMoveZ = 0f; 
        // Keeping inputs alive allows momentum vectors to shift smoothly into the new direction.
        if (anim != null) anim.SetTrigger("TurnAroundTrigger");
    }

    private System.Collections.IEnumerator RollRoutine()
    {
        isRolling = true;
        yield return new WaitForSeconds(rollColliderDelay);

        if (col != null)
        {
            col.height = rollColliderHeight;
            col.center = new Vector3(originalColCenter.x, originalColCenter.y - (originalColHeight - rollColliderHeight) / 2f, originalColCenter.z);
        }

        float entrySpeed = GetCurrentHorizontalSpeed();
        float targetBoost = Mathf.Max(entrySpeed, rollSpeedBoost);
        ApplySpeedOverboost(targetBoost, acceleration * 2f, rollDuration, 0.2f);

        yield return new WaitForSeconds(rollDuration - rollColliderDelay);

        if (col != null)
        {
            Vector3 rayStart = transform.position + (Vector3.up * (rollColliderHeight / 2f));
            float checkDistance = originalColHeight - (rollColliderHeight / 2f) + 0.15f;
            while (Physics.Raycast(rayStart, Vector3.up, checkDistance, groundMask)) yield return null;

            col.height = originalColHeight;
            col.center = originalColCenter;
        }

        yield return new WaitForSeconds(rollCooldown);
        isRolling = false;
    }

    public void TriggerVictoryDance() { if (anim != null) anim.SetTrigger("VictoryTrigger"); }

    public void ApplySpeedOverboost(float boostedMaxSpeed, float boostedAccel, float decayDuration, float holdDuration = 0.7f)
    {
        if (boostCoroutine != null) StopCoroutine(boostCoroutine);
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

    private void SetRagdollState(bool active)
    {
        foreach (var boneRb in ragdollRigidbodies) boneRb.isKinematic = !active;
        foreach (var boneCol in ragdollColliders) boneCol.enabled = active;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Vector3 deathVelocity = rb != null ? rb.linearVelocity : Vector3.zero;

        if (anim != null) anim.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null) rb.isKinematic = true;
        if (movementModifiers != null) movementModifiers.enabled = false;

        SetRagdollState(true);

        if (ragdollRigidbodies != null)
        {
            foreach (var boneRb in ragdollRigidbodies)
            {
                boneRb.linearVelocity = deathVelocity;
                boneRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }

        if (camController != null && ragdollCameraTarget != null)
        {
            camController.SwitchToRagdollTarget(ragdollCameraTarget);
        }
    }

    public float GetCurrentHorizontalSpeed() => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
    public void TakeDamage(int damageAmount) { currentHealth = Mathf.Max(0, currentHealth - damageAmount); if (currentHealth <= 0) Die(); }
    private void OnDrawGizmosSelected() { if (groundCheck != null) { Gizmos.color = isGrounded ? Color.green : Color.red; Gizmos.DrawWireSphere(groundCheck.position, groundDistance); } }
}