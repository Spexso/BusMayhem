using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameplayScreenUI : MonoBehaviour
{
    // Fields
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timerText;


    // Methods
    private void Start()
    {
        levelText.text = $"Level {LevelManager.Instance.CurrentLevelIndex + 1}";
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
        TimerManager.OnTimerUpdated += HandleTimerUpdated;
    }

    private void OnDisable()
    {

    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.Win)
        {
            SceneLoader.LoadEndScene();
        }
        else if (newState == GameState.Fail)
        {
            SceneLoader.LoadEndScene();
        }
    }

    private void HandleTimerUpdated(float remaining)
    {
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
