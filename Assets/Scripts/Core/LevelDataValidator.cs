using System.Collections.Generic;

public enum ValidationSeverity
{
    Warning,
    Error
}

public class ValidationResult
{
    public ValidationSeverity Severity;
    public string Message;

    public ValidationResult(ValidationSeverity severity, string message)
    {
        Severity = severity;
        Message = message;
    }
}

public static class LevelDataValidator
{
    // Methods
    public static List<ValidationResult> Validate(LevelData levelData)
    {
        var results = new List<ValidationResult>();

        if (levelData == null)
        {
            results.Add(new ValidationResult(ValidationSeverity.Error, "LevelData is null."));
            return results;
        }

        ValidateGridSize(levelData, results);
        ValidateCellBounds(levelData, results);
        ValidateDuplicateCells(levelData, results);
        ValidateTimer(levelData, results);
        ValidateBusSequence(levelData, results);
        ValidateHouses(levelData, results);
        ValidateColorBalance(levelData, results);
        ValidateExitRow(levelData, results);
        ValidateAllCellsCanReachExit(levelData, results);

        return results;
    }

    public static bool HasErrors(List<ValidationResult> results)
    {
        foreach (var result in results)
        {
            if (result.Severity == ValidationSeverity.Error)
                return true;
        }
        return false;
    }

    private static void ValidateGridSize(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.GridWidth <= 0)
            results.Add(new ValidationResult(ValidationSeverity.Error, "Grid width must be greater than zero."));

