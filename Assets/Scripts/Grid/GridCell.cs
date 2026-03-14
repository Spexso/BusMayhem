using UnityEngine;

/// <summary>
/// Represents a single cell within a grid that can be occupied by a stickman or a house.
/// </summary>
public class GridCell : MonoBehaviour
{
    // Fields
    private int gridX;
    private int gridY;
    private StickmanController occupant;
    private HouseController house;

    // Properties
    public int GridX => gridX;
    public int GridY => gridY;
    public bool IsOccupied => occupant != null || house != null;
    public StickmanController Occupant => occupant;
    public HouseController House => house;
    public bool IsHouse => house != null;

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

    public void SetHouse(HouseController houseController)
    {
        house = houseController;
    }

    public void ClearHouse()
    {
        house = null;
    }
}