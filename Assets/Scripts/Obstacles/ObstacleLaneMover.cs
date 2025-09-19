using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObstacleLaneMover : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private int minLane, maxLane;
    private float laneStep;
    private int dir = 1;

    private Rigidbody rb;
    private float minX, maxX;

    public void Setup(int laneCount, float step, int paddingLanes = 0)
    {
        int half = (laneCount - 1) / 2;
        minLane = -half + Mathf.Clamp(paddingLanes, 0, half);
        maxLane = half - Mathf.Clamp(paddingLanes, 0, half);
        laneStep = step;

        minX = minLane * laneStep;
        maxX = maxLane * laneStep;

        dir = Random.value < 0.5f ? -1 : 1;

        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        if (rb) rb.position = p; else transform.position = p;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();        
    }

    void FixedUpdate()
    {
        Vector3 p = rb.position;
        p.x += dir * speed * Time.fixedDeltaTime;

        if (p.x <= minX || p.x >= maxX)
        {
            p.x = Mathf.Clamp(p.x, minX, maxX);
            dir *= -1;
        }

        rb.MovePosition(p);
    }
}
