using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamically spawns level segments in front of the player.
/// Each segment contains obstacles and coins.
/// Segments behind the player are despawned and returned to pool.
/// </summary>
public class SegmentSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Segment")]
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private float segmentLength = 50f;
    [SerializeField] private int keepAhead = 3;    // Number of segments to always keep in front of the player
    [SerializeField] private int keepBehind = 1;   // Number of segments to keep behind player
    [Tooltip("How many lanes we consider a roadside")]
    [SerializeField] private int lanePadding = 1;

    [Header("Lanes")]
    [SerializeField] private int laneCount = 7;
    [SerializeField] private float laneStep = 1f;

    [Header("Coins (per segment)")]
    [SerializeField] private int coinsPerSegment = 10;  // How many coins to attempt spawning in each segment.
    [SerializeField] private ObjectPool coinPool;      // Object pool that manages coin instances.
    [SerializeField] private bool coinsCenterWeighted = true;   // If true, coin spawn is weighted toward center lanes (triangular distribution).
    [SerializeField] private float coinJitterZ = 0.35f;     // Random Z-offset applied to coins within a segment to avoid perfect lines.
    [SerializeField] private float coinY = 0.75f;   // Y-position at which coins are spawned above ground.

    [Header("Obstacles (per segment)")]
    [SerializeField] private int obstaclesPerSegment = 5;   // How many obstacles to attempt spawning per segment.
    [SerializeField] private ObjectPool cubeStaticPool;     //Object pool for cube-shaped static obstacles.
    [SerializeField] private ObjectPool rectStaticPool;     //Object pool for rectangular-shaped static obstacles.
    [SerializeField] private ObjectPool cubeMovingLanePool;     //Object pool for cube-shaped obstacles that move between lanes.
    [SerializeField] private ObjectPool rectMovingVerticalPool;     //Object pool for cube-shaped obstacles that move vertically.
    [SerializeField] private float obstacleY = 0.225f;      // Y-position where obstacles are placed (height from ground).
    [SerializeField] private LayerMask groundMask;      // LayerMask defining which layers are considered "ground"

    [Header("Spawn rules")]
    [SerializeField] private float minLocalZ = 10f;     // Minimum local Z offset inside a segment where objects can spawn.
    [SerializeField] private float maxLocalZ = 47f;     // Maximum local Z offset inside a segment where objects can spawn.
    [SerializeField] private LayerMask obstacleMask;        // LayerMask used to detect existing obstacles when spawning coins
    [SerializeField] private float rayHeight = 6f;      // Height at which raycasts originate when checking for obstacles under coins.

    private readonly LinkedList<Segment> _active = new();       // Active segment queue around the player.
    private Queue<Segment> _segmentPool = new();        // Pool of inactive/recycled segments.
    private int _half;      // Half the number of lanes (used for indexing).
    private int minLanePlayable;
    private int maxLanePlayable;

    /// <summary>
    /// Represents a single level segment.
    /// Holds root transform, spawn container, and its world Z range.
    /// </summary>
    private class Segment
    {
        public GameObject go;
        public Transform root;      // Root of the segment prefab
        public Transform spawnRoot; // Container for obstacles/coins
        public float startZ;        // Segment start position
        public float endZ;          // Segment end position
    }

    /// <summary>
    /// Ensures inspector values remain valid after changes in the editor.
    /// Runs automatically inside Unity Editor when values are modified.
    /// </summary>
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

        // Clamp Z spawn range inside segment
        minLocalZ = Mathf.Clamp(minLocalZ, 0f, segmentLength);
        maxLocalZ = Mathf.Clamp(maxLocalZ, 0f, segmentLength);
        if (maxLocalZ <= minLocalZ)
        { 
            maxLocalZ = Mathf.Min(segmentLength - 1f, minLocalZ + 1f);
        }    
    }

    void Awake()
    {
        _half = (laneCount - 1) / 2;
        minLanePlayable = -_half + Mathf.Clamp(lanePadding, 0, _half);
        maxLanePlayable = _half - Mathf.Clamp(lanePadding, 0, _half);
    }

    /// <summary>
    /// Initializes world with enough starting segments
    /// so that player is already inside an active queue.
    /// </summary>
    void Start()
    {
        float baseZ = Mathf.Floor(player.position.z / segmentLength) * segmentLength;
        for (int i = -keepBehind; i <= keepAhead; i++)
        {
            SpawnSegment(baseZ + i * segmentLength);
        }
    }

    /// <summary>
    /// Keeps world updated around the player:
    /// - Spawns new segments in front if necessary
    /// - Despawns old segments behind
    /// </summary>
    void Update()
    {
        if (_active.Count == 0) return;

        // Spawn ahead
        while (FrontEndZ() - player.position.z < keepAhead * segmentLength)
        {
            SpawnSegment(FrontEndZ());
        }

        // Despawn behind
        while (player.position.z - BackStartZ() > keepBehind * segmentLength)
        {
            DespawnBackSegment();
        }
    }

    /// <summary>
    /// Returns Z coordinate of the furthest active segment.
    /// </summary>
    private float FrontEndZ() => _active.Last.Value.endZ;

    /// <summary>
    /// Returns Z coordinate of the earliest active segment.
    /// </summary>
    private float BackStartZ() => _active.First.Value.startZ;

    /// <summary>
    /// Spawns a new segment at given Z position.
    /// Clears spawn container and fills it with obstacles & coins.
    /// </summary>
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

    /// <summary>
    /// Removes the oldest segment behind the player
    /// and returns its objects to the pool.
    /// </summary>
    private void DespawnBackSegment()
    {
        var seg = _active.First.Value;
        _active.RemoveFirst();
        ClearSpawnRoot(seg);
        ReturnSegment(seg);
    }

    /// <summary>
    /// Retrieves a segment from pool or instantiates a new one.
    /// </summary>
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

    /// <summary>
    /// Returns segment back into pool (disables it).
    /// </summary>
    private void ReturnSegment(Segment seg)
    {
        seg.go.SetActive(false);
        _segmentPool.Enqueue(seg);
    }

    /// <summary>
    /// Clears all children of a segment's spawn root.
    /// Returns pooled objects to pool instead of destroying.
    /// </summary>
    private void ClearSpawnRoot(Segment seg)
    {
        for (int i = seg.spawnRoot.childCount - 1; i >= 0; i--)
        {
            var t = seg.spawnRoot.GetChild(i);
            if (t.TryGetComponent<PooledBehaviour>(out var pooled))
                pooled.Despawn();
            else
                t.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Spawns coins randomly in this segment.
    /// Skips positions blocked by obstacles.
    /// </summary>
    private void SpawnCoins(Segment seg)
    {
        for (int i = 0; i < coinsPerSegment; i++)
        {
            float localZ = Random.Range(minLocalZ, maxLocalZ);
            float z = seg.startZ + localZ + Random.Range(-coinJitterZ, +coinJitterZ);

            int lane = PickPlayableLane();
            float x = lane * laneStep;

            // Avoid placing coins inside obstacles
            Vector3 rayOrigin = new Vector3(x, rayHeight, z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, rayHeight * 2f, obstacleMask))
            { 
                continue;
            }

            float y = coinY;
            if (coinPool.TryGet(new Vector3(x, y, z), out var coin))
                coin.transform.SetParent(seg.spawnRoot, true);
        }
    }

    /// <summary>
    /// Spawns random obstacles (static or moving) inside this segment.
    /// </summary>
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
                    {
                        SetupStatic(o1.transform);
                        o1.transform.SetParent(seg.spawnRoot, true);
                    }
                    break;

                case 2:
                    if (rectStaticPool.TryGet(pos, out var o2))
                    {
                        SetupStatic(o2.transform);
                        o2.transform.SetParent(seg.spawnRoot, true);
                    }
                    break;

                case 3:
                    if (cubeMovingLanePool.TryGet(pos, out var o3))
                    {
                        SetupHorizontalMove(o3.transform, laneCount, laneStep, lanePadding);
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

    /// <summary>
    /// Disables all movement components on obstacle and freezes Rigidbody.
    /// </summary>
    private static void SetupStatic(Transform t)
    {
        var h = t.GetComponent<ObstacleLaneMover>(); if (h) h.enabled = false;
        var v = t.GetComponent<ObstacleVerticalMover>(); if (v) v.enabled = false;

        if (t.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Configures obstacle to move horizontally between lanes.
    /// </summary>
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

    /// <summary>
    /// Configures obstacle to move vertically up and down (sinusoidal).
    /// </summary>
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

    /// <summary>
    /// Picks a random lane index within playable range.
    /// Supports optional "center weighted" mode (triangular distribution).
    /// </summary>
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

        // Weighted towards center lanes
        float t = Random.value;
        float tri = 1f - Mathf.Abs(t * 2f - 1f);
        return Mathf.RoundToInt(Mathf.Lerp(minLanePlayable, maxLanePlayable, tri));
    }
}
