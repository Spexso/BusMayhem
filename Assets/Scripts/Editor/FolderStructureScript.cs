using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides functionality to create a standardized folder hierarchy within a Unity project to organize scripts,
/// prefabs, scenes, and related assets.
/// </summary>
public class CreateFolderStructure
{
    [MenuItem("Tools/Setup Folder Structure")]
    public static void Create()
    {
        string[] folders = new string[]
        {
            "Assets/Scripts/Core",
            "Assets/Scripts/Grid",
            "Assets/Scripts/Bus",
            "Assets/Scripts/Stickman",
            "Assets/Scripts/UI",
            "Assets/Scripts/Data",
            "Assets/Scripts/Editor",
            "Assets/Levels",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Grid",
            "Assets/Prefabs/Bus",
            "Assets/Scenes",
            "Assets/Materials"
        };

        foreach (string path in folders)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                    Debug.Log($"Created: {next}");
                }
                current = next;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Folder structure setup done.");
    }
}