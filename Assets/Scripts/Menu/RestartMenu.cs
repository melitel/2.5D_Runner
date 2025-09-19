using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI highestScoreText;
    [SerializeField] private TextMeshProUGUI currentScoreText;

    private void Start()
    {
        int highScore = PlayerPrefs.GetInt(CollectibleManager.HighScoreKey, 0);
        int lastScore = PlayerPrefs.GetInt(CollectibleManager.LastScoreKey, 0);

        highestScoreText.text = highScore.ToString();
        currentScoreText.text = lastScore.ToString();
    }

    public void OnRestartGameClicked()
    {        
        SceneManager.LoadScene("Game");
    }

    public void OnExitGameClicked()
    {
        // Check if the application is running in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
