using TMPro;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI scoreTxt;       
    [SerializeField] private IntEventChannelSO coinCollected;
   
    public const string HighScoreKey = "HighScore";
    public const string LastScoreKey = "LastScore";

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    private void OnEnable()
    {
        if (coinCollected)
        { 
            coinCollected.OnEventRaised += OnCoinCollected;
        }
    }

    private void OnDisable()
    {
        if (coinCollected)
        { 
            coinCollected.OnEventRaised -= OnCoinCollected;
        }
        PlayerPrefs.SetInt(LastScoreKey, Score);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        UpdateUI();
    }

    private void OnCoinCollected(int value)
    {
        Score += value;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreTxt)
        {
            scoreTxt.text = Score.ToString();
        }
    }

}
