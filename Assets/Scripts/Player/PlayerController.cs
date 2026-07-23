using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 7f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start() { rb = GetComponent<Rigidbody>(); }

    private void Update()
    {
        // Checks if player is touching the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Handle jumping input
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

    }

    private void FixedUpdate()
    {

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Move relative to players direction
        Vector3 direction = transform.right * x + transform.forward * z;

        // Apply that velocity
        rb.linearVelocity = new Vector3(direction.normalized.x * moveSpeed, rb.linearVelocity.y, direction.normalized.z * moveSpeed);
    }

}
