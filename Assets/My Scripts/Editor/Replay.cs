using System;
using System.IO;
using Cinemachine;
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
    private Track track;
    private string filepath;
     
    [MenuItem ("MLAgents/Replay Demo", priority = 40)]
    public static void ShowWindow() {
        GetWindow(typeof(Replay));
    }

    void OnGUI () {
        track = (Track) EditorGUILayout.ObjectField("Track", track, typeof(Track), allowSceneObjects: false);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Demo File", filepath);
        if (GUILayout.Button("Browse")) {
            filepath = EditorUtility.OpenFilePanel("Choose a demo file", "", "state");
        }
        EditorGUILayout.EndHorizontal();
        if(GUILayout.Button("Load Demo")) {
            if (SetupReplayScene()) {
                EditorApplication.EnterPlaymode();
            }
        }
    }

    bool SetupReplayScene() {
        string trackFolder = ReplaySettings.GetSerializedSettings().FindProperty("m_TrackFolder").stringValue;
        ArcadeKart kart;
        try {
            string trackName = filepath.Replace(".state", "").Split("/")[^1].Split("-")[0];
            if (EditorSceneManager.OpenScene($"{trackFolder}/{trackName}.unity") == null) {
                throw new FileNotFoundException();
            }
            
            FindObjectOfType<GameFlowManagerCustom>().gameObject.SetActive(false);
            FindObjectOfType<Objective>().gameObject.SetActive(false);
            
            kart = FindObjectOfType<ArcadeKart>();
            kart.GetComponent<KartAgent>().enabled = false;
            kart.GetComponent<DemonstrationRecorder>().enabled = false;
            kart.GetComponent<StateRecorder>().enabled = false;

        } catch (Exception e){
            Debug.LogError(e);
            if (track != null) {
                EditorSceneManager.OpenScene("Assets/Scenes/Training.unity");
                Instantiate(track);
                kart = (ArcadeKart)Instantiate(ReplaySettings.GetSerializedSettings().FindProperty("m_DefaultKart").objectReferenceValue);
            } else {
                Debug.LogError("Filename does not contain track number and a track was not manually specified");
                return false;
            }
        }
        kart.gameObject.SetActive(false);
            StatePlayer statePlayer = kart.AddComponent<StatePlayer>();
            statePlayer.SetFullpath(filepath);
        kart.gameObject.SetActive(true);
        return true;
    }
}
