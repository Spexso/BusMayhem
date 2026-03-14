using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
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
    private List<HouseData> workingHouses = new List<HouseData>();

    // Fields - painting

    private enum PaintMode { Color, Hidden, House }
    private PaintMode currentPaintMode = PaintMode.Color;
    private StickmanColor selectedPaintColor = StickmanColor.Red;
    private bool isDirty = false;

    // Fields - house editing

    private int selectedHouseIndex = -1;

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
        DrawHouseSection();
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
        EditorGUILayout.LabelField("Paint Mode", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        foreach (StickmanColor color in System.Enum.GetValues(typeof(StickmanColor)))
        {
            bool isSelected = currentPaintMode == PaintMode.Color && color == selectedPaintColor;
            GUIStyle style = new GUIStyle(GUI.skin.button);

            if (isSelected)
                style.normal.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.4f));

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = GetColorForStickmanColor(color);

            string label = color == StickmanColor.None ? "Path" : color.ToString();

            if (GUILayout.Button(label, style, GUILayout.Width(PaletteButtonSize * 2f), GUILayout.Height(PaletteButtonSize)))
            {
                selectedPaintColor = color;
                currentPaintMode = PaintMode.Color;
                selectedHouseIndex = -1;
            }

            GUI.backgroundColor = prev;
        }

        GUIStyle hiddenStyle = new GUIStyle(GUI.skin.button);
        if (currentPaintMode == PaintMode.Hidden)
            hiddenStyle.normal.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.4f));

        Color prevHidden = GUI.backgroundColor;
        GUI.backgroundColor = Color.black;

        if (GUILayout.Button("Hidden", hiddenStyle, GUILayout.Width(PaletteButtonSize * 2f), GUILayout.Height(PaletteButtonSize)))
        {
            currentPaintMode = PaintMode.Hidden;
            selectedHouseIndex = -1;
        }

        GUI.backgroundColor = prevHidden;

        GUIStyle houseStyle = new GUIStyle(GUI.skin.button);
        if (currentPaintMode == PaintMode.House)
            houseStyle.normal.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.4f));

        Color prevHouse = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.6f, 0.4f, 0.2f);

        if (GUILayout.Button("House", houseStyle, GUILayout.Width(PaletteButtonSize * 2f), GUILayout.Height(PaletteButtonSize)))
        {
            currentPaintMode = PaintMode.House;
            selectedHouseIndex = -1;
        }

        GUI.backgroundColor = prevHouse;

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
                HouseData existingHouse = FindHouse(x, y);

                Color cellColor;
                string cellLabel = "";

                if (existingHouse != null)
                {
                    int houseIdx = workingHouses.IndexOf(existingHouse);
                    bool isSelectedHouse = houseIdx == selectedHouseIndex;
                    cellColor = isSelectedHouse
                        ? new Color(1f, 0.7f, 0.2f)
                        : new Color(0.6f, 0.4f, 0.2f);
                    cellLabel = $"H({existingHouse.StickmanQueue?.Length ?? 0})";
                }
                else if (!existingCell.HasValue)
                {
                    cellColor = new Color(0.15f, 0.15f, 0.15f);
                }
                else if (existingCell.Value.isHidden)
                {
                    cellColor = Color.black;
                    cellLabel = "H";
                }
                else if (existingCell.Value.color == StickmanColor.None)
                {
                    cellColor = new Color(0.45f, 0.45f, 0.45f);
                }
                else
                {
                    cellColor = GetColorForStickmanColor(existingCell.Value.color);
                }

                EditorGUI.DrawRect(cellRect, cellColor);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), Color.black);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), Color.black);

                if (!string.IsNullOrEmpty(cellLabel))
                    GUI.Label(cellRect, cellLabel, new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    });

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
            workingBusSequence.Add(CreateBusData(StickmanColor.Red, 3));
            isDirty = true;
        }
    }

    private void DrawHouseSection()
    {
        EditorGUILayout.LabelField("Houses", EditorStyles.miniBoldLabel);

        if (workingHouses.Count == 0)
        {
            EditorGUILayout.LabelField("No houses placed. Select House paint mode and click a cell.", EditorStyles.miniLabel);
            return;
        }

        for (int i = 0; i < workingHouses.Count; i++)
        {
            HouseData house = workingHouses[i];
            bool isSelected = i == selectedHouseIndex;

            GUIStyle rowStyle = new GUIStyle(GUI.skin.box);
            if (isSelected)
                rowStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.7f, 0.2f, 0.2f));

            EditorGUILayout.BeginVertical(rowStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"House ({house.GridX},{house.GridY})", GUILayout.Width(100));

            if (GUILayout.Button(isSelected ? "collapse" : "edit", GUILayout.Width(56)))
            {
                selectedHouseIndex = isSelected ? -1 : i;
                Repaint();
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                if (selectedHouseIndex == i)
                    selectedHouseIndex = -1;
                workingHouses.RemoveAt(i);
                isDirty = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (isSelected)
                DrawHouseQueueEditor(i);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawHouseQueueEditor(int houseIndex)
    {
        HouseData house = workingHouses[houseIndex];
        List<StickmanColor> queue = new List<StickmanColor>(house.StickmanQueue ?? new StickmanColor[0]);

        EditorGUILayout.LabelField("Stickman Queue (top = first dispensed)", EditorStyles.miniLabel);

        for (int i = 0; i < queue.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"  {i + 1}.", GUILayout.Width(24));

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = GetColorForStickmanColor(queue[i]);
            StickmanColor newColor = (StickmanColor)EditorGUILayout.EnumPopup(queue[i], GUILayout.Width(80));
            GUI.backgroundColor = prev;

            if (newColor != queue[i])
            {
                queue[i] = newColor;
                workingHouses[houseIndex] = new HouseData(house.GridX, house.GridY, queue.ToArray());
                isDirty = true;
            }

            GUI.enabled = i > 0;
            if (GUILayout.Button("up", GUILayout.Width(28)))
            {
                StickmanColor tmp = queue[i];
                queue[i] = queue[i - 1];
                queue[i - 1] = tmp;
                workingHouses[houseIndex] = new HouseData(house.GridX, house.GridY, queue.ToArray());
                isDirty = true;
            }
            GUI.enabled = i < queue.Count - 1;
            if (GUILayout.Button("dn", GUILayout.Width(28)))
            {
                StickmanColor tmp = queue[i];
                queue[i] = queue[i + 1];
                queue[i + 1] = tmp;
                workingHouses[houseIndex] = new HouseData(house.GridX, house.GridY, queue.ToArray());
                isDirty = true;
            }
            GUI.enabled = true;

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                queue.RemoveAt(i);
                workingHouses[houseIndex] = new HouseData(house.GridX, house.GridY, queue.ToArray());
                isDirty = true;
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(2);

        if (GUILayout.Button("Add Stickman to Queue"))
        {
            queue.Add(StickmanColor.Red);
            workingHouses[houseIndex] = new HouseData(house.GridX, house.GridY, queue.ToArray());
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

        workingHouses = new List<HouseData>();
        if (currentAsset.Houses != null)
        {
            foreach (var house in currentAsset.Houses)
            {
                if (house != null)
                    workingHouses.Add(new HouseData(house.GridX, house.GridY, house.StickmanQueue));
            }
        }

        selectedHouseIndex = -1;
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
            workingHouses.Clear();
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
            element.FindPropertyRelative("isHidden").boolValue = workingCells[i].isHidden;
        }

        SerializedProperty busProp = serialized.FindProperty("busSequence");
        busProp.arraySize = workingBusSequence.Count;
        for (int i = 0; i < workingBusSequence.Count; i++)
        {
            SerializedProperty element = busProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("capacity").intValue = workingBusSequence[i].Capacity;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingBusSequence[i].Color;
        }

        SerializedProperty housesProp = serialized.FindProperty("houses");
        housesProp.arraySize = workingHouses.Count;
        for (int i = 0; i < workingHouses.Count; i++)
        {
            SerializedProperty element = housesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("gridX").intValue = workingHouses[i].GridX;
            element.FindPropertyRelative("gridY").intValue = workingHouses[i].GridY;

            StickmanColor[] queue = workingHouses[i].StickmanQueue ?? new StickmanColor[0];
            SerializedProperty queueProp = element.FindPropertyRelative("stickmanQueue");
            queueProp.arraySize = queue.Length;
            for (int j = 0; j < queue.Length; j++)
                queueProp.GetArrayElementAtIndex(j).enumValueIndex = (int)queue[j];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Methods - painting

    private void PaintCell(int x, int y)
    {
        if (currentPaintMode == PaintMode.House)
        {
            PaintHouse(x, y);
            return;
        }

        HouseData existingHouse = FindHouse(x, y);
        if (existingHouse != null)
        {
            workingHouses.Remove(existingHouse);
            isDirty = true;
        }

        for (int i = 0; i < workingCells.Count; i++)
        {
            if (workingCells[i].gridX == x && workingCells[i].gridY == y)
            {
                ColoredCell updated = workingCells[i];
                updated.color = currentPaintMode == PaintMode.Hidden ? workingCells[i].color : selectedPaintColor;
                updated.isHidden = currentPaintMode == PaintMode.Hidden;
                workingCells[i] = updated;
                isDirty = true;
                return;
            }
        }

        workingCells.Add(new ColoredCell
        {
            gridX = x,
            gridY = y,
            color = currentPaintMode == PaintMode.Hidden ? StickmanColor.Red : selectedPaintColor,
            isHidden = currentPaintMode == PaintMode.Hidden
        });
        isDirty = true;
    }

    private void PaintHouse(int x, int y)
    {
        HouseData existing = FindHouse(x, y);
        if (existing != null)
        {
            int idx = workingHouses.IndexOf(existing);
            selectedHouseIndex = idx;
            return;
        }

        workingCells.RemoveAll(c => c.gridX == x && c.gridY == y);

        HouseData newHouse = new HouseData(x, y, new StickmanColor[0]);
        workingHouses.Add(newHouse);
        selectedHouseIndex = workingHouses.Count - 1;
        isDirty = true;
    }

    private void EraseCell(int x, int y)
    {
        HouseData existingHouse = FindHouse(x, y);
        if (existingHouse != null)
        {
            if (selectedHouseIndex == workingHouses.IndexOf(existingHouse))
                selectedHouseIndex = -1;
            workingHouses.Remove(existingHouse);
            isDirty = true;
            return;
        }

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

    private HouseData FindHouse(int x, int y)
    {
        foreach (var house in workingHouses)
        {
            if (house.GridX == x && house.GridY == y)
                return house;
        }
        return null;
    }

    private void ClampCellsToGrid()
    {
        workingCells.RemoveAll(c => c.gridX >= workingGridWidth || c.gridY >= workingGridHeight);
        workingHouses.RemoveAll(h => h.GridX >= workingGridWidth || h.GridY >= workingGridHeight);
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
            element.FindPropertyRelative("isHidden").boolValue = workingCells[i].isHidden;
        }

        SerializedProperty busProp = serialized.FindProperty("busSequence");
        busProp.arraySize = workingBusSequence.Count;
        for (int i = 0; i < workingBusSequence.Count; i++)
        {
            SerializedProperty element = busProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("capacity").intValue = workingBusSequence[i].Capacity;
            element.FindPropertyRelative("color").enumValueIndex = (int)workingBusSequence[i].Color;
        }

        SerializedProperty housesProp = serialized.FindProperty("houses");
        housesProp.arraySize = workingHouses.Count;
        for (int i = 0; i < workingHouses.Count; i++)
        {
            SerializedProperty element = housesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("gridX").intValue = workingHouses[i].GridX;
            element.FindPropertyRelative("gridY").intValue = workingHouses[i].GridY;

            StickmanColor[] queue = workingHouses[i].StickmanQueue ?? new StickmanColor[0];
            SerializedProperty queueProp = element.FindPropertyRelative("stickmanQueue");
            queueProp.arraySize = queue.Length;
            for (int j = 0; j < queue.Length; j++)
                queueProp.GetArrayElementAtIndex(j).enumValueIndex = (int)queue[j];
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