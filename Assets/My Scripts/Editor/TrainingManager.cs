using UnityEngine;
using UnityEditor;
using KartGame.Custom;
using KartGame.Custom.AI;
using System.Diagnostics;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System.IO;
using UnityEditor.SceneManagement;
using System;

public class TrainingManager: EditorWindow
{
    private TrainingSettings settings;
    private Track[] instantiatedTracks;
    
    private string trainerName = "";
    private string runId = "";
    private string condaStartScript = "";

    [MenuItem ("MLAgents/Start Tensorboard", priority = 50)]
    public static void StartTensorboard() {
        string condaStartScript = KartGame.Custom.Training.TrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
        LaunchTensorboard(condaStartScript);
        LaunchLocalhost();
    }

    [MenuItem ("MLAgents/Train", priority = 1)]
    public static void ShowTrainingManager() {
        GetWindow(typeof(TrainingManager));
    }

    
    void OnGUI () {
        GUILayout.Label("Environment Settings", EditorStyles.boldLabel);
        settings.track = (Track) EditorGUILayout.ObjectField("Track", settings.track, typeof(Track), allowSceneObjects: false);
        settings.trackInstances = EditorGUILayout.IntField("Track instances", settings.trackInstances);
        
        settings.agent = (KartAgent) EditorGUILayout.ObjectField("Agent", settings.agent, typeof(KartAgent), allowSceneObjects: false);
        settings.agentInstances = EditorGUILayout.IntField("Agent instances", settings.agentInstances);

        GUILayout.Label("Trainer Settings", EditorStyles.boldLabel);
        trainerName = EditorGUILayout.TextField("Trainer Filename", trainerName);
        runId = EditorGUILayout.TextField("Run ID", runId);

        if(GUILayout.Button("Start Training")) {
            CheckInput();
            SetupTrainingScene();
            EditorCoroutineUtility.StartCoroutineOwnerless(DelayedEnterPlaymode(30f));
            LaunchTrainer(condaStartScript, trainerName, runId);
        }
    }

    IEnumerator DelayedEnterPlaymode(float delay) {
        UnityEngine.Debug.Log($"Entering play mode in {delay} seconds...");
        yield return new EditorWaitForSeconds(delay);
        EditorApplication.EnterPlaymode();
    }

    void CheckInput() {
        if (settings.track == null) throw new ArgumentNullException("Track");
        if (settings.trackInstances == 0) throw new ArgumentNullException("Track Instances");
        if (settings.agent == null) throw new ArgumentNullException("Agent");
        if (settings.agentInstances == 0) throw new ArgumentNullException("Agent Instances");
        if (condaStartScript == "") {
            condaStartScript = KartGame.Custom.Training.TrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
            if (condaStartScript == "") throw new ArgumentNullException("Conda activation script");
        }
        if (trainerName == "") {
            trainerName = KartGame.Custom.Training.TrainingSettings.GetSerializedSettings().FindProperty("m_DefaultTrainer").stringValue;
            if (trainerName == "") throw new ArgumentNullException("Trainer");
            UnityEngine.Debug.Log($"Using default trainer: {trainerName}");
        }
        if (runId == "") throw new ArgumentNullException("RunID");
    }

    void SetupTrainingScene() {
        EditorSceneManager.OpenScene("Assets/Scenes/Training.unity");   //Opens scene if not currently open, reloads scene otherwise
        InstantiateTracks();
        InstantiateKarts();
    }

    void InstantiateTracks() {
        instantiatedTracks = new Track[settings.trackInstances];
        Vector3 trackBounds = settings.track.GetBoundingBox().size;
        int side = Mathf.CeilToInt(Mathf.Sqrt(settings.trackInstances));
        for (int row = 0, index = 0; row < side && index < settings.trackInstances; row++) {
            for (int col = 0; col < side && index < settings.trackInstances; col++, index++) {
                instantiatedTracks[index] = Instantiate(settings.track, new Vector3(col * trackBounds.x, 0, row * trackBounds.z), Quaternion.identity);
                instantiatedTracks[index].name = $"{settings.track.name} {index}";
                instantiatedTracks[index].SetSpawnpoint(); 
            }
        }
    }

    void InstantiateKarts() {
        for (int t = 0; t < instantiatedTracks.Length; t++) {
            Track track = instantiatedTracks[t];
            Transform spawnpoint = track.GetSpawnpoint();
            
            for (int i = 0; i < settings.agentInstances; i++) {
                KartAgent instance = Instantiate(settings.agent, spawnpoint.position, spawnpoint.rotation);
                instance.name = $"{settings.agent.name} {t}-{i}";
                instance.GetComponent<KartAgent>().Track = track;
            }
        }
    }
    
    public static void LaunchTrainer(string condaStartScript, string trainerName, string runId) {
        using (Process trainer = new()) {
            trainer.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start_training.bat";
            trainer.StartInfo.UseShellExecute = true;
            trainer.StartInfo.Arguments = $"{condaStartScript} {trainerName} {runId}";
            trainer.Start();
        }
    }

    public static void LaunchTensorboard(string condaStartScript) {
        using (Process tb = new()) {
            tb.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start_tensorboard.bat";
            tb.StartInfo.UseShellExecute = true;
            tb.StartInfo.Arguments = $"{condaStartScript}";
            tb.Start();
        }
    }

    public static void LaunchLocalhost() {
        using (Process lh = new()) {
            lh.StartInfo.FileName = "http://localhost:6006";
            lh.StartInfo.UseShellExecute = true;
            lh.Start();
        }
    }
}