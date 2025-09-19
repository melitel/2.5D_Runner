using UnityEngine;

/// <summary>
/// Makes an obstacle move up and down (vertical oscillation).
/// Uses a kinematic Rigidbody to smoothly animate between ground level
/// and a configurable amplitude above it.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ObstacleVerticalMover : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;

    private float centerY;   // Vertical center point of oscillation
    private float startX;    // Fixed X position
    private float startZ;    // Fixed Z position
    private float amplitude; // Distance from center to top/bottom extremes
    private float speed;     // Speed multiplier for oscillation
    private bool configured; // Flag to ensure SetupFromCurrent() was called

    /// <summary>
    /// Configures the vertical oscillation based on current position.
    /// The current Y position is assumed to be "ground level" (after SnapToGround).
    /// The oscillation center is shifted upward by the amplitude,
    /// ensuring that the minimum Y never goes below ground.
    /// </summary>
    /// <param name="amplitude">Maximum distance above or below the center point.</param>
    /// <param name="speed">Oscillation speed multiplier.</param>
    public void SetupFromCurrent(float amplitude, float speed)
    {
        this.amplitude = Mathf.Abs(amplitude);
        this.speed = Mathf.Abs(speed);

        var p = transform.position;
        startX = p.x;
        startZ = p.z;

        // Current position is treated as ground contact.
        // Center is shifted upward by +amplitude so the minimum Y stays above ground.
        centerY = p.y + this.amplitude;

        // Immediately snap Rigidbody position to the oscillation center
        rb.position = new Vector3(startX, centerY, startZ);

        configured = true;
    }

    /// <summary>
    /// Initializes and configures Rigidbody for scripted movement.
    /// - Rigidbody is set to kinematic (moved via MovePosition).
    /// - Gravity is disabled since movement is scripted.
    /// - Interpolation and continuous speculative detection are enabled for smoother collisions.
    /// </summary>
    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    /// <summary>
    /// Applies vertical oscillation every physics update.
    /// The obstacle moves sinusoidally between (centerY - amplitude) and (centerY + amplitude).
    /// </summary>
    void FixedUpdate()
    {
        if (!configured) return;

        float y = centerY + Mathf.Sin(Time.time * speed) * amplitude;
        rb.MovePosition(new Vector3(startX, y, startZ));
    }
}
