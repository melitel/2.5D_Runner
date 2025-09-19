using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private PooledBehaviour prefab;
    [SerializeField] private int prewarm = 32;
    [SerializeField] private Transform container;
    [SerializeField] private bool hardCapacity = false; 

    private readonly Queue<PooledBehaviour> pool = new();

    void Awake()
    {
        if (!container)
        { 
            container = transform;
        }
        for (int i = 0; i < prewarm; i++)
        { 
            pool.Enqueue(Create());
        }
    }

    private PooledBehaviour Create()
    {
        var obj = Instantiate(prefab, container);
        obj.gameObject.SetActive(false);
        obj.SetOwner(this);
        return obj;
    }

    public bool TryGet(Vector3 position, Quaternion rotation, out PooledBehaviour obj)
    {
        if (pool.Count == 0 && hardCapacity) { obj = null; return false; }
        obj = pool.Count > 0 ? pool.Dequeue() : Create();

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(null, true);         // taking out from the container
        obj.gameObject.SetActive(true);
        obj.OnSpawned();
        return true;
    }

    public bool TryGet(Vector3 position, out PooledBehaviour obj) =>
        TryGet(position, Quaternion.identity, out obj);

    public void Return(PooledBehaviour obj)
    {
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(container, false);
        pool.Enqueue(obj);
    }
}
