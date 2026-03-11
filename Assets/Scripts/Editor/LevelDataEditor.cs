using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    // Fields

    private List<ValidationResult> _lastResults;

    // Methods

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Validate Level Data", GUILayout.Height(30)))
        {
            LevelData levelData = (LevelData)target;
            _lastResults = LevelDataValidator.Validate(levelData);
        }

        if (_lastResults != null)
            DrawValidationResults();
    }

    private void DrawValidationResults()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

        if (_lastResults.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
            return;
        }

        foreach (var result in _lastResults)
        {
            MessageType messageType = result.Severity == ValidationSeverity.Error
                ? MessageType.Error
                : MessageType.Warning;

            EditorGUILayout.HelpBox(result.Message, messageType);
        }

        EditorGUILayout.Space(4);

        if (LevelDataValidator.HasErrors(_lastResults))
            EditorGUILayout.HelpBox("This level has errors and may not function correctly.", MessageType.Error);
        else
            EditorGUILayout.HelpBox("This level has warnings but no blocking errors.", MessageType.Warning);
    }
}