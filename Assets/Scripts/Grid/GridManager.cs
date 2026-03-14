using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the creation, organization, and interaction of grid cells and stickman entities within the game scene.
/// Provides methods to initialize the grid, move stickmen, and update their visual highlights.
/// </summary>
public class GridManager : MonoBehaviour
{
    // Fields
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject stickmanPrefab;
    [SerializeField] private GameObject borderTilePrefab;
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private float cellSize = 1f;

    private int gridWidth;
    private int gridHeight;
    private GridCell[,] cells;
    private List<StickmanController> stickmans = new List<StickmanController>();

    // Properties
    public int StickmanCount => stickmans.Count;
    public static GridManager Instance { get; private set; }

    // Methods
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeGrid(LevelData data)
    {
        gridWidth = data.GridWidth;
        gridHeight = data.GridHeight;
        cells = new GridCell[gridWidth, gridHeight];

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        CalculateXMinMaxDimensions(ref minX, ref maxX, data);
        float offsetX = (minX + maxX) * cellSize * 0.5f;

        SpawnCells(data, offsetX);
        SpawnHouses(data, offsetX);
        SpawnBorderTiles(data, offsetX);
        RefreshHighlights();
    }

    public bool HasAnyStickmen()
    {
        return stickmans.Count > 0;
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return null;

        return cells[x, y];
    }

    public bool IsColumnClear(int x, int fromY)
    {
        for (int y = fromY; y >= 0; y--)
        {
            if (cells[x, y] == null)
                continue;

            if (cells[x, y].IsOccupied)
                return false;
        }
        return true;
    }

    public void SpawnStickmanAtCell(int x, int y, StickmanColor color)
    {
        if (cells[x, y] == null)
        {
            Debug.LogError($"[GridManager] SpawnStickmanAtCell: cell at ({x},{y}) is null.");
            return;
        }

        if (cells[x, y].IsOccupied)
        {
            Debug.LogError($"[GridManager] SpawnStickmanAtCell: cell at ({x},{y}) is already occupied.");
            return;
        }

        Vector3 spawnPosition = cells[x, y].transform.position;
        GameObject stickmanObj = Instantiate(stickmanPrefab, spawnPosition, Quaternion.identity, this.transform);
        stickmanObj.name = $"Stickman_{x}_{y}";

        StickmanController stickman = stickmanObj.GetComponent<StickmanController>();
        if (stickman == null)
        {
            Debug.LogError($"[GridManager] SpawnStickmanAtCell: StickmanController not found on prefab at ({x},{y}).");
            return;
        }

        stickman.Initialize(x, y, color, false);
        cells[x, y].SetOccupant(stickman);
        stickmans.Add(stickman);

        RefreshHighlights();
    }

    private Vector3[] BuildWorldPath(List<Vector2Int> gridPath, StickmanController stickman)
    {
        List<Vector3> worldPath = new List<Vector3>();

        foreach (Vector2Int gridPos in gridPath)
            worldPath.Add(cells[gridPos.x, gridPos.y].transform.position);

        int exitX = gridPath.Count > 0 ? gridPath[gridPath.Count - 1].x : stickman.GridX;
        Vector3 exitCellPos = cells[exitX, 0].transform.position;
        Vector3 exitPos = exitCellPos + Vector3.forward * cellSize;
        worldPath.Add(exitPos);

        return worldPath.ToArray();
    }

    public void MoveStickman(StickmanController stickman)
    {
        if (stickman == null)
            return;

        if (stickman.IsMoving)
        {
            Debug.LogWarning($"[GridManager] Attempted to move stickman that is already moving: {stickman.name}");
            return;
        }

        List<Vector2Int> gridPath = PathFinder.BFS(cells, stickman.GridX, stickman.GridY);

        if (gridPath == null)
        {
            stickman.PlayBlockedFeedback();
            return;
        }

        cells[stickman.GridX, stickman.GridY].ClearOccupant();
        stickmans.Remove(stickman);

        Vector3[] worldPath = BuildWorldPath(gridPath, stickman);
        stickman.MoveToExit(worldPath, () => OnStickmanReachedExit(stickman));

        RefreshHighlights();
    }

    public void RefreshHighlights()
    {
        foreach (StickmanController stickman in stickmans)
        {
            if (stickman.IsMoving)
            {
                stickman.SetDimmed();
                continue;
            }

            List<Vector2Int> path = PathFinder.BFS(cells, stickman.GridX, stickman.GridY);
            bool hasPath = path != null;

            if (stickman.IsHidden)
            {
                if (hasPath)
                    stickman.Initialize(stickman.GridX, stickman.GridY, stickman.CColor, false);

                continue;
            }
            else
            {
                stickman.SetHiddenIcon(false);
            }

            if (hasPath)
                stickman.SetHighlighted();
            else
                stickman.SetDimmed();
        }
    }

