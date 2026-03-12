using System;
using Unity.VisualScripting;
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
        // Load level data from LevelManager
        LevelData levelData = LevelManager.Instance.CurrentLevelData;

        ColorConverter.Initialize(colorMap);
        gridManager.InitializeGrid(levelData);
        waitingAreaManager.Initialize(levelData);
        busManager.Initialize(levelData);
        timerManager.Initialize(levelData);

        InputManager.Instance.SetInputEnabled(false);

        busManager.OnBusDepart += HandleBusDepart;
        waitingAreaManager.OnWaitingAreaFull += HandleWaitingAreaFull;
        timerManager.OnTimerExpired += HandleTimerExpired;

        SetState(GameState.Idle);
        StartGame();

        // Temporary Subscribe to state changes for debugging
        OnGameStateChanged += state => Debug.Log($"[GameManager] State: {state}");
    }

    private void OnDestroy()
    {
        busManager.OnBusDepart -= HandleBusDepart;
        waitingAreaManager.OnWaitingAreaFull -= HandleWaitingAreaFull;
        timerManager.OnTimerExpired -= HandleTimerExpired;
    }

    public void StartGame()
    {
        if (currentState != GameState.Idle)
            return;

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
        InputManager.Instance.SetInputEnabled(false);
        timerManager.StopTimer();
        LevelManager.Instance.OnLevelWin();
        SetState(GameState.Win);
    }

    private void TriggerFail(GameResult failReason)
    {
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