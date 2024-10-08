using System;
using System.IO;
using System.Linq;
using KartGame.Custom;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handles downloading and saving user data from the server.
/// </summary>
public class ImportFromServer : EditorWindow
{
    private enum DownloadType {
        Cumulative,
        NPerTrack
    }
    private string serverPath;
    private string fixedServerPath;
    private string localPath;
    private bool clear;

    private readonly string[] trackNameList = {"Track0", "Track1", "Track2", "Track3", "Track4"};

    private const int laps = 4;
    private DownloadType downloadType;
    private int lapsToDownload;
    private int demoMask;

    [MenuItem ("Kart/Import Demos From Firebase", priority = 30)]
    public static void ShowWindow() {
        GetWindow(typeof(ImportFromServer));
    }

    public void OnGUI() {
        serverPath = EditorGUILayout.TextField("Server Path to User", serverPath);
        downloadType = (DownloadType)EditorGUILayout.EnumPopup("Download Type", downloadType);
        EditorGUI.indentLevel++;
        switch (downloadType) {
            case DownloadType.NPerTrack:
                lapsToDownload = EditorGUILayout.IntSlider("Training Laps", lapsToDownload, 1, laps);
                EditorGUILayout.LabelField($"Evaluation Laps:    {laps - lapsToDownload}");
            break;
            case DownloadType.Cumulative:
                demoMask = EditorGUILayout.MaskField("Training Tracks", demoMask, trackNameList);
                EditorGUILayout.MaskField("Evaluation Tracks", int.MaxValue-demoMask, trackNameList);
                lapsToDownload = EditorGUILayout.IntSlider("Training Laps", lapsToDownload, 1, laps);
            break;
        }
        EditorGUI.indentLevel--;
        clear = EditorGUILayout.Toggle("Clear Folder", clear);
        if (GUILayout.Button("Import")) {
            localPath = EditorUtility.SaveFolderPanel("Import Location", "Training", "demos");
            if (localPath != "" && Check()) {
                if (clear) Clear(localPath);
                Import();
            }
        }
    }

    public void Clear(string path) {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
        if (Path.GetFileName(path) != "demos") throw new AccessViolationException(path);
        
        string[] dirarray = Directory.EnumerateDirectories(path).ToArray();
        if (dirarray.Any(f => Path.GetFileName(f) != "demonstrations" && Path.GetFileName(f) != "replays")) {
            throw new AccessViolationException(path);
        }
        foreach (string dir in dirarray) {
            Directory.Delete(dir, true);
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
                
                foreach (int trainingTrackN in trainingArray) {
                    for (int lap = 0; lap < lapsToDownload; lap++) {
                        DownloadLap(trackNameList[trainingTrackN], lap, trainingTrackN.ToString(), true);
                    }
                }
                foreach (int evaluationTrackN in evaluationArray) {
                    for (int lap = 0; lap < laps; lap++) {
                        DownloadLap(trackNameList[evaluationTrackN], lap, evaluationTrackN.ToString(), false);
                    }
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