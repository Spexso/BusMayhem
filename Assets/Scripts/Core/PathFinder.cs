using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides static methods for performing pathfinding operations using the Breadth-First Search (BFS) algorithm within
/// a grid-based environment.
/// </summary>
/// <remarks>The PathFinder class is primarily used by the GridManager to determine the shortest path for
/// entities, such as Stickmans, to reach an exit within a grid. It also includes functionality to verify level
/// solvability in level editor scenarios. All methods are static and thread-safe, making the class suitable for use in
/// both runtime and editor contexts.</remarks>
public class PathFinder
{
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    public static List<Vector2Int> BFS(GridCell[,] cells, int startX, int startY)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        bool[] visited = new bool[width * height];
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();

        Vector2Int startCell = new Vector2Int(startX, startY);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        visited[startX * height + startY] = true;

        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Vector2Int CurrentCell = queue.Dequeue();

            // Exit found
            if (CurrentCell.y == 0)
                return ReconstructPath(parent, startCell, CurrentCell);

            // Visit Neighbours of CurrentCell
            foreach (var direction in directions)
            {
                Vector2Int neighbour = CurrentCell + direction;

                if (!IsInBounds(neighbour, width, height))
                    continue;

                if (visited[neighbour.x * height + neighbour.y])
                    continue;

                if (cells[neighbour.x, neighbour.y] == null)
                    continue;

                if (neighbour != startCell && cells[neighbour.x, neighbour.y].IsOccupied)
                    continue;

                visited[neighbour.x * height + neighbour.y] = true;
                parent[neighbour] = CurrentCell;
                queue.Enqueue(neighbour);
            }
        }

        return null;
    }

    // Level Editor version of BFS, used to check if the level is solvable.
    public static bool BFSLevelEditor(bool[,] painted, int startX, int startY)
    {
        int width = painted.GetLength(0);
        int height = painted.GetLength(1);
        bool[] visited = new bool[width * height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited[startX * height + startY] = true;
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

            if (currentCell.y == 0)
                return true;

            foreach (var direction in directions)
            {
                Vector2Int neighbour = currentCell + direction;

                if (!IsInBounds(neighbour, width, height))
                    continue;

                if (visited[neighbour.x * height + neighbour.y])
                    continue;

                if (!painted[neighbour.x, neighbour.y])
                    continue;

                visited[neighbour.x * height + neighbour.y] = true;
                queue.Enqueue(neighbour);
            }
        }

        return false;
    }

    private static bool IsInBounds(Vector2Int pos, int width, int height)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int startCell, Vector2Int endCell)
    {
        // Backtrack from endCell to startCell using the parent dictionary
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = endCell;

        while (current != startCell)
        {
            path.Add(current);
            current = parent[current];
        }

        path.Reverse();
        return path;
    }
}