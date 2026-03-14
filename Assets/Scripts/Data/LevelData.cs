using UnityEngine;

/// <summary>
/// Represents the configuration data for a game level, including grid dimensions, timer settings, and bus and passenger.
/// arrangements.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "BusMayhem/LevelData")]
public class LevelData : ScriptableObject
{
    // Fields
    [Header("General Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 7;
    [SerializeField] private float timerDuration = 60f;
    [SerializeField] private int waitingAreaSize = 3;

    [Header("Passengers")]
    [SerializeField] private ColoredCell[] cells;

    [Header("Buses")]
    [SerializeField] private BusData[] busSequence;

    // Methods
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public ColoredCell[] Cells => cells;
    public BusData[] BusSequence => busSequence;
    public float TimerDuration => timerDuration;
    public int WaitingAreaSize => waitingAreaSize;
}