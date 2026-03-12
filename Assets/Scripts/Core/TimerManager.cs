using System;
using UnityEngine;


public class TimerManager : MonoBehaviour
{
    // Fields
    private float duration;
    private float remaining;
    private bool isRunning;

    public event Action OnTimerExpired;
    public static event Action<float> OnTimerUpdated;

    public static TimerManager Instance { get; private set; }

    public float Remaining => remaining;
    public float Duration => duration;
    public float NormalizedRemaining => duration > 0f ? remaining / duration : 0f;

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

    public void Initialize(LevelData data)
    {
        duration = data.TimerDuration;
        remaining = duration;
        isRunning = false;
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        remaining -= Time.deltaTime;
        OnTimerUpdated?.Invoke(remaining);
        LogManager.TimerLog($"{remaining:F1}s");

        if (remaining <= 0f)
        {
            remaining = 0f;
            isRunning = false;
            OnTimerExpired?.Invoke();
        }
    }
}