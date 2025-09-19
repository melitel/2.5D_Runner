using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Segment")]
    [SerializeField] private GameObject segmentPrefab;  
    [SerializeField] private float segmentLength = 50f; 
    [SerializeField] private int keepAhead = 3;    //segments to keep ahead     
    [SerializeField] private int keepBehind = 1;   //segments to keep behind
    //[Tooltip("How many lanes we consider a roadside")]
    [SerializeField] private int lanePadding = 1;

    [Header("Lanes")]
    [SerializeField] private int laneCount = 7;
    [SerializeField] private float laneStep = 1f;    

    [Header("Coins (per segment)")]
    [SerializeField] private int coinsPerSegment = 10;
    [SerializeField] private ObjectPool coinPool;
    [SerializeField] private bool coinsCenterWeighted = true;
    [SerializeField] private float coinJitterZ = 0.35f;
    [SerializeField] private float coinY = 0.75f;

    [Header("Obstacles (per segment)")]
    [SerializeField] private int obstaclesPerSegment = 5;
    [SerializeField] private ObjectPool cubeStaticPool;       
    [SerializeField] private ObjectPool rectStaticPool;       
    [SerializeField] private ObjectPool cubeMovingLanePool;   
    [SerializeField] private ObjectPool rectMovingVerticalPool; 
    [SerializeField, Range(0f, 1f)] private float aboveObstacleCoinChance = 0.5f;
    [SerializeField] private float obstacleY = 0.225f;
    [SerializeField] private LayerMask groundMask;

    [Header("Spawn rules")]
    [SerializeField] private float minLocalZ = 10f;      
    [SerializeField] private float maxLocalZ = 47f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float rayHeight = 6f;
    [SerializeField] private float airborneOffset = 0.6f;
        
    private readonly LinkedList<Segment> _active = new();
    private Queue<Segment> _segmentPool = new();
    private int _half;
    private int minLanePlayable;
    private int maxLanePlayable;

    private class Segment
    {
        public GameObject go;
        public Transform root;      // segment
        public Transform spawnRoot; // container
        public float startZ;        
        public float endZ;          
    }

    void OnValidate()
    {
        if (segmentLength <= 0f) segmentLength = 50f;
        if (keepAhead < 1) keepAhead = 1;
        if (keepBehind < 0) keepBehind = 0;

        laneCount = Mathf.Max(1, laneCount | 1); 
        _half = (laneCount - 1) / 2;

        lanePadding = Mathf.Clamp(lanePadding, 0, _half);
        minLanePlayable = -_half + lanePadding;
        maxLanePlayable = _half - lanePadding;
        if (minLanePlayable > maxLanePlayable) 
        { 
            minLanePlayable = maxLanePlayable = 0; 
        }

        // min\max in the segment range
        minLocalZ = Mathf.Clamp(minLocalZ, 0f, segmentLength);
        maxLocalZ = Mathf.Clamp(maxLocalZ, 0f, segmentLength);
        if (maxLocalZ <= minLocalZ) maxLocalZ = Mathf.Min(segmentLength - 1f, minLocalZ + 1f);
    }

    void Awake()
    {
        _half = (laneCount - 1) / 2;
        minLanePlayable = -_half + Mathf.Clamp(lanePadding, 0, _half);
        maxLanePlayable = _half - Mathf.Clamp(lanePadding, 0, _half);
    }

    void Start()
    {
        // Initializing segments so our Player is inside the queue
        float baseZ = Mathf.Floor(player.position.z / segmentLength) * segmentLength;
        for (int i = -keepBehind; i <= keepAhead; i++)
        {
            SpawnSegment(baseZ + i * segmentLength);
        }
    }

    void Update()
    {
        if (_active.Count == 0) return;

        // Spawn ahead if not enough
        while (FrontEndZ() - player.position.z < keepAhead * segmentLength)
        { 
            SpawnSegment(FrontEndZ());
        }

        // Despawn odd
        while (player.position.z - BackStartZ() > keepBehind * segmentLength)
        { 
            DespawnBackSegment();
        }
    }  

    private float FrontEndZ()
    {
        return _active.Last.Value.endZ;
    }

    private float BackStartZ()
    {
        return _active.First.Value.startZ;
    }

    private void SpawnSegment(float startZ)
    {
        Segment seg = GetSegment();
        seg.startZ = startZ;
        seg.endZ = startZ + segmentLength;

        seg.root.position = new Vector3(0f, 0f, startZ); 
                
        ClearSpawnRoot(seg);
                
        SpawnObstacles(seg);
        SpawnCoins(seg);

        _active.AddLast(seg);
    }

    private void DespawnBackSegment()
    {
        var seg = _active.First.Value;
        _active.RemoveFirst();
        // Return coins and obstacles to pool
        ClearSpawnRoot(seg);
        ReturnSegment(seg);
    }

    private Segment GetSegment()
    {
        if (_segmentPool.Count > 0)
        {
            var seg = _segmentPool.Dequeue();
            seg.go.SetActive(true);
            return seg;
        }

        var go = Instantiate(segmentPrefab);
        var segNew = new Segment
        {
            go = go,
            root = go.transform,
            spawnRoot = go.transform.Find("SpawnRoot") ?? go.transform
        };
        return segNew;
    }

    private void ReturnSegment(Segment seg)
    {
        seg.go.SetActive(false);
        _segmentPool.Enqueue(seg);
    }

    private void ClearSpawnRoot(Segment seg)
    {
        for (int i = seg.spawnRoot.childCount - 1; i >= 0; i--)
        {
            var t = seg.spawnRoot.GetChild(i);
            if (t.TryGetComponent<PooledBehaviour>(out var pooled))
            {
                pooled.Despawn();       
            }
            else
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void SpawnCoins(Segment seg)
    {
        for (int i = 0; i < coinsPerSegment; i++)
        {
            float localZ = Random.Range(minLocalZ, maxLocalZ);
            float z = seg.startZ + localZ + Random.Range(-coinJitterZ, +coinJitterZ);

            int lane = PickPlayableLane();                 
            float x = lane * laneStep;

            // DO NOT spawn coins over obstacles: if there is something with obstacleMask under the coin → skip it
            Vector3 rayOrigin = new Vector3(x, rayHeight, z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, rayHeight * 2f, obstacleMask))
                continue;

            float y = coinY;
            if (coinPool.TryGet(new Vector3(x, y, z), out var coin))
                coin.transform.SetParent(seg.spawnRoot, true);
        }
    }

    private void SpawnObstacles(Segment seg)
    {
        for (int i = 0; i < obstaclesPerSegment; i++)
        {
            float localZ = Random.Range(minLocalZ, maxLocalZ);
            float z = seg.startZ + localZ;

            int type = Random.Range(1, 5);
            int lane = PickPlayableLane();                  
            float x = lane * laneStep;
            Vector3 pos = new Vector3(x, obstacleY, z);

            switch (type)
            {
                case 1:
                    if (cubeStaticPool.TryGet(pos, out var o1))
                    { SetupStatic(o1.transform); o1.transform.SetParent(seg.spawnRoot, true); }
                    break;

                case 2:
                    if (rectStaticPool.TryGet(pos, out var o2))
                    { SetupStatic(o2.transform); o2.transform.SetParent(seg.spawnRoot, true); }
                    break;

                case 3:
                    if (cubeMovingLanePool.TryGet(pos, out var o3))
                    {
                        SetupHorizontalMove(o3.transform, laneCount, laneStep, lanePadding);  // ← передаємо padding
                        o3.transform.SetParent(seg.spawnRoot, true);
                    }
                    break;

                case 4:
                    if (rectMovingVerticalPool.TryGet(pos, out var o4))
                    {
                        GroundUtil.SnapToGround(o4.transform, groundMask, 4f, 0.02f);
                        SetupVerticalMove(o4.transform, amplitude: 1.0f, speed: Random.Range(0.6f, 1.2f));
                        o4.transform.SetParent(seg.spawnRoot, true);
                    }
                    break;
            }
        }
    }

    private static void SetupStatic(Transform t)
    {        
        var h = t.GetComponent<ObstacleLaneMover>(); if (h) h.enabled = false;
        var v = t.GetComponent<ObstacleVerticalMover>(); if (v) v.enabled = false;
        
        if (t.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private static void SetupHorizontalMove(Transform t, int laneCount, float laneStep, int lanePadding)
    {
        var h = t.GetComponent<ObstacleLaneMover>();
        var v = t.GetComponent<ObstacleVerticalMover>();
        if (v) v.enabled = false;

        if (h)
        {
            h.enabled = true;
            h.Setup(laneCount, laneStep, lanePadding);     
        }
    }

    private static void SetupVerticalMove(Transform t, float amplitude, float speed)
    {
        var h = t.GetComponent<ObstacleLaneMover>(); if (h) h.enabled = false;
        var v = t.GetComponent<ObstacleVerticalMover>();
        if (v)
        {
            v.enabled = true;
            v.SetupFromCurrent(amplitude, speed);
        }
    }


    private int PickPlayableLane()
    {
        if (minLanePlayable == maxLanePlayable)
        { 
            return minLanePlayable;
        }

        if (!coinsCenterWeighted)
        { 
            return Random.Range(minLanePlayable, maxLanePlayable + 1);
        }

        // triangular distribution to the center within playable
        float t = Random.value;
        float tri = 1f - Mathf.Abs(t * 2f - 1f);
        return Mathf.RoundToInt(Mathf.Lerp(minLanePlayable, maxLanePlayable, tri));
    }
}