        if (levelData.GridHeight <= 0)
            results.Add(new ValidationResult(ValidationSeverity.Error, "Grid height must be greater than zero."));
    }

    private static void ValidateCellBounds(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Cells == null)
            return;

        for (int i = 0; i < levelData.Cells.Length; i++)
        {
            var cell = levelData.Cells[i];

            if (cell.gridX < 0 || cell.gridX >= levelData.GridWidth)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Cell at index {i} has gridX {cell.gridX} which is out of bounds (valid range: 0 to {levelData.GridWidth - 1})."));
            }

            if (cell.gridY < 0 || cell.gridY >= levelData.GridHeight)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Cell at index {i} has gridY {cell.gridY} which is out of bounds (valid range: 0 to {levelData.GridHeight - 1})."));
            }
        }
    }

    private static void ValidateDuplicateCells(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Cells == null)
            return;

        var seen = new HashSet<string>();

        foreach (var cell in levelData.Cells)
        {
            string key = $"{cell.gridX},{cell.gridY}";

            if (!seen.Add(key))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Duplicate stickman at gridX {cell.gridX}, gridY {cell.gridY}."));
            }
        }
    }

    private static void ValidateTimer(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.TimerDuration <= 0f)
        {
            results.Add(new ValidationResult(
                ValidationSeverity.Error,
                $"Timer duration must be greater than zero (current value: {levelData.TimerDuration})."));
        }
    }

    private static void ValidateBusSequence(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.BusSequence == null || levelData.BusSequence.Length == 0)
        {
            results.Add(new ValidationResult(
                ValidationSeverity.Error,
                "Bus sequence is empty. At least one bus must be defined."));
            return;
        }

        for (int i = 0; i < levelData.BusSequence.Length; i++)
        {
            var bus = levelData.BusSequence[i];

            if (bus == null)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Bus at index {i} is null."));
                continue;
            }

            if (bus.Capacity <= 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Bus at index {i} (color: {bus.Color}) has capacity {bus.Capacity}. Capacity must be greater than zero."));
            }
        }
    }

    private static void ValidateHouses(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Houses == null || levelData.Houses.Length == 0)
            return;

        var occupiedByCell = new HashSet<string>();
        if (levelData.Cells != null)
        {
            foreach (var cell in levelData.Cells)
                occupiedByCell.Add($"{cell.gridX},{cell.gridY}");
        }

        var seenHousePositions = new HashSet<string>();

        for (int i = 0; i < levelData.Houses.Length; i++)
        {
            var house = levelData.Houses[i];

            if (house == null)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"House at index {i} is null."));
                continue;
            }

            if (house.GridX < 0 || house.GridX >= levelData.GridWidth ||
                house.GridY < 0 || house.GridY >= levelData.GridHeight)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"House at index {i} position ({house.GridX},{house.GridY}) is out of grid bounds."));
            }

            string posKey = $"{house.GridX},{house.GridY}";

            if (!seenHousePositions.Add(posKey))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Duplicate house position at ({house.GridX},{house.GridY})."));
            }

            if (occupiedByCell.Contains(posKey))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"House at ({house.GridX},{house.GridY}) overlaps with an existing cell."));
            }

            if (house.StickmanQueue == null || house.StickmanQueue.Length == 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Warning,
                    $"House at ({house.GridX},{house.GridY}) has an empty stickman queue."));
            }
        }
    }

    private static void ValidateExitRow(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Cells == null || levelData.Cells.Length == 0)
            return;

        foreach (var cell in levelData.Cells)
        {
            if (cell.gridY == 0)
                return;
        }

        results.Add(new ValidationResult(
            ValidationSeverity.Error,
            "No cell exists at row 0. At least one cell must be in the top row so stickmen can exit."));
    }

    private static void ValidateColorBalance(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Cells == null || levelData.BusSequence == null)
            return;

        var stickmanCounts = new Dictionary<StickmanColor, int>();

        foreach (var cell in levelData.Cells)
        {
            if (cell.color == StickmanColor.None)
                continue;

            if (!stickmanCounts.ContainsKey(cell.color))
                stickmanCounts[cell.color] = 0;
            stickmanCounts[cell.color]++;
        }

        if (levelData.Houses != null)
        {
            foreach (var house in levelData.Houses)
            {
                if (house?.StickmanQueue == null)
                    continue;

                foreach (StickmanColor color in house.StickmanQueue)
                {
                    if (color == StickmanColor.None)
                        continue;

                    if (!stickmanCounts.ContainsKey(color))
                        stickmanCounts[color] = 0;
                    stickmanCounts[color]++;
                }
            }
        }

        var busCapacityPerColor = new Dictionary<StickmanColor, int>();

        foreach (var bus in levelData.BusSequence)
        {
            if (bus == null)
                continue;

            if (!busCapacityPerColor.ContainsKey(bus.Color))
                busCapacityPerColor[bus.Color] = 0;
            busCapacityPerColor[bus.Color] += bus.Capacity;
        }

        foreach (var kvp in stickmanCounts)
        {
            StickmanColor color = kvp.Key;
            int stickmanCount = kvp.Value;

            if (!busCapacityPerColor.ContainsKey(color))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Color {color} has {stickmanCount} stickman(s) (grid + houses) but no bus of that color exists in the sequence."));
                continue;
            }

            int totalCapacity = busCapacityPerColor[color];

            if (stickmanCount != totalCapacity)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Warning,
                    $"Color {color}: {stickmanCount} stickman(s) total (grid + houses) but total bus capacity is {totalCapacity}. They may not all board cleanly."));
            }
        }

        foreach (var kvp in busCapacityPerColor)
        {
            StickmanColor color = kvp.Key;

            if (!stickmanCounts.ContainsKey(color))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Warning,
                    $"Bus color {color} is in the sequence but has no stickmen on the grid or in any house. That bus will never fill."));
            }
        }
    }

    private static void ValidateAllCellsCanReachExit(LevelData levelData, List<ValidationResult> results)
    {
        if (levelData.Cells == null || levelData.Cells.Length == 0)
            return;

        int width = levelData.GridWidth;
        int height = levelData.GridHeight;

        bool[,] painted = new bool[width, height];

        foreach (var cell in levelData.Cells)
        {
            if (cell.gridX >= 0 && cell.gridX < width &&
                cell.gridY >= 0 && cell.gridY < height)
                painted[cell.gridX, cell.gridY] = true;
        }

        if (levelData.Houses != null)
        {
            foreach (var house in levelData.Houses)
            {
                if (house == null)
                    continue;

                if (house.GridX >= 0 && house.GridX < width &&
                    house.GridY >= 0 && house.GridY < height)
                    painted[house.GridX, house.GridY] = true;
            }
        }

        foreach (var cell in levelData.Cells)
        {
            if (cell.gridX < 0 || cell.gridX >= width ||
                cell.gridY < 0 || cell.gridY >= height)
                continue;

            if (!PathFinder.BFSLevelEditor(painted, cell.gridX, cell.gridY))
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    $"Cell at ({cell.gridX}, {cell.gridY}) has no valid path to the exit row (y=0)."));
            }
        }
    }
}