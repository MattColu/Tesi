using System;
using System.IO;
using KartGame.Custom;
using KartGame.Custom.AI;
using KartGame.Custom.Demo;
using KartGame.KartSystems;
using Unity.MLAgents.Demonstrations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class Replay : EditorWindow
{
    private string filepath;
     
    [MenuItem ("MLAgents/Replay .state File", priority = 31)]
    public static void ShowWindow() {
        GetWindow(typeof(Replay));
    }

    void OnGUI () {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Demo File", filepath);
        if (GUILayout.Button("Browse")) {
            filepath = EditorUtility.OpenFilePanel("Choose a demo file", "", "state");
        }
        EditorGUILayout.EndHorizontal();
        if(GUILayout.Button("Load Demo")) {
            if (SetupAndOpenReplayScene(filepath)) {
                EditorApplication.EnterPlaymode();
            }
        }
    }

    public static bool SetupAndOpenReplayScene(string filepath, Track track = null, bool replay = true) {
        string trackFolder = ReplaySettings.GetSerializedSettings().FindProperty("m_TrackFolder").stringValue;
        ArcadeKart kart;
        string trackName = GetTrackNameByFilename(filepath);

        if (trackName == "") throw new FormatException($"Couldn't isolate track name from path {filepath}");
        if (EditorSceneManager.OpenScene($"{trackFolder}/{trackName}.unity") == null) throw new FileNotFoundException($"{trackName}.unity");
        
        FindObjectOfType<GameFlowManagerCustom>().gameObject.SetActive(false);
        FindObjectOfType<Objective>().gameObject.SetActive(false);
        
        kart = FindObjectOfType<ArcadeKart>();
        kart.GetComponent<KartAgent>().enabled = false;
        kart.GetComponent<DemonstrationRecorder>().enabled = false;
        kart.GetComponent<StateRecorder>().enabled = false;

        if (replay) {
            kart.gameObject.SetActive(false);
                StatePlayer statePlayer = kart.AddComponent<StatePlayer>();
                statePlayer.SetFullpath(filepath);
            kart.gameObject.SetActive(true);
        }
        return true;
    }
    
    public static string GetTrackNameByFilename(string filepath) {
        return filepath                 // path\to/file\Track0-0.state
                .Replace(".state", "")  // path\to/file\Track0-0
                .Split('/', '\\')[^1]   // Track0-0
                .Split('-')[0];         // Track0
    }
}
