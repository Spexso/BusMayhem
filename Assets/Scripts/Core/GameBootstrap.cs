using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    // Fields
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LevelData levelData;
    [SerializeField] private ColorMatchPalette colorMap;

    // Methods
    private void Start()
    {
        ColorConverter.Initialize(colorMap);
        gridManager.InitializeGrid(levelData);
    }
}