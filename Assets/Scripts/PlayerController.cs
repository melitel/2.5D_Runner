using System;
using UnityEngine;

/// <summary>
/// Controls player movement:
/// - Automatic forward motion
/// - Lane switching left/right
/// - Jumping
/// - Start/stop state handling
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Forward")]
    [SerializeField] private float forwardSpeed = 10f;      // Constant forward speed

    [Header("Air Control")]
    [SerializeField] private float airForwardMultiplier = 0.55f; // 55% from ground speed
    [SerializeField] private float extraFallGravity = 15f;       // additional gravity while falling
    [SerializeField] private float lowJumpGravity = 12f;         // lowering height 

    [Header("Lanes")]
    [SerializeField] private int laneCount = 7;             // Total number of horizontal lanes
    [SerializeField] private float laneStep = 1f;           // Distance in world units between lanes
    [SerializeField] private float laneChangeSpeed = 8f;    // How fast player moves horizontally between lanes

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;          // Upward impulse applied during jump

    private Animator myAnimator;
    private Rigidbody rb;

    private int minLane, maxLane;       // Lane index boundaries
    private int currentLane = 0;        // Player's current lane index
    private int targetLane = 0;         // Lane index player is moving toward
    private bool grounded;              // True if the player is standing on ground
    private bool jumpHeld;

    private bool canMove = true;        // Global movement lock

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myAnimator = GetComponentInChildren<Animator>();

        int half = (laneCount - 1) / 2;
        minLane = -half;
        maxLane = +half;

        // Determine current lane based on X position
        currentLane = Mathf.Clamp(Mathf.RoundToInt(transform.position.x / laneStep), minLane, maxLane);
        targetLane = currentLane;

        ResetPhysicsForRun();
    }

    private void Start()
    {
        Debug.Log($"[Player RB] isKinematic={rb.isKinematic}, detectCollisions={rb.detectCollisions}, layer={gameObject.layer}");
        myAnimator.SetBool("isRunning", true);
    }

    /// <summary>
    /// Handles input and jump logic.
    /// - A/D (or Left/Right) switch lanes
    /// - Space performs jump if grounded
    /// </summary>
    void Update()
    {
        if (!canMove) return;

        // Lane switching
        if (Input.GetKeyDown(KeyCode.A))
        {
            targetLane = Mathf.Max(targetLane - 1, minLane);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            targetLane = Mathf.Min(targetLane + 1, maxLane);
        }

        // Holding jump button
        if (Input.GetKeyDown(KeyCode.Space)) jumpHeld = true;
        if (Input.GetKeyUp(KeyCode.Space)) jumpHeld = false;

        // Jump
        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            // Reset vertical velocity before jump
            Vector3 v = rb.linearVelocity;
            v.y = 0;
            rb.linearVelocity = v;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);            
        }
    }

    /// <summary>
    /// Applies forward movement and lane interpolation in physics update.
    /// Moves player smoothly toward target lane while constantly moving forward.
    /// </summary>
    void FixedUpdate()
    {
        if (!canMove) return;

        if (!grounded)
        {
            if (rb.linearVelocity.y < 0f)
            {
                // falling
                rb.AddForce(Vector3.down * extraFallGravity, ForceMode.Acceleration);
            }
            else if (!jumpHeld)
            {
                // “low jump” — not holding button - short jump
                rb.AddForce(Vector3.down * lowJumpGravity, ForceMode.Acceleration);
            }
        }

        // Moving forward with airForwardMultiplier 
        float forward = forwardSpeed * (grounded ? 1f : airForwardMultiplier);

        Vector3 start = rb.position;

        // Smooth horizontal interpolation toward target lane
        float targetX = targetLane * laneStep;
        float step = laneChangeSpeed * Time.fixedDeltaTime;
        float newX = Mathf.MoveTowards(start.x, targetX, step);

        // Constant forward motion
        Vector3 forwardMove = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;

        // Apply movement
        rb.MovePosition(new Vector3(newX, start.y, start.z) + forwardMove);

        // Update current lane once reached
        if (Mathf.Abs(newX - targetX) < 0.001f)
        {
            currentLane = targetLane;
        }
    }
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Ground")
        {
            grounded = true;
        }
    }
    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.tag == "Ground")
        {
            grounded = false;
        }
    }

    /// Stops all movement and switches animation to idle/standing.
    public void Halt(bool freezeRigidBody = true)
    {
        canMove = false;
        myAnimator.SetBool("isRunning", false);
    }

    /// <summary>
    /// Resets player state for a new run.
    /// Re-enables movement, resets physics, and starts running animation.
    /// </summary>
    public void ResetForNewRun()
    {
        canMove = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (myAnimator) myAnimator.SetBool("isRunning", true);

        targetLane = currentLane = Mathf.Clamp(
            Mathf.RoundToInt(transform.position.x / laneStep), minLane, maxLane);

        ResetPhysicsForRun();
    }

    /// <summary>
    /// Helper method to restore Rigidbody defaults for active gameplay.
    /// Ensures gravity, collisions, and movement settings are re-enabled.
    /// </summary>
    private void ResetPhysicsForRun()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
