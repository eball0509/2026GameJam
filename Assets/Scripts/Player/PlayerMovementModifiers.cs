using UnityEngine;

public class PlayerMovementModifiers : MonoBehaviour
{
    [Header("Wall Slide Settings")]
    [Tooltip("Initial downward speed on wall contact (e.g., -1 for a brief stick/pause).")]
    public float entryDownwardVelocity = -1f;

    [Tooltip("Terminal velocity downward while sliding on a wall.")]
    public float maxWallSlideSpeed = 20f;

    [Tooltip("Raycast distance to detect walls.")]
    public float wallCheckDistance = 1.2f;
    public LayerMask wallMask;

    [Header("Wall Bounce Settings")]
    public float wallBounceUpForce = 12f;
    public float wallBounceAwayForce = 14f;

    public bool IsWallSliding => isWallSliding;
    public Vector3 WallNormal => wallNormal;

    private Rigidbody rb;
    private PlayerController playerController;

    private bool isWallSliding;
    private bool wasWallSlidingLastFrame;
    private Vector3 wallNormal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        CheckForWall();
    }

    void FixedUpdate()
    {
        if (isWallSliding)
        {
            // Guarantee player is always sliding DOWN at least -1.5f (prevents sticking/freezing)
            if (rb.linearVelocity.y > -1.5f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -1.5f, rb.linearVelocity.z);
            }

            // Cap maximum slide speed
            if (rb.linearVelocity.y < -maxWallSlideSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxWallSlideSpeed, rb.linearVelocity.z);
            }
        }
    }

    private void CheckForWall()
    {
        // If grounded, never wall slide
        if (playerController != null && playerController.IsGrounded)
        {
            isWallSliding = false;
            return;
        }

        Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };
        bool wallHit = false;

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, wallMask))
            {
                wallHit = true;
                wallNormal = hit.normal;
                break;
            }
        }

        // CRITICAL: Only wall slide if touching a wall AND moving downward/falling!
        // This prevents freezing in place when releasing keys.
        isWallSliding = wallHit && rb.linearVelocity.y < 0.0f;
    }

    public void PerformWallBounce()
    {
        isWallSliding = false;
        wasWallSlidingLastFrame = false;

        // Launch up and away from the wall normal
        Vector3 bounceDirection = (wallNormal * wallBounceAwayForce) + (Vector3.up * wallBounceUpForce);
        rb.linearVelocity = bounceDirection;
    }
}