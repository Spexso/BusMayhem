using UnityEngine;

[System.Serializable]
public class HouseData
{
    [SerializeField] private int gridX;
    [SerializeField] private int gridY;
    [SerializeField] private StickmanColor[] stickmanQueue;

    public int GridX => gridX;
    public int GridY => gridY;
    public StickmanColor[] StickmanQueue => stickmanQueue;

    public HouseData(int x, int y, StickmanColor[] queue)
    {
        gridX = x;
        gridY = y;
        stickmanQueue = queue;
    }
}