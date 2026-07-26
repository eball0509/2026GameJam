using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementModifiers : MonoBehaviour
{
    [Header("Wall Slide Settings")]
    public float maxWallSlideSpeed = 20f;
    public float wallCheckDistance = 1.2f;
    public LayerMask wallMask;

    [Header("Wall Climb Momentum")]
    [Tooltip("How much of your horizontal entry speed is transferred into upward vertical speed.")]
    public float speedToClimbMultiplier = 0.8f;
    private bool justHitWall = false;

    [Header("Wall Bounce Settings")]
    public float wallBounceUpForce = 12f;
    [Tooltip("How hard you are pushed directly AWAY from the wall (90 degrees). Lower this if you are flying too far back.")]
    public float wallBounceAwayForce = 5f;
    [Tooltip("How much baseline forward speed along the wall you get if jumping from a standstill.")]
    public float wallBounceForwardForce = 14f;
    [Range(0f, 1f)]
    [Tooltip("0 = Purely push away from wall. 1 = Heavily favor forward momentum down the corridor.")]
    public float tangentBias = 0.75f;

    [Header("Wall Run Settings")]
    [Range(0f, 90f)] public float wallRunAngleThreshold = 45f;
    [Tooltip("Force keeping you glued to the wall while running.")]
    public float wallStickForce = 25f;
    [Tooltip("0 = Floating/Zero gravity during wall run. 1 = Full normal gravity. Try 0.1 to 0.25 for a slow, smooth fall.")]
    [Range(0f, 1f)] public float wallRunGravityMultiplier = 0.15f;

    public bool IsWallSliding => isWallSliding;
    public bool IsWallRunning => isWallRunning;
    public Vector3 WallNormal => wallNormal;
    public bool IsRightWall => isRightWall;

    private Rigidbody rb;
    private PlayerController playerController;
    private CapsuleCollider capsuleCollider;

    private bool isWallSliding;
    private bool isWallRunning;
    private bool isRightWall;
    private Vector3 wallNormal;
    private float wallInteractionCooldown = 0f;

    private Vector3 preCalculatedForwardMomentum = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        if ((isWallSliding || isWallRunning) && Keyboard.current[OptionsManager.MoveBackward].wasPressedThisFrame)
        {
            ExitWallInteraction();
        }
    }

    void FixedUpdate()
    {
        if (wallInteractionCooldown > 0f)
        {
            wallInteractionCooldown -= Time.fixedDeltaTime;
            isWallSliding = false;
            isWallRunning = false;
            return;
        }

        CheckForWall();

        if (isWallSliding && !isWallRunning)
        {
            Vector3 wallFaceDirection = -wallNormal;
            if (wallFaceDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(wallFaceDirection, Vector3.up);
            }

            if (rb.linearVelocity.y > 0) return;

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (rb.linearVelocity.y > -1.5f) rb.linearVelocity = new Vector3(0f, -1.5f, 0f);
            if (rb.linearVelocity.y < -maxWallSlideSpeed) rb.linearVelocity = new Vector3(0f, -maxWallSlideSpeed, 0f);
        }

        if (isWallRunning)
        {
            HandleWallSticking();

            // --- NEW: COUNTER GRAVITY LOOP FOR SLOWER FALLS ---
            // Compensate for global gravity so the player doesn't plunge immediately
            float counterGravity = Physics.gravity.y * (1f - wallRunGravityMultiplier);
            rb.AddForce(new Vector3(0f, -counterGravity, 0f), ForceMode.Acceleration);
        }
    }

    private void CheckForWall()
    {
        if (playerController != null && playerController.IsGrounded)
        {
            isWallSliding = false;
            isWallRunning = false;
            justHitWall = false;
            return;
        }

        Vector3[] directions = { transform.forward, transform.right, -transform.right, -transform.forward };
        bool wallHit = false;

        float raycastLength = wallCheckDistance;
        if (capsuleCollider != null)
        {
            raycastLength = capsuleCollider.radius + 0.25f;
        }

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, raycastLength, wallMask))
            {
                wallHit = true;
                wallNormal = hit.normal;

                Vector3 cross = Vector3.Cross(transform.forward, -wallNormal);
                isRightWall = cross.y > 0;

                break;
            }
        }

        if (wallHit)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float entrySpeed = horizontalVelocity.magnitude;

            if (entrySpeed > 1f)
            {
                preCalculatedForwardMomentum = horizontalVelocity;
            }

            Vector3 travelDirection = entrySpeed > 0.1f ? horizontalVelocity.normalized : transform.forward;
            float angle = Vector3.Angle(travelDirection, -wallNormal);
            bool qualifiesForWallRun = entrySpeed > 2f && angle > wallRunAngleThreshold;

            if (qualifiesForWallRun)
            {
                isWallRunning = true;
                isWallSliding = false;

                if (!justHitWall)
                {
                    justHitWall = true;
                    if (playerController != null && playerController.anim != null)
                    {
                        playerController.anim.SetTrigger("WallRunTrigger");
                    }
                }
            }
            else
            {
                isWallRunning = false;
                isWallSliding = true;

                if (!justHitWall)
                {
                    justHitWall = true;
                    if (entrySpeed > 2f)
                    {
                        float upwardClimbForce = entrySpeed * speedToClimbMultiplier;
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, upwardClimbForce), rb.linearVelocity.z);
                    }
                }
            }
        }
        else
        {
            isWallSliding = false;
            isWallRunning = false;
            justHitWall = false;
        }
    }

    private void HandleWallSticking()
    {
        bool pressingForward = Keyboard.current[OptionsManager.MoveForward].isPressed;
        bool pressingTowardsWall = false;

        if (isRightWall)
        {
            pressingTowardsWall = Keyboard.current[OptionsManager.MoveRight].isPressed;
        }
        else
        {
            pressingTowardsWall = Keyboard.current[OptionsManager.MoveLeft].isPressed;
        }

        if (!pressingForward || !pressingTowardsWall)
        {
            isWallRunning = false;
            isWallSliding = false;
            justHitWall = false;
            wallInteractionCooldown = 0.35f;
            return;
        }

        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Acceleration);
    }

    private void ExitWallInteraction()
    {
        isWallSliding = false;
        isWallRunning = false;
        justHitWall = false;
        wallInteractionCooldown = 0.4f;
        rb.linearVelocity = new Vector3(wallNormal.x * 4f, rb.linearVelocity.y, wallNormal.z * 4f);
    }

    public void PerformWallBounce()
    {
        bool launchedFromWallRun = isWallRunning;
        Vector3 wallForwardTangent = Vector3.ProjectOnPlane(transform.forward, wallNormal).normalized;
        Vector3 parallelMomentum;

        if (!launchedFromWallRun)
        {
            parallelMomentum = Vector3.zero;
        }
        else if (preCalculatedForwardMomentum.magnitude > 2f)
        {
            parallelMomentum = Vector3.ProjectOnPlane(preCalculatedForwardMomentum, wallNormal);
        }
        else
        {
            parallelMomentum = wallForwardTangent * wallBounceForwardForce;
        }

        Vector3 outwardPush = wallNormal * wallBounceAwayForce;
        Vector3 finalHorizontalVelocity;

        if (!launchedFromWallRun)
        {
            finalHorizontalVelocity = outwardPush;
        }
        else
        {
            Vector3 combinedHorizontal = Vector3.Lerp(outwardPush, parallelMomentum.normalized * parallelMomentum.magnitude, tangentBias);
            float targetSpeed = Mathf.Max(parallelMomentum.magnitude, wallBounceForwardForce);
            finalHorizontalVelocity = combinedHorizontal.normalized * targetSpeed;
        }

        wallInteractionCooldown = 0.45f;
        isWallSliding = false;
        isWallRunning = false;
        justHitWall = false;

        rb.linearVelocity = new Vector3(finalHorizontalVelocity.x, wallBounceUpForce, finalHorizontalVelocity.z);

        if (finalHorizontalVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(finalHorizontalVelocity.normalized, Vector3.up);
        }

        if (playerController != null)
        {
            playerController.InjectWallJumpInput(finalHorizontalVelocity, launchedFromWallRun);
            playerController.ApplySpeedOverboost(
                boostedMaxSpeed: Mathf.Max(finalHorizontalVelocity.magnitude, playerController.maxRunSpeed),
                boostedAccel: playerController.acceleration * 2.5f,
                decayDuration: 1.2f,
                holdDuration: 0.35f
            );
        }

        preCalculatedForwardMomentum = Vector3.zero;
    }
}