using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Forward")]
    [SerializeField] private float forwardSpeed = 10f;      // static speed

    [Header("Lanes")]
    [SerializeField] private int laneCount = 7;             
    [SerializeField] private float laneStep = 1f;           
    [SerializeField] private float laneChangeSpeed = 8f;    

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;         
    [SerializeField] private Transform groundCheck;        
    [SerializeField] private float groundRadius = 0.2f;    
    [SerializeField] private LayerMask groundMask;

    Animator myAnimator;
    private Rigidbody rb;
    private int minLane, maxLane;       
    private int currentLane = 0;        
    private int targetLane = 0;         
    private bool grounded;

    private bool canMove = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myAnimator = GetComponentInChildren<Animator>();

        int half = (laneCount - 1) / 2;
        minLane = -half;
        maxLane = +half;

        currentLane = Mathf.Clamp(Mathf.RoundToInt(transform.position.x / laneStep), minLane, maxLane);
        targetLane = currentLane;

        ResetPhysicsForRun();
    }

    private void Start()
    {
        Debug.Log($"[Player RB] isKinematic={rb.isKinematic}, detectCollisions={rb.detectCollisions}, layer={gameObject.layer}");
        myAnimator.SetBool("isRunning", true);
    }

    void Update()
    {
        if (!canMove) return;


        if (Input.GetKeyDown(KeyCode.A))
        {
            targetLane = Mathf.Max(targetLane - 1, minLane);
        }

        if (Input.GetKeyDown(KeyCode.D))
        { 
            targetLane = Mathf.Min(targetLane + 1, maxLane);
        }

        //Groundcheck
        grounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;        
        
        Vector3 start = rb.position;
                
        float targetX = targetLane * laneStep;                
        float step = laneChangeSpeed * Time.fixedDeltaTime;
        float newX = Mathf.MoveTowards(start.x, targetX, step);
                
        Vector3 forwardMove = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;
                
        rb.MovePosition(new Vector3(newX, start.y, start.z) + forwardMove);

        // changing currentLane
        if (Mathf.Abs(newX - targetX) < 0.001f)
        { 
            currentLane = targetLane;
        }
    }

    public void Halt(bool freezeRigidBody = true)
    {        
        canMove = false;
        myAnimator.SetBool("isRunning", false);
    }

    public void ResetForNewRun()
    {
        canMove = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (myAnimator) myAnimator.SetBool("isRunning", true);
        targetLane = currentLane = Mathf.Clamp(Mathf.RoundToInt(transform.position.x / laneStep), minLane, maxLane);
        ResetPhysicsForRun();
    }

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
