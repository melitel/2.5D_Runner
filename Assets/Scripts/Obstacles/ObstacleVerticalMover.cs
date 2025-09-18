using UnityEngine;

public class ObstacleVerticalMover : MonoBehaviour
{
    private float baseY;
    private float amplitude = 1f;
    private float speed = 1f;

    public void Setup(float baseY, float amplitude, float speed)
    {
        this.baseY = baseY;
        this.amplitude = amplitude;
        this.speed = speed;
    }

    void Update()
    {
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = p;
    }
}
