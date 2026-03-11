using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    // Fields
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BusManager busManager;
    [SerializeField] private WaitingAreaManager waitingAreaManager;
    [SerializeField] private LevelData levelData;
    [SerializeField] private ColorMatchPalette colorMap;

    // Methods
    private void Awake()
    {
        ColorConverter.Initialize(colorMap);
    }

    private void Start()
    {
        gridManager.InitializeGrid(levelData);
        busManager.Initialize(levelData);
        waitingAreaManager.Initialize(levelData);

        InputManager.Instance.SetInputEnabled(true);
    }
}