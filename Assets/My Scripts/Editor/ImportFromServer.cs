using System;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ImportFromServer : EditorWindow
{
    private string serverPath;
    private string localPath;

    private readonly string[] trackNameList = {"Track1", "Track2", "Track3", "Track5"};
    private const int laps = 5;

    [MenuItem ("MLAgents/Import Demos From Firebase", priority = 30)]
    public static void ShowWindow() {
        GetWindow(typeof(ImportFromServer));
    }

    public void OnGUI() {
        serverPath = EditorGUILayout.TextField("Server Path to User", serverPath);
        if (GUILayout.Button("Import")) {
            localPath = EditorUtility.SaveFolderPanel("Import Location", "Training", "demos");
            if (Check()) {
                Import();
            }
        }
    }

    public void Import() {
        int folderN = 0;
        int counter = 0;
        string fixedServerPath = serverPath[1..].Replace(":", "%3A").Replace("/", "%2F"); // recordings%2Fusername-HH%3Amm%3Ass
        for (int limit = 1; limit <= trackNameList.Length; limit++) {
            foreach (var trackName in trackNameList[..limit]) {
                for (int lap = 0; lap < laps; lap++) {
                    string tServerPath = $"%2F{trackName}%2F{trackName}-{lap}.demo?alt=media";
                    string tLocalPath = $"/{folderN}/{trackName}-{lap}.demo";
                    EditorCoroutineUtility.StartCoroutine(RESTManager.GetAndWrite(fixedServerPath + tServerPath, localPath + tLocalPath), this);
                    counter++;
                }
            }
            folderN++;
        }
    }

    public bool Check() {
        if (serverPath == "") throw new ArgumentNullException("serverPath");
        if (localPath == "") throw new ArgumentNullException("localPath");
        return true;
    }
}