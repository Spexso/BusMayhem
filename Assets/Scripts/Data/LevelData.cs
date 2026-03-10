using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusMayhem/LevelData")]
public class LevelData : ScriptableObject
{
    // Fields
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 7;

    [Header("Stickmen")]
    [SerializeField] private ColoredCell[] cells;

    [Header("Bus Sequence")]
    [SerializeField] private StickmanColor[] busSequence;

    [Header("Timer")]
    [SerializeField] private float timerDuration = 60f;

    // Properties
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public ColoredCell[] Cells => cells;
    public StickmanColor[] BusSequence => busSequence;
    public float TimerDuration => timerDuration;
}