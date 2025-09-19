using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private CrashEventChannelSO playerCrashed;

    [Header("Explosion / Player")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private PlayerController player;

    private void Start()
    {        
        if (player) player.ResetForNewRun();
    }
    void OnEnable() 
    {
        if (playerCrashed)
        { 
            playerCrashed.OnEventRaised += OnPlayerCrashed; 
        }
    }
    void OnDisable() 
    {  
        if (playerCrashed)
        { 
            playerCrashed.OnEventRaised -= OnPlayerCrashed; 
        }
    }

    void OnPlayerCrashed(Vector3 atPosition)
    {

        if (explosionPrefab)
        { 
            Instantiate(explosionPrefab, atPosition, Quaternion.identity);
        }

        player.Halt();
        StartCoroutine(LoadRestartMenuWithDelay(2f));
    }

    private IEnumerator LoadRestartMenuWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene("RestartMenu");        
    }

}
