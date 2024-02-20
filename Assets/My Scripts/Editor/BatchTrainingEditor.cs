using System.IO;
using KartGame.Custom;
using UnityEditor;
using UnityEngine;

public class BatchTrainingEditor : EditorWindow
{
    public TrainingSettings[] settings = {};
    private SerializedObject serializedSettings;

    [MenuItem ("MLAgents/Batch Train Settings", priority = 10)]
    public static void ShowWindow() {
        GetWindow(typeof(BatchTrainingEditor));
    }

    public void OnEnable() {
        ScriptableObject target = this;
        serializedSettings = new SerializedObject(target);
    }

    public void OnGUI() {
        GUILayout.Label("Batch Training Settings", EditorStyles.boldLabel);
        serializedSettings.Update();
        SerializedProperty settingsProperty = serializedSettings.FindProperty("settings");

        EditorGUILayout.PropertyField(settingsProperty, includeChildren: true);
        serializedSettings.ApplyModifiedProperties();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save")) {
            string savefile = EditorUtility.SaveFilePanel("Save Batch Training Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "config", "json");
            if (savefile != "") new TrainingSession(settings).SaveToFile(savefile);
        }

        if (GUILayout.Button("Load")) {
            string savefile = EditorUtility.OpenFilePanel("Load Batch Training Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "json");
            if (savefile != "") settings = TrainingSession.FromFile(savefile);
            serializedSettings.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();
    }

}
