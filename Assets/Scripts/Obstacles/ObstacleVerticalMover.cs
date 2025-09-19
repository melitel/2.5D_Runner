using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ObstacleVerticalMover : MonoBehaviour
{
    [SerializeField] float clearance = 0.02f;

    Rigidbody rb; 
    Collider col;
    float centerY, startX, startZ;
    float amplitude, speed;
    bool configured;

    public void SetupFromCurrent(float amplitude, float speed)
    {
        this.amplitude = Mathf.Abs(amplitude);
        this.speed = Mathf.Abs(speed);

        var p = transform.position;
        startX = p.x; startZ = p.z;

        // поточна позиція вже стоїть на землі (після SnapToGround)
        centerY = p.y + this.amplitude;   // min = p.y - amplitude; ми підняли центр на +amp, щоб мінімум не нижче землі
        rb.position = new Vector3(startX, centerY, startZ);

        configured = true;
    }

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void FixedUpdate()
    {
        if (!configured) return;
        float y = centerY + Mathf.Sin(Time.time * speed) * amplitude;
        rb.MovePosition(new Vector3(startX, y, startZ));
    }
}