    private void SpawnCells(LevelData data, float offsetX)
    {
        foreach (ColoredCell coloredCell in data.Cells)
        {
            Vector3 localPosition = new Vector3(coloredCell.gridX * cellSize - offsetX, 0f, -(coloredCell.gridY * cellSize));
            GameObject cellObj = Instantiate(cellPrefab, Vector3.zero, cellPrefab.transform.rotation, this.transform);
            cellObj.transform.localPosition = localPosition;
            cellObj.name = $"Cell_{coloredCell.gridX}_{coloredCell.gridY}";

            GridCell cell = cellObj.GetComponent<GridCell>();
            if (cell == null)
            {
                Debug.LogError($"[GridManager] GridCell component not found on prefab: {cellObj.name}");
                return;
            }

            cell.Initialize(coloredCell.gridX, coloredCell.gridY);
            cells[coloredCell.gridX, coloredCell.gridY] = cell;

            if (coloredCell.color == StickmanColor.None)
                continue;

            Vector3 stickmanPosition = cellObj.transform.position;
            GameObject stickmanObj = Instantiate(stickmanPrefab, stickmanPosition, Quaternion.identity, this.transform);
            stickmanObj.name = $"Stickman_{coloredCell.gridX}_{coloredCell.gridY}";

            StickmanController stickman = stickmanObj.GetComponent<StickmanController>();
            if (stickman == null)
            {
                Debug.LogError($"[GridManager] StickmanController not found on prefab: {stickmanObj.name}");
                return;
            }

            stickman.Initialize(coloredCell.gridX, coloredCell.gridY, coloredCell.color, coloredCell.isHidden);
            cell.SetOccupant(stickman);
            stickmans.Add(stickman);
        }
    }

    private void SpawnHouses(LevelData data, float offsetX)
    {
        if (housePrefab == null)
        {
            Debug.LogError("[GridManager] housePrefab is not assigned.");
            return;
        }

        if (data.Houses == null || data.Houses.Length == 0)
            return;

        foreach (HouseData houseData in data.Houses)
        {
            int x = houseData.GridX;
            int y = houseData.GridY;

            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                Debug.LogError($"[GridManager] House at ({x},{y}) is out of grid bounds.");
                continue;
            }

            if (cells[x, y] == null)
            {
                Vector3 localPosition = new Vector3(x * cellSize - offsetX, 0f, -(y * cellSize));
                GameObject cellObj = Instantiate(cellPrefab, Vector3.zero, cellPrefab.transform.rotation, this.transform);
                cellObj.transform.localPosition = localPosition;
                cellObj.name = $"Cell_{x}_{y}";

                GridCell cell = cellObj.GetComponent<GridCell>();
                if (cell == null)
                {
                    Debug.LogError($"[GridManager] GridCell component not found on house cell prefab at ({x},{y}).");
                    continue;
                }

                cell.Initialize(x, y);
                cells[x, y] = cell;
            }

            Vector3 housePosition = cells[x, y].transform.position;
            GameObject houseObj = Instantiate(housePrefab, housePosition, housePrefab.transform.rotation, this.transform);
            houseObj.name = $"House_{x}_{y}";

            HouseController houseController = houseObj.GetComponent<HouseController>();
            if (houseController == null)
            {
                Debug.LogError($"[GridManager] HouseController not found on housePrefab at ({x},{y}).");
                continue;
            }

            houseController.Initialize(x, y, houseData.StickmanQueue);
            cells[x, y].SetHouse(houseController);
        }
    }

    private void SpawnBorderTiles(LevelData data, float offsetX)
    {
        if (borderTilePrefab == null)
        {
            Debug.LogError("[GridManager] borderTilePrefab is not assigned.");
            return;
        }

        HashSet<Vector2Int> paintedPositions = new HashSet<Vector2Int>();
        foreach (ColoredCell coloredCell in data.Cells)
            paintedPositions.Add(new Vector2Int(coloredCell.gridX, coloredCell.gridY));

        if (data.Houses != null)
        {
            foreach (HouseData houseData in data.Houses)
                paintedPositions.Add(new Vector2Int(houseData.GridX, houseData.GridY));
        }

        HashSet<Vector2> spawnedMidpoints = new HashSet<Vector2>();

        int[] dirX = { 1, 0, -1, 0, 1, -1, -1, 1 };
        int[] dirY = { 0, 1, 0, -1, 1, 1, -1, -1 };

        foreach (Vector2Int painted in paintedPositions)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2Int neighbor = new Vector2Int(painted.x + dirX[i], painted.y + dirY[i]);

                if (!paintedPositions.Contains(neighbor))
                    continue;

                Vector2 midpoint = new Vector2(painted.x + dirX[i] * 0.5f, painted.y + dirY[i] * 0.5f);

                if (spawnedMidpoints.Contains(midpoint))
                    continue;

                spawnedMidpoints.Add(midpoint);

                Vector3 localPosition = new Vector3(midpoint.x * cellSize - offsetX, 0f, -(midpoint.y * cellSize));
                GameObject borderObj = Instantiate(borderTilePrefab, Vector3.zero, borderTilePrefab.transform.rotation, this.transform);
                borderObj.transform.localPosition = localPosition;
                borderObj.name = $"Border_{painted.x}_{painted.y}_to_{neighbor.x}_{neighbor.y}";
            }
        }
    }

    private void CalculateXMinMaxDimensions(ref int minX, ref int maxX, LevelData data)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;

        foreach (ColoredCell coloredCell in data.Cells)
        {
            if (coloredCell.gridX < minX) minX = coloredCell.gridX;
            if (coloredCell.gridX > maxX) maxX = coloredCell.gridX;
        }

        if (data.Houses != null)
        {
            foreach (HouseData houseData in data.Houses)
            {
                if (houseData.GridX < minX) minX = houseData.GridX;
                if (houseData.GridX > maxX) maxX = houseData.GridX;
            }
        }
    }

    private void OnStickmanReachedExit(StickmanController stickman)
    {
        PassengerRouter.Instance?.RoutePassenger(stickman);
    }
}