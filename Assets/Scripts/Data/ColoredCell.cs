
/// <summary>
/// Represents a cell within a grid that is associated with a specific color.
/// </summary>
[System.Serializable]
public struct ColoredCell
{
    public int gridX;
    public int gridY;
    public StickmanColor color;
    public bool isHidden;
}