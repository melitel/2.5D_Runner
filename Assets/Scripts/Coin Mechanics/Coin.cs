using UnityEngine;

public class Coin : PooledBehaviour
{
    public int value = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // TODO: Coin Wallet CoinEvents.RaiseCollected(value);
        Despawn();
    }
}
