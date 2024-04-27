using System;
using KartGame.Custom;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public class ImportFromServer : EditorWindow
{

    private enum DownloadType {
        Cumulative,
        NPerTrack
    }
    private string serverPath;
    private string localPath;

    private readonly string[] trackNameList = {"Track1", "Track2", "Track3", "Track4", "Track5"};
    private readonly string[] cumulativeTrackNameList = {"Track1", "Track2", "Track3", "Track5"};

    private const int laps = 5;
    private DownloadType downloadType;
    private int lapsToDownload;
    private int trackMask;

    [MenuItem ("MLAgents/Import Demos From Firebase", priority = 30)]
    public static void ShowWindow() {
        GetWindow(typeof(ImportFromServer));
    }

    public void OnGUI() {
        serverPath = EditorGUILayout.TextField("Server Path to User", serverPath);
        downloadType = (DownloadType)EditorGUILayout.EnumPopup("Download Type", downloadType);
        if (downloadType == DownloadType.NPerTrack) {
            lapsToDownload = EditorGUILayout.IntField("Laps to Download", lapsToDownload);
        } else if (downloadType == DownloadType.Cumulative) {
            trackMask = EditorGUILayout.MaskField("Tracks to Download", trackMask, trackNameList);
        }
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
        string serverPathFixed = serverPath[1..].Replace(":", "%3A").Replace("/", "%2F"); // recordings%2Fusername-HH%3Amm%3Ass
        switch (downloadType) {
            case DownloadType.Cumulative:
                for (int limit = 1; limit <= cumulativeTrackNameList.Length; limit++) {
                    foreach (var trackName in cumulativeTrackNameList[..limit]) {
                        for (int lap = 0; lap < laps; lap++) {
                            string tServerPath = $"%2F{trackName}%2F{trackName}-{lap}.demo?alt=media";
                            string tLocalPath = $"/{folderN}/{trackName}-{lap}.demo";
                            EditorCoroutineUtility.StartCoroutine(RESTManager.GetAndWrite(serverPathFixed + tServerPath, localPath + tLocalPath, true), this);
                            counter++;
                        }
                    }
                    folderN++;
                }    
            break;
            case DownloadType.NPerTrack:
                foreach (var trackName in trackNameList) {
                    string type = "demo";
                    string subfolder = "demonstrations";
                    bool binary = true;
                    for (int lap = 0; lap < laps; lap++) {
                        if (lap >= lapsToDownload) {
                            type = "state";
                            subfolder = "replays";
                            binary = false;
                        }
                        string tServerPath = $"%2F{trackName}%2F{trackName}-{lap}.{type}?alt=media";
                        string tLocalPath = $"/{subfolder}/{folderN}/{trackName}-{lap}.{type}";
                        EditorCoroutineUtility.StartCoroutine(RESTManager.GetAndWrite(serverPathFixed + tServerPath, localPath + tLocalPath, binary), this);
                    }
                    folderN++;
                }
            break;
        }
    }

    public bool Check() {
        if (serverPath == "") throw new ArgumentNullException("Server Path");
        if (localPath == "") throw new ArgumentNullException("Local Path");
        if (downloadType == DownloadType.NPerTrack && lapsToDownload == 0) throw new ArgumentNullException("Laps to download");
        return true;
    }
}