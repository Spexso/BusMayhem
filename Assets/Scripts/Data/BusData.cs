using UnityEngine;

/// <summary>
/// Represents the data associated with a bus, its color and seating capacity.
/// </summary>
[System.Serializable]
public class BusData
{
    // Fields
    [SerializeField] private int capacity;
    [SerializeField] private StickmanColor color;

    // Methods
    public int Capacity => capacity;
    public StickmanColor Color => color;

    public BusData(StickmanColor color, int capacity)
    {
        this.color = color;
        this.capacity = capacity;
    }
}
