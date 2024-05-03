using System;
using System.Collections.Generic;
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
    private string fixedServerPath;
    private string localPath;

    private readonly string[] trackNameList = {"Track0", "Track1", "Track2", "Track3", "Track4"};

    private const int laps = 5;
    private DownloadType downloadType;
    private int lapsToDownload;
    private int demoMask;
    private int replayMask;

    [MenuItem ("MLAgents/Import Demos From Firebase", priority = 30)]
    public static void ShowWindow() {
        GetWindow(typeof(ImportFromServer));
    }

    public void OnGUI() {
        serverPath = EditorGUILayout.TextField("Server Path to User", serverPath);
        downloadType = (DownloadType)EditorGUILayout.EnumPopup("Download Type", downloadType);
        EditorGUI.indentLevel++;
        switch (downloadType) {
            case DownloadType.NPerTrack:
                lapsToDownload = EditorGUILayout.IntField("Laps to Download", lapsToDownload);
            break;
            case DownloadType.Cumulative:
                demoMask = EditorGUILayout.MaskField("Training Tracks", demoMask, trackNameList);
                EditorGUILayout.MaskField("Evaluation Tracks", int.MaxValue-demoMask, trackNameList);                
            break;
        }
        EditorGUI.indentLevel--;
        if (GUILayout.Button("Import")) {
            localPath = EditorUtility.SaveFolderPanel("Import Location", "Training", "demos");
            if (localPath != "" && Check()) {
                Import();
            }
        }
    }

    public void Import() {
        int folderN = 0;        
        
        fixedServerPath =           // /recordings/username-HH:mm:ss
            serverPath[1..]         // recordings/username-HH:mm:ss
            .Replace(":", "%3A")    // recordings/username-HH%3Amm%3Ass
            .Replace("/", "%2F");   // recordings%2Fusername-HH%3Amm%3Ass

        switch (downloadType) {
            case DownloadType.Cumulative:
                int selectedCount = MaskToNumberOfSelected(demoMask, trackNameList.Length);
                int[] trainingArray = new int[selectedCount];
                int trainingIndex = 0;
                Debug.Log(trackNameList.Length - selectedCount);
                int[] evaluationArray = new int [trackNameList.Length - selectedCount];
                int evaluationIndex = 0;

                for (int i = 0; i < trackNameList.Length; i++) {
                    if ((demoMask & (1 << i)) == 0) {
                        evaluationArray[evaluationIndex] = i;
                        evaluationIndex++;
                    } else {
                        trainingArray[trainingIndex] = i;
                        trainingIndex++;
                    }
                }

                for (int i = 1; i <= selectedCount; i++, folderN++) {
                    foreach (int trackN in trainingArray[..i]) {
                        for (int lap = 0; lap < laps; lap++) {
                            DownloadLap(trackNameList[trackN], lap, folderN.ToString(), true);
                        }
                    }
                }

                folderN = 0;
                foreach (int trackN in evaluationArray) {
                    for (int lap = 0; lap < laps; lap++) {
                        DownloadLap(trackNameList[trackN], lap, folderN.ToString(), false);
                    }
                    folderN++;
                }

            break;
            case DownloadType.NPerTrack:
                foreach (var trackName in trackNameList) {
                    bool isBinary = true;
                    for (int lap = 0; lap < laps; lap++) {
                        if (lap >= lapsToDownload) {
                            isBinary = false;
                        }
                        DownloadLap(trackName, lap, folderN.ToString(), isBinary);
                    }
                    folderN++;
                }
            break;
        }
    }

    public void DownloadLap(string trackName, int lap, string folderName, bool isBinary) {
        string type;
        string subfolder;
        if (isBinary) {
            type = "demo";
            subfolder = "demonstrations";
        } else {
            type = "state";
            subfolder = "replays";
        }

        string tServerPath = $"%2F{trackName}%2F{trackName}-{lap}.{type}?alt=media";
        string tLocalPath = $"/{subfolder}/{folderName}/{trackName}-{lap}.{type}";

        EditorCoroutineUtility.StartCoroutine(RESTManager.GetAndWrite(fixedServerPath + tServerPath, localPath + tLocalPath, isBinary), this);
    }

    public bool Check() {
        if (serverPath == "") throw new ArgumentNullException("Server Path");
        if (localPath == "") throw new ArgumentNullException("Local Path");
        if (downloadType == DownloadType.NPerTrack && lapsToDownload == 0) throw new ArgumentNullException("Laps to download");
        return true;
    }

    public static int MaskToNumberOfSelected(int mask, int length) {
        int acc = 0;
        for (int i = 0; i < length; i++) {
            if ((mask & (1 << i)) != 0) {
                acc++;
            }
        }
        return acc;
    } 
}