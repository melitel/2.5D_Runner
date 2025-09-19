using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private IntEventChannelSO coinCollected;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;   
    [SerializeField] private AudioClip coinPickupSfx;
    [Range(0f, 1f)] public float coinPickupVolume = 1f;

    private void OnEnable() 
    { 
        if (coinCollected) coinCollected.OnEventRaised += OnCoinCollected; 
    }
    private void OnDisable() 
    { 
        if (coinCollected) coinCollected.OnEventRaised -= OnCoinCollected; 
    }

    private void OnCoinCollected(int value)
    {
        PlayOneShot(coinPickupSfx, coinPickupVolume);
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (!clip || !sfxSource) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
