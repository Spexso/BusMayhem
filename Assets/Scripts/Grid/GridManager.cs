using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Fields 
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject stickmanPrefab;
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

    public void InitializeGrid(LevelData Data)
    {
        gridWidth = Data.GridWidth;
        gridHeight = Data.GridHeight;
        cells = new GridCell[gridWidth, gridHeight];

        SpawnCells();
        SpawnStickmans(Data);
        RefreshHighlights();
    }

    public bool IsColumnClear(int x, int fromY)
    {
        for (int y = fromY; y >= 0; y--)
        {
            if (cells[x, y].IsOccupied)
                return false;
        }
        return true;
    }

    private Vector3[] BuildWorldPath(List<Vector2Int> gridPath, StickmanController stickman)
    {
        List<Vector3> worldPath = new List<Vector3>();

        foreach (Vector2Int gridPos in gridPath)
            worldPath.Add(cells[gridPos.x, gridPos.y].transform.position + Vector3.up * 0.5f);

        int exitX = gridPath.Count > 0 ? gridPath[gridPath.Count - 1].x : stickman.GridX;
        Vector3 exitCellPos = cells[exitX, 0].transform.position;
        Vector3 exitPos = exitCellPos + Vector3.forward * cellSize + Vector3.up * 0.5f;
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
            return;

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
            if (path != null)
                stickman.SetHighlighted();
            else
                stickman.SetDimmed();
        }
    }

    private void SpawnCells()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float offsetX = (gridWidth - 1) * cellSize * 0.5f;
                Vector3 localPosition = new Vector3(x * cellSize - offsetX, 0f, -(y * cellSize));
                GameObject CellObj = Instantiate(cellPrefab, Vector3.zero, cellPrefab.transform.rotation, this.transform);
                CellObj.transform.localPosition = localPosition;

                CellObj.name = $"Cell_{x}_{y}";
                GridCell cell = CellObj.GetComponent<GridCell>();
                if (cell == null)
                {
                    Debug.LogError($"[GridManager] GridCell component not found on prefab: {CellObj.name}");
                    return;
                }

                cell.Initialize(x, y);
                cells[x, y] = cell;
            }
        }
    }

    private void SpawnStickmans(LevelData levelData)
    {
        foreach (ColoredCell cell in levelData.Cells)
        {
            Vector3 position = cells[cell.gridX, cell.gridY].transform.position + Vector3.up * 0.5f;
            GameObject obj = Instantiate(stickmanPrefab, position, Quaternion.identity, this.transform);

            obj.name = $"Stickman_{cell.gridX}_{cell.gridY}";
            StickmanController stickman = obj.GetComponent<StickmanController>();
            if (stickman == null)
            {
                Debug.LogError($"[GridManager] StickmanController not found on prefab: {obj.name}");
                return;
            }

            stickman.Initialize(cell.gridX, cell.gridY, cell.color);
            cells[cell.gridX, cell.gridY].SetOccupant(stickman);
            stickmans.Add(stickman);
        }
    }

    private void OnStickmanReachedExit(StickmanController stickman)
    {
        BusManager.Instance.HandleStickmanArrival(stickman);

        // HandlesStickmanArrival->BoardStickman already disables gameObject, so no need to destroy it here
        //Destroy(stickman.gameObject); 
    }
}
