using UnityEngine;

public abstract class PooledBehaviour : MonoBehaviour
{
    private ObjectPool _owner;
    private bool _returned;

    public void SetOwner(ObjectPool pool) => _owner = pool;

    protected virtual void OnEnable() { _returned = false; }
    protected virtual void OnDisable() { }

    /// Return object to pool
    public void Despawn()
    {
        if (_returned) return;
        _returned = true;

        if (_owner != null) _owner.Return(this);
        else gameObject.SetActive(false);
    }

    /// Called by pool after activation for state reset
    public virtual void OnSpawned() { }
}
