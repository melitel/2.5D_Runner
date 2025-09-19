using UnityEngine;

/// <summary>
/// Base class for objects managed by ObjectPool.
/// Provides a common lifecycle for pooled objects, including spawn and despawn logic.
/// Any behaviour that needs to be pooled should inherit from this class.
/// </summary>
public abstract class PooledBehaviour : MonoBehaviour
{
    private ObjectPool _owner;   // Reference to the pool that created/owns this object
    private bool _returned;      // Prevents multiple returns of the same object

    /// Assigns the pool that owns this object.
    public void SetOwner(ObjectPool pool) => _owner = pool;


    /// Unity callback invoked when the object is enabled.
    /// Resets the "returned" flag to allow the object to be despawned later.
    protected virtual void OnEnable() { _returned = false; }


    /// Unity callback invoked when the object is disabled.
    protected virtual void OnDisable() { }


    /// Returns this object back to its pool.
    /// If the object has already been returned once, further calls are ignored.
    /// If the object has no pool owner assigned, it will simply be deactivated.
    public void Despawn()
    {
        if (_returned) return;    // Prevent duplicate returns
        _returned = true;

        if (_owner != null)
        {
            _owner.Return(this);  // Return to pool
        }
        else
        {
            // Fallback: deactivate the object if no pool is assigned
            gameObject.SetActive(false);
        }
    }
    public virtual void OnSpawned() { }
}
