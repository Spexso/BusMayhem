using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides a custom Unity Editor window for creating, editing, and managing levels. Enables
/// users to configure grid layouts, paint cells, set bus sequences, and validate level data through an interactive
/// graphical interface.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    // Fields - window layout

    private const float LeftPanelWidth = 180f;
    private const float RightPanelWidth = 340f;
    private const float CellDrawSize = 40f;
    private const float PaletteButtonSize = 32f;
    private const string LevelsPath = "Assets/Levels";

    // Fields - panel scroll positions

    private Vector2 leftPanelScroll;
    private Vector2 centerPanelScroll;
    private Vector2 rightPanelScroll;

    // Fields - level list

    private List<LevelData> levelAssets = new List<LevelData>();
    private int selectedLevelIndex = -1;

    // Fields - working copy

    private LevelData currentAsset;
    private string currentAssetPath;
    private int workingGridWidth = 5;
    private int workingGridHeight = 7;
    private float workingTimer = 60f;
    private int workingWaitingAreaSize;
    private List<ColoredCell> workingCells = new List<ColoredCell>();
    private List<BusData> workingBusSequence = new List<BusData>();

    // Fields - painting

    private StickmanColor selectedPaintColor = StickmanColor.Red;
    private bool isDirty = false;

    // Fields - validation

    private List<ValidationResult> validationResults = new List<ValidationResult>();

    // Methods - window lifecycle

    [MenuItem("BusMayhem/Level Editor")]
    public static void OpenWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(800f, 500f);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshLevelList();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        DrawLeftPanel();
        DrawCenterPanel();
        DrawRightPanel();

        EditorGUILayout.EndHorizontal();
    }

    // Methods - panels
    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
        EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        leftPanelScroll = EditorGUILayout.BeginScrollView(leftPanelScroll);

        for (int i = 0; i < levelAssets.Count; i++)
        {
            if (levelAssets[i] == null)
                continue;

            bool isSelected = i == selectedLevelIndex;
            GUIStyle buttonStyle = isSelected ? GetSelectedButtonStyle() : GUI.skin.button;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(levelAssets[i].name, buttonStyle, GUILayout.Height(28)))
            {
                if (selectedLevelIndex != i)
                {
                    TrySaveCurrentAsset();
                    LoadLevel(i);
                }
            }

            if (GUILayout.Button("x", GUILayout.Width(24), GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete Level",
                    $"Are you sure you want to delete '{levelAssets[i].name}'? This action cannot be undone.",
                    "Delete",
                    "Cancel"))
                {
                    EditorGUILayout.EndHorizontal();
                    DeleteLevel(i);
                    break;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(4);

        if (GUILayout.Button("New Level", GUILayout.Height(30)))
            CreateNewLevel();

        EditorGUILayout.EndVertical();
    }

    private void DrawCenterPanel()
    {
        EditorGUILayout.BeginVertical();

        if (currentAsset == null)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select or create a level to begin.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }


        EditorGUILayout.LabelField("Grid Canvas", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        centerPanelScroll = EditorGUILayout.BeginScrollView(centerPanelScroll);

        DrawColorPalette();
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical();
        DrawWaitingAreaPreview();
        EditorGUILayout.Space(6);
        DrawGridCanvas();
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(RightPanelWidth));
        rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

        if (currentAsset == null)
        {
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawGridSizeFields();
        EditorGUILayout.Space(8);
        DrawWaitingAreaField();
        EditorGUILayout.Space(8);
        DrawTimerField();
        EditorGUILayout.Space(8);
        DrawBusSequence();
        EditorGUILayout.Space(8);
        DrawValidationSection();
        EditorGUILayout.Space(8);
        DrawSaveButton();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // Methods - grid canvas

    private void DrawColorPalette()
    {
        EditorGUILayout.LabelField("Paint Color", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        foreach (StickmanColor color in System.Enum.GetValues(typeof(StickmanColor)))
        {
            bool isSelected = color == selectedPaintColor;
            GUIStyle style = new GUIStyle(GUI.skin.button);

            if (isSelected)
                style.normal.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.4f));

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = GetColorForStickmanColor(color);

            string label = color == StickmanColor.None ? "Path" : color.ToString();

            if (GUILayout.Button(label, style, GUILayout.Width(PaletteButtonSize * 2f), GUILayout.Height(PaletteButtonSize)))
                selectedPaintColor = color;

            GUI.backgroundColor = prev;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGridCanvas()
    {
        float totalWidth = workingGridWidth * CellDrawSize;
        float totalHeight = workingGridHeight * CellDrawSize;

        Rect canvasRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

        for (int y = 0; y < workingGridHeight; y++)
        {
            for (int x = 0; x < workingGridWidth; x++)
            {
                Rect cellRect = new Rect(
                    canvasRect.x + x * CellDrawSize,
                    canvasRect.y + y * CellDrawSize,
                    CellDrawSize - 1f,
                    CellDrawSize - 1f);

                ColoredCell? existingCell = FindCell(x, y);

                Color cellColor;
                if (!existingCell.HasValue)
                    cellColor = new Color(0.15f, 0.15f, 0.15f);
                else if (existingCell.Value.color == StickmanColor.None)
                    cellColor = new Color(0.45f, 0.45f, 0.45f);
                else
                    cellColor = GetColorForStickmanColor(existingCell.Value.color);

                EditorGUI.DrawRect(cellRect, cellColor);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), Color.black);

                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                        PaintCell(x, y);
                    else if (Event.current.button == 1)
                        EraseCell(x, y);

                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    // Methods - right panel sections

    private void DrawGridSizeFields()
    {
        EditorGUILayout.LabelField("Grid Size", EditorStyles.miniBoldLabel);

        int newWidth = EditorGUILayout.IntField("Width", workingGridWidth);
        int newHeight = EditorGUILayout.IntField("Height", workingGridHeight);

        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        if (newWidth != workingGridWidth || newHeight != workingGridHeight)
        {
            workingGridWidth = newWidth;
            workingGridHeight = newHeight;
            ClampCellsToGrid();
            isDirty = true;
        }
    }

    private void DrawWaitingAreaPreview()
    {
        float totalWidth = workingWaitingAreaSize * CellDrawSize;
        float gridTotalWidth = workingGridWidth * CellDrawSize;
        float offsetX = (gridTotalWidth - totalWidth) * 0.5f;

        Rect labelRect = GUILayoutUtility.GetRect(gridTotalWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(new Rect(labelRect.x + offsetX, labelRect.y, totalWidth, labelRect.height), "Waiting Area", EditorStyles.miniBoldLabel);

        Rect areaRect = GUILayoutUtility.GetRect(gridTotalWidth, CellDrawSize);

        for (int i = 0; i < workingWaitingAreaSize; i++)
        {
            Rect slotRect = new Rect(
                areaRect.x + offsetX + i * CellDrawSize,
                areaRect.y,
                CellDrawSize - 1f,
                CellDrawSize - 1f);

            EditorGUI.DrawRect(slotRect, new Color(0.35f, 0.35f, 0.35f));
            EditorGUI.DrawRect(new Rect(slotRect.x, slotRect.y, slotRect.width, 1f), Color.white);
            EditorGUI.DrawRect(new Rect(slotRect.x, slotRect.y, 1f, slotRect.height), Color.white);
            EditorGUI.DrawRect(new Rect(slotRect.x + slotRect.width, slotRect.y, 1f, slotRect.height), Color.white);
            EditorGUI.DrawRect(new Rect(slotRect.x, slotRect.y + slotRect.height, slotRect.width, 1f), Color.white);
        }
    }

    private void DrawWaitingAreaField()
    {
        EditorGUILayout.LabelField("Waiting Area", EditorStyles.miniBoldLabel);

        int newSize = EditorGUILayout.IntField("Slot Count", workingWaitingAreaSize);
        newSize = Mathf.Max(1, newSize);

        if (newSize != workingWaitingAreaSize)
        {
            workingWaitingAreaSize = newSize;
            isDirty = true;
        }
    }

    private void DrawTimerField()
    {
        EditorGUILayout.LabelField("Timer", EditorStyles.miniBoldLabel);

        float newTimer = EditorGUILayout.FloatField("Duration (s)", workingTimer);
        newTimer = Mathf.Max(1f, newTimer);

        if (!Mathf.Approximately(newTimer, workingTimer))
        {
            workingTimer = newTimer;
            isDirty = true;
        }
    }

    private void DrawBusSequence()
    {
        EditorGUILayout.LabelField("Bus Sequence", EditorStyles.miniBoldLabel);

        for (int i = 0; i < workingBusSequence.Count; i++)
        {
            BusData bus = workingBusSequence[i];
            if (bus == null)
                continue;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Bus {i + 1}", GUILayout.Width(40));

            StickmanColor newColor = (StickmanColor)EditorGUILayout.EnumPopup(bus.Color, GUILayout.Width(80));
            int newCapacity = EditorGUILayout.IntField(bus.Capacity, GUILayout.Width(40));
            newCapacity = Mathf.Max(1, newCapacity);

            if (newColor != bus.Color || newCapacity != bus.Capacity)
            {
                SetBusData(bus, newColor, newCapacity);
                isDirty = true;
            }

            GUI.enabled = i > 0;
            if (GUILayout.Button("up", GUILayout.Width(28)))
            {
                workingBusSequence.RemoveAt(i);
                workingBusSequence.Insert(i - 1, bus);
                isDirty = true;
            }
            GUI.enabled = i < workingBusSequence.Count - 1;
            if (GUILayout.Button("dn", GUILayout.Width(28)))
            {
                workingBusSequence.RemoveAt(i);
                workingBusSequence.Insert(i + 1, bus);
                isDirty = true;
            }
            GUI.enabled = true;

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                workingBusSequence.RemoveAt(i);
                isDirty = true;
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Add Bus"))
        {
            BusData newBus = CreateBusData(StickmanColor.Red, 3);
            workingBusSequence.Add(newBus);
            isDirty = true;
        }
    }

    private void DrawValidationSection()
    {
        EditorGUILayout.LabelField("Validation", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("Validate", GUILayout.Height(24)))
            RunValidation();

        if (validationResults.Count == 0)
            return;

        EditorGUILayout.Space(4);

        foreach (var result in validationResults)
        {
            MessageType msgType = result.Severity == ValidationSeverity.Error
                ? MessageType.Error
                : MessageType.Warning;

            EditorGUILayout.HelpBox(result.Message, msgType);
        }
    }

    private void DrawSaveButton()
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = isDirty ? new Color(0.6f, 1f, 0.6f) : Color.white;

        if (GUILayout.Button("Save Level", GUILayout.Height(32)))
            TrySaveCurrentAsset();

        GUI.backgroundColor = prev;
    }

    // Methods - level management

    private void RefreshLevelList()
    {
        levelAssets.Clear();

        if (!Directory.Exists(LevelsPath))
            Directory.CreateDirectory(LevelsPath);

        string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset != null)
                levelAssets.Add(asset);
        }

        levelAssets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
    }

    private void LoadLevel(int index)
    {
        if (index < 0 || index >= levelAssets.Count)
            return;

        selectedLevelIndex = index;
        currentAsset = levelAssets[index];
        currentAssetPath = AssetDatabase.GetAssetPath(currentAsset);

        workingGridWidth = currentAsset.GridWidth;
        workingGridHeight = currentAsset.GridHeight;
        workingTimer = currentAsset.TimerDuration;
        workingWaitingAreaSize = currentAsset.WaitingAreaSize;

        workingCells = new List<ColoredCell>();
        if (currentAsset.Cells != null)
        {
            foreach (var cell in currentAsset.Cells)
                workingCells.Add(cell);
        }

        workingBusSequence = new List<BusData>();
        if (currentAsset.BusSequence != null)
        {
            foreach (var bus in currentAsset.BusSequence)
            {
                if (bus != null)
                    workingBusSequence.Add(CreateBusData(bus.Color, bus.Capacity));
            }
        }

        validationResults.Clear();
        isDirty = false;
    }

    private void CreateNewLevel()
    {
        if (!Directory.Exists(LevelsPath))
            AssetDatabase.CreateFolder("Assets", "Levels");

        int levelNumber = levelAssets.Count + 1;
        string assetName = $"Level{levelNumber:D2}";
        string assetPath = $"{LevelsPath}/{assetName}.asset";

        while (File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath)))
        {
            levelNumber++;
            assetName = $"Level{levelNumber:D2}";
            assetPath = $"{LevelsPath}/{assetName}.asset";
        }

        LevelData newAsset = CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        RefreshLevelList();

        int newIndex = levelAssets.FindIndex(l => l.name == assetName);
        if (newIndex >= 0)
            LoadLevel(newIndex);
    }

    private void DeleteLevel(int index)
    {
        if (index < 0 || index >= levelAssets.Count)
            return;

        string path = AssetDatabase.GetAssetPath(levelAssets[index]);

        if (currentAsset == levelAssets[index])
        {
            currentAsset = null;
            currentAssetPath = null;
            selectedLevelIndex = -1;
            workingCells.Clear();
            workingBusSequence.Clear();
            validationResults.Clear();
            isDirty = false;
        }

        AssetDatabase.DeleteAsset(path);
        RefreshLevelList();

        if (selectedLevelIndex >= levelAssets.Count)
            selectedLevelIndex = levelAssets.Count - 1;
    }

    private void TrySaveCurrentAsset()
    {
        if (currentAsset == null)
            return;

        RunValidation();

        if (LevelDataValidator.HasErrors(validationResults))
        {
            EditorUtility.DisplayDialog(
                "Cannot Save Level",
                "The level has errors that must be fixed before saving:\n\n" +
                GetErrorMessages(),
                "OK");
            return;
        }

        WriteWorkingCopyToAsset();

        EditorUtility.SetDirty(currentAsset);
        AssetDatabase.SaveAssets();

        isDirty = false;
    }

    private string GetErrorMessages()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var result in validationResults)
        {
            if (result.Severity == ValidationSeverity.Error)
                sb.AppendLine($"- {result.Message}");
        }
        return sb.ToString();
    }

    private void WriteWorkingCopyToAsset()
    {
        SerializedObject serialized = new SerializedObject(currentAsset);

        serialized.FindProperty("gridWidth").intValue = workingGridWidth;
        serialized.FindProperty("gridHeight").intValue = workingGridHeight;
        serialized.FindProperty("timerDuration").floatValue = workingTimer;
        serialized.FindProperty("waitingAreaSize").intValue = workingWaitingAreaSize;

        SerializedProperty cellsProp = serialized.FindProperty("cells");
        cellsProp.arraySize = workingCells.Count;
        for (int i = 0; i < workingCells.Count; i++)
        {
            SerializedProperty element = cellsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("gridX").intValue = workingCells[i].gridX;
            element.FindPropertyRelative("gridY").intValue = workingCells[i].gridY;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingCells[i].color;
        }

        SerializedProperty busProp = serialized.FindProperty("busSequence");
        busProp.arraySize = workingBusSequence.Count;
        for (int i = 0; i < workingBusSequence.Count; i++)
        {
            SerializedProperty element = busProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("capacity").intValue = workingBusSequence[i].Capacity;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingBusSequence[i].Color;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Methods - painting

    private void PaintCell(int x, int y)
    {
        for (int i = 0; i < workingCells.Count; i++)
        {
            if (workingCells[i].gridX == x && workingCells[i].gridY == y)
            {
                ColoredCell updated = workingCells[i];
                updated.color = selectedPaintColor;
                workingCells[i] = updated;
                isDirty = true;
                return;
            }
        }

        workingCells.Add(new ColoredCell { gridX = x, gridY = y, color = selectedPaintColor });
        isDirty = true;
    }

    private void EraseCell(int x, int y)
    {
        for (int i = 0; i < workingCells.Count; i++)
        {
            if (workingCells[i].gridX == x && workingCells[i].gridY == y)
            {
                workingCells.RemoveAt(i);
                isDirty = true;
                return;
            }
        }
    }

    private ColoredCell? FindCell(int x, int y)
    {
        foreach (var cell in workingCells)
        {
            if (cell.gridX == x && cell.gridY == y)
                return cell;
        }
        return null;
    }

    private void ClampCellsToGrid()
    {
        workingCells.RemoveAll(c => c.gridX >= workingGridWidth || c.gridY >= workingGridHeight);
    }

    // Methods - validation

    private void RunValidation()
    {
        LevelData tempAsset = CreateInstance<LevelData>();
        SerializedObject serialized = new SerializedObject(tempAsset);

        serialized.FindProperty("gridWidth").intValue = workingGridWidth;
        serialized.FindProperty("gridHeight").intValue = workingGridHeight;
        serialized.FindProperty("timerDuration").floatValue = workingTimer;
        serialized.FindProperty("waitingAreaSize").intValue = workingWaitingAreaSize;

        SerializedProperty cellsProp = serialized.FindProperty("cells");
        cellsProp.arraySize = workingCells.Count;
        for (int i = 0; i < workingCells.Count; i++)
        {
            SerializedProperty element = cellsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("gridX").intValue = workingCells[i].gridX;
            element.FindPropertyRelative("gridY").intValue = workingCells[i].gridY;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingCells[i].color;
        }

        SerializedProperty busProp = serialized.FindProperty("busSequence");
        busProp.arraySize = workingBusSequence.Count;
        for (int i = 0; i < workingBusSequence.Count; i++)
        {
            SerializedProperty element = busProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("capacity").intValue = workingBusSequence[i].Capacity;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingBusSequence[i].Color;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        validationResults = LevelDataValidator.Validate(tempAsset);
        DestroyImmediate(tempAsset);
    }

    // Methods - helpers

    private GUIStyle GetSelectedButtonStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = MakeTex(2, 2, new Color(0.3f, 0.6f, 1f, 0.6f));
        return style;
    }

    private Color GetColorForStickmanColor(StickmanColor color)
    {
        switch (color)
        {
            case StickmanColor.None: return new Color(0.45f, 0.45f, 0.45f);
            case StickmanColor.Red: return new Color(0.9f, 0.2f, 0.2f);
            case StickmanColor.Blue: return new Color(0.2f, 0.4f, 0.9f);
            case StickmanColor.Green: return new Color(0.2f, 0.8f, 0.3f);
            case StickmanColor.Yellow: return new Color(0.95f, 0.85f, 0.1f);
            case StickmanColor.Purple: return new Color(0.6f, 0.2f, 0.8f);
            case StickmanColor.Orange: return new Color(0.95f, 0.5f, 0.1f);
            case StickmanColor.Pink: return new Color(0.95f, 0.4f, 0.7f);
            case StickmanColor.Cyan: return new Color(0.1f, 0.85f, 0.9f);
            default: return Color.grey;
        }
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private BusData CreateBusData(StickmanColor color, int capacity)
    {
        return new BusData(color, capacity);
    }

    private void SetBusData(BusData bus, StickmanColor color, int capacity)
    {
        workingBusSequence[workingBusSequence.IndexOf(bus)] = new BusData(color, capacity);
        isDirty = true;
    }
}