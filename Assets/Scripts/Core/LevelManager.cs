using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // Fields
    [SerializeField] private List<LevelData> levels;

    private int currentLevelIndex;

    private const string LevelIndexKey = "CurrentLevelIndex";
    private const string LastGameResultKey = "LastGameResult"; // UI Purposes

    public static LevelManager Instance { get; private set; }

    public LevelData CurrentLevelData => levels[currentLevelIndex];
    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevels => levels.Count;

    // Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentLevelIndex = PlayerPrefs.GetInt(LevelIndexKey, 0);
        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
    }

    public void OnLevelWin()
    {
        PlayerPrefs.SetInt(LastGameResultKey, (int)GameResult.Win);
        currentLevelIndex = (currentLevelIndex + 1) % levels.Count;
        PlayerPrefs.SetInt(LevelIndexKey, currentLevelIndex);
        PlayerPrefs.Save();
    }

    public void OnLevelFail(GameResult failReason)
    {
        PlayerPrefs.SetInt(LastGameResultKey, (int)failReason);
        PlayerPrefs.Save();
    }

    public static GameResult GetLastGameResult()
    {
        return (GameResult)PlayerPrefs.GetInt(LastGameResultKey, (int)GameResult.FailTimerExpired);
    }
}