using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnStartGameClicked()
    {
        Debug.Log("Start clicked");
        SceneManager.LoadScene("Game");
    }

    public void OnExitClicked()
    {
        // Check if the application is running in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
