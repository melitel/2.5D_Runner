using UnityEngine;

public class ObstacleLaneMover : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private int minLane, maxLane;
    private float laneStep;
    private int dir = 1;

    public void Setup(int laneCount, float step)
    {
        int half = (laneCount - 1) / 2;
        minLane = -half; 
        maxLane = half;
        laneStep = step;
        // random direction
        dir = Random.value < 0.5f ? -1 : 1;
    }

    void Update()
    {
        Vector3 p = transform.position;
        p.x += dir * speed * Time.deltaTime;
        float minX = minLane * laneStep, maxX = maxLane * laneStep;
        if (p.x <= minX || p.x >= maxX) 
        { 
            p.x = Mathf.Clamp(p.x, minX, maxX); 
            dir *= -1; 
        }
        transform.position = p;
    }
}
