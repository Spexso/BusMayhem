using System;
using UnityEngine;

public enum GameState
{
    Idle,
    Playing,
    Win,
    Fail
}

public class GameManager : MonoBehaviour
{
    // Fields
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BusManager busManager;
    [SerializeField] private WaitingAreaManager waitingAreaManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ColorMatchPalette colorMap;

    private GameState currentState;
    private int stickmenOnMove;
    private GameResult pendingFailResult;
    private bool pendingFail;

    public static GameManager Instance { get; private set; }
    public static event Action<GameState> OnGameStateChanged;
    public GameState CurrentState => currentState;

    // Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[GameManager] LevelManager instance is null.");
            return;
        }

        LevelData levelData = LevelManager.Instance.CurrentLevelData;

        if (levelData == null)
        {
            Debug.LogError("[GameManager] CurrentLevelData is null.");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("[GameManager] GridManager is not assigned.");
            return;
        }

        if (waitingAreaManager == null)
        {
            Debug.LogError("[GameManager] WaitingAreaManager is not assigned.");
            return;
        }

        if (busManager == null)
        {
            Debug.LogError("[GameManager] BusManager is not assigned.");
            return;
        }

        if (timerManager == null)
        {
            Debug.LogError("[GameManager] TimerManager is not assigned.");
            return;
        }

        if (colorMap == null)
        {
            Debug.LogError("[GameManager] ColorMatchPalette is not assigned.");
            return;
        }

        ColorConverter.Initialize(colorMap);
        gridManager.InitializeGrid(levelData);
        waitingAreaManager.Initialize(levelData);
        busManager.Initialize(levelData);
        timerManager.Initialize(levelData);

        if (InputManager.Instance == null)
        {
            Debug.LogError("[GameManager] InputManager instance is null.");
            return;
        }

        InputManager.Instance.SetInputEnabled(false);

        busManager.OnBusDepart += HandleBusDepart;
        waitingAreaManager.OnWaitingAreaFull += HandleWaitingAreaFull;
        timerManager.OnTimerExpired += HandleTimerExpired;

        SetState(GameState.Idle);
        StartGame();

        OnGameStateChanged += state => Debug.Log($"[GameManager] State: {state}");
    }

    private void OnDestroy()
    {
        if (busManager != null)
            busManager.OnBusDepart -= HandleBusDepart;

        if (waitingAreaManager != null)
            waitingAreaManager.OnWaitingAreaFull -= HandleWaitingAreaFull;

        if (timerManager != null)
            timerManager.OnTimerExpired -= HandleTimerExpired;
    }

    public void StartGame()
    {
        if (currentState != GameState.Idle)
            return;

        if (InputManager.Instance == null)
        {
            Debug.LogError("[GameManager] InputManager instance is null.");
            return;
        }

        SetState(GameState.Playing);
        InputManager.Instance.SetInputEnabled(true);
        timerManager.StartTimer();
    }

    public void OnStickmanWalkStarted()
    {
        stickmenOnMove++;
    }

    public void OnStickmanWalkEnded()
    {
        stickmenOnMove--;

        if (stickmenOnMove < 0)
            stickmenOnMove = 0;

        if (pendingFail && stickmenOnMove == 0)
        {
            pendingFail = false;
            TriggerFail(pendingFailResult);
        }
    }

    private void HandleBusDepart()
    {
        if (currentState != GameState.Playing)
            return;

        bool gridEmpty = !gridManager.HasAnyStickmen();
        bool waitingEmpty = !waitingAreaManager.HasAnyStickmans();

        if (gridEmpty && waitingEmpty)
            TriggerWin();
    }

    private void HandleWaitingAreaFull()
    {
        if (currentState != GameState.Playing)
            return;

        if (stickmenOnMove > 0)
        {
            pendingFail = true;
            pendingFailResult = GameResult.FailWaitingAreaFull;
            return;
        }

        TriggerFail(GameResult.FailWaitingAreaFull);
    }

    private void HandleTimerExpired()
    {
        if (currentState != GameState.Playing)
            return;

        TriggerFail(GameResult.FailTimerExpired);
    }

    private void TriggerWin()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("[GameManager] InputManager instance is null.");
            return;
        }

        InputManager.Instance.SetInputEnabled(false);
        timerManager.StopTimer();
        LevelManager.Instance.OnLevelWin();
        PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins", 0) + 10);
        PlayerPrefs.Save();
        SetState(GameState.Win);
    }

    private void TriggerFail(GameResult failReason)
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("[GameManager] InputManager instance is null.");
            return;
        }

        InputManager.Instance.SetInputEnabled(false);
        timerManager.StopTimer();
        LevelManager.Instance.OnLevelFail(failReason);
        SetState(GameState.Fail);
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }
}