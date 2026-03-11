using UnityEngine;

[System.Serializable]
public class BusData
{
    // Fields
    [SerializeField] private int capacity;
    [SerializeField] private StickmanColor color;

    // Methods
    public int Capacity => capacity;
    public StickmanColor Color => color;
}
