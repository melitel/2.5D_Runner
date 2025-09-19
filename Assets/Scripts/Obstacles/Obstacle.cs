using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Obstacle : PooledBehaviour
{
    [SerializeField] private CrashEventChannelSO playerCrashed;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        //Debug.Log($"[Obstacle] Trigger with {other.name} at {Time.time:F2}");
        playerCrashed?.Raise(other.transform.position);
    }
}