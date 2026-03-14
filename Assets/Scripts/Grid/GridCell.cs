using UnityEngine;


/// <summary>
/// Represents a single cell within a grid that can be occupied by a StickmanController.
/// </summary>
public class GridCell : MonoBehaviour
{
    // Fields
    private int gridX;
    private int gridY;
    private StickmanController occupant;

    // Properties
    public int GridX => gridX;
    public int GridY => gridY;
    public bool IsOccupied => occupant != null;
    public StickmanController Occupant => occupant;

    // Methods
    public void Initialize(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public void SetOccupant(StickmanController stickman)
    {
        occupant = stickman;
    }

    public void ClearOccupant()
    {
        occupant = null;
    }
}
