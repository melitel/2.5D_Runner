using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pooling system for reusing instances of a prefab.
/// Helps reduce runtime allocations and instantiation overhead by reusing
/// pre-created objects instead of destroying/instantiating repeatedly.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private PooledBehaviour prefab;     // Prefab to pool
    [SerializeField] private int prewarm = 32;           // Number of instances pre-created on Awake
    [SerializeField] private Transform container;        // Parent container for pooled objects
    [SerializeField] private bool hardCapacity = false;  // If true, pool cannot grow beyond prewarm size

    private readonly Queue<PooledBehaviour> pool = new(); // Internal queue of available objects

    /// <summary>
    /// Initializes the pool by pre-creating objects (prewarm step).
    /// If no container is assigned, this GameObject becomes the parent container.
    /// </summary>
    void Awake()
    {
        if (!container)
        {
            container = transform;
        }

        // Pre-instantiate the defined number of objects
        for (int i = 0; i < prewarm; i++)
        {
            pool.Enqueue(Create());
        }
    }

    /// <summary>
    /// Creates a new pooled object instance.
    /// This object is initially inactive and parented to the container.
    /// </summary>
    private PooledBehaviour Create()
    {
        var obj = Instantiate(prefab, container);
        obj.gameObject.SetActive(false);
        obj.SetOwner(this); 
        return obj;
    }

    /// <summary>
    /// Attempts to retrieve an object from the pool.
    /// If the pool is empty and hard capacity is enabled, returns false.
    /// Otherwise, creates a new instance as needed.
    /// </summary>
    /// <param name="position">World position to place the object.</param>
    /// <param name="rotation">World rotation to apply to the object.</param>
    /// <param name="obj">The retrieved pooled object, or null if unavailable.</param>
    /// <returns>True if an object was successfully retrieved, false otherwise.</returns>
    public bool TryGet(Vector3 position, Quaternion rotation, out PooledBehaviour obj)
    {
        if (pool.Count == 0 && hardCapacity)
        {
            obj = null;
            return false;
        }

        // Reuse an existing instance or create a new one
        obj = pool.Count > 0 ? pool.Dequeue() : Create();

        // Prepare object for use
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(null, true); // detach from container
        obj.gameObject.SetActive(true);
        obj.OnSpawned();
        return true;
    }

    /// <summary>
    /// Overload of TryGet that spawns with default identity rotation.
    /// </summary>
    public bool TryGet(Vector3 position, out PooledBehaviour obj) =>
        TryGet(position, Quaternion.identity, out obj);

    /// <summary>
    /// Returns an object back to the pool.
    /// The object is deactivated, reparented under the container,
    /// and re-queued for future reuse.
    /// </summary>
    /// <param name="obj">The pooled object to return.</param>
    public void Return(PooledBehaviour obj)
    {
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(container, false);
        pool.Enqueue(obj);
    }
}