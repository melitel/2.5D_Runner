using UnityEngine;

public class Coin : PooledBehaviour
{
    [SerializeField] private IntEventChannelSO coinCollected;

    public int value = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        coinCollected?.Raise(value);
        Despawn();
    }
}
