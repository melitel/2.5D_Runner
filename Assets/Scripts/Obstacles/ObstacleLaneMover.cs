using UnityEngine;

/// <summary>
/// Controls lateral movement of an obstacle along discrete lanes.
/// The obstacle moves left and right between calculated lane bounds,
/// reversing its direction when reaching the edges.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ObstacleLaneMover : MonoBehaviour
{
    [SerializeField] private float speed = 3f;  // Horizontal movement speed
    private int minLane, maxLane;               // Lane limits (min and max indices)
    private float laneStep;                     // Distance between lanes
    private int dir = 1;                        // Current direction: -1 = left, +1 = right

    private Rigidbody rb;                       // Cached Rigidbody for physics movement
    private float minX, maxX;                   // World-space X boundaries for movement

    /// <summary>
    /// Initializes obstacle lane movement configuration.
    /// Calculates the minimum and maximum allowed lanes for horizontal movement
    /// based on lane count and optional padding lanes that remain unused.
    /// Also clamps the starting position of the obstacle within the allowed range
    /// and randomly assigns initial movement direction.
    /// </summary>
    /// <param name="laneCount">Total number of available lanes.</param>
    /// <param name="step">Distance in world units between lanes.</param>
    /// <param name="paddingLanes">Number of lanes excluded from movement at both edges.</param>
    public void Setup(int laneCount, float step, int paddingLanes = 0)
    {
        int half = (laneCount - 1) / 2;
        minLane = -half + Mathf.Clamp(paddingLanes, 0, half);
        maxLane = half - Mathf.Clamp(paddingLanes, 0, half);
        laneStep = step;

        // Convert lane indices into world-space X coordinates
        minX = minLane * laneStep;
        maxX = maxLane * laneStep;

        // Pick random initial direction: left (-1) or right (+1)
        dir = Random.value < 0.5f ? -1 : 1;

        // Ensure initial position is within allowed bounds
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);

        // Apply position using Rigidbody if available (for physics consistency)
        if (rb)
        {
            rb.position = p;
        }
        else
        {
            transform.position = p;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Handles horizontal movement of the obstacle on each physics step.
    /// The obstacle moves in the current direction until it reaches
    /// the allowed bounds, where its direction is reversed.
    /// Movement is applied via Rigidbody.MovePosition for physics safety.
    /// </summary>
    void FixedUpdate()
    {
        Vector3 p = rb.position;
        p.x += dir * speed * Time.fixedDeltaTime;

        // If boundaries are reached, clamp position and reverse direction
        if (p.x <= minX || p.x >= maxX)
        {
            p.x = Mathf.Clamp(p.x, minX, maxX);
            dir *= -1;
        }

        // Apply movement
        rb.MovePosition(p);
    }
}
