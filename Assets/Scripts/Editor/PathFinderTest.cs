using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides a suite of automated tests for verifying the correctness of the PathFinder pathfinding logic in various
/// grid scenarios.
/// </summary>
public class PathFinderTests
{
    [MenuItem("BusMayhem/Tests/Run PathFinder Tests")]
    public static void RunAll()
    {
        TestClearColumn();
        TestBlockedByStickman();
        TestLateralMovement();
        TestNoPath();
        Debug.Log("All tests completed.");
    }

    private static void TestClearColumn()
    {
        // 3x3 grid, stickman at (1,2), clear column above
        GridCell[,] cells = CreateGrid(3, 3);
        SetOccupied(cells, 1, 2);

        List<Vector2Int> path = PathFinder.BFS(cells, 1, 2);

        if (path != null && path.Count > 0 && path[path.Count - 1].y == 0)
            Debug.Log("[PASS] TestClearColumn");
        else
            Debug.LogError("[FAIL] TestClearColumn");
    }

    private static void TestBlockedByStickman()
    {
        // 3x3 grid, stickman at (1,2), blocker at (1,1), no lateral escape
        GridCell[,] cells = CreateGrid(3, 3);
        SetOccupied(cells, 1, 2);
        SetOccupied(cells, 1, 1);
        SetOccupied(cells, 0, 2);
        SetOccupied(cells, 2, 2);
        SetOccupied(cells, 0, 1);
        SetOccupied(cells, 2, 1);
        SetOccupied(cells, 0, 0);
        SetOccupied(cells, 2, 0);

        List<Vector2Int> path = PathFinder.BFS(cells, 1, 2);

        if (path == null)
            Debug.Log("[PASS] TestBlockedByStickman");
        else
            Debug.LogError("[FAIL] TestBlockedByStickman");
    }

    private static void TestLateralMovement()
    {
        // 3x3 grid, stickman at (0,2), blocker at (0,1), must go right then up
        GridCell[,] cells = CreateGrid(3, 3);
        SetOccupied(cells, 0, 2);
        SetOccupied(cells, 0, 1);

        List<Vector2Int> path = PathFinder.BFS(cells, 0, 2);

        if (path != null && path[path.Count - 1].y == 0)
            Debug.Log("[PASS] TestLateralMovement");
        else
            Debug.LogError("[FAIL] TestLateralMovement");
    }

    private static void TestNoPath()
    {
        // 1x3 grid, stickman at (0,2), fully blocked above
        GridCell[,] cells = CreateGrid(1, 3);
        SetOccupied(cells, 0, 2);
        SetOccupied(cells, 0, 1);
        SetOccupied(cells, 0, 0);

        List<Vector2Int> path = PathFinder.BFS(cells, 0, 2);

        if (path == null)
            Debug.Log("[PASS] TestNoPath");
        else
            Debug.LogError("[FAIL] TestNoPath");
    }

    private static GridCell[,] CreateGrid(int width, int height)
    {
        GridCell[,] cells = new GridCell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells[x, y] = new GameObject($"Cell_{x}_{y}").AddComponent<GridCell>();
        return cells;
    }

    private static void SetOccupied(GridCell[,] cells, int x, int y)
    {
        GameObject dummy = new GameObject($"Stickman_{x}_{y}");
        StickmanController stickman = dummy.AddComponent<StickmanController>();
        cells[x, y].SetOccupant(stickman);
    }
}