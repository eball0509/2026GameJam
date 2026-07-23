using UnityEngine;

public class PlayerSpeedTracker : MonoBehaviour
{

    private Rigidbody rb;

    [Header("Speed Settings")]
    public float currentSpeed;
    public float minimumRequiredSpeed = 5f;
    public float explodeTimer = 3f;

    private float countdown;

    void Start()
    {

        rb = GetComponent<Rigidbody>();
        countdown = explodeTimer;

    }

    void Update()
    {

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        currentSpeed = horizontalVelocity.magnitude;

        if (currentSpeed < minimumRequiredSpeed)
        {
            countdown -= Time.deltaTime;
            Debug.Log($"SLOW AHH! Exploding in: {countdown:F1} seconds");

            if (countdown <= 0)
            {
                Explode();
            }
        }
        else
        {
            countdown = explodeTimer;
        }

    }

    private void Explode()
    {
        Debug.Log("You suh and died");
    }

}
