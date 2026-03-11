using System;
using UnityEngine;

public class BusController : MonoBehaviour
{
    // Fields
    [SerializeField] private Renderer busRenderer;
    private StickmanColor color;
    private int capacity;
    private int boardedCount;
    public event Action<BusController> onBusFull;

    // Methods
    public StickmanColor BusColor => color;
    public int BoardedCount => boardedCount;
    public int Capacity => capacity;
    public bool IsFull => boardedCount >= capacity;

    public void Initialize(BusData data)
    {
        color = data.Color;
        capacity = data.Capacity;
        boardedCount = 0;

        if (busRenderer != null)
        {
            busRenderer.material.color = ColorConverter.GetColor(color);
        }
    }

    public bool TryBoardPassenger()
    {
        if (IsFull)
            return false;
        else if (boardedCount + 1 >= capacity)
            onBusFull?.Invoke(this);

        boardedCount++;
        return true;
    }
}
