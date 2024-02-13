using UnityEngine;
using UnityEditor;
using KartGame.Custom;
using KartGame.Custom.AI;
using System.Diagnostics;
using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Threading;

public class TrainingManagerEditor : EditorWindow
{
    private Track trainingTrack;
    private int trackInstances;
    private KartAgent trainingAgent;
    private int agentInstances;
    
    private string trainerName;
    private string runId;
    private string condaStartScript;

    private Process trainer;
    private bool wantsToStartPlayMode;
    public event Action OnReady; 

    public void StartTraining() {
        using (trainer = new()) {
            trainer.StartInfo.FileName = "cmd";
            trainer.StartInfo.UseShellExecute = false;
            trainer.StartInfo.RedirectStandardError = true;
            trainer.StartInfo.RedirectStandardOutput = true;
            trainer.StartInfo.RedirectStandardInput = true;
            trainer.StartInfo.Arguments = "/k";
            trainer.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError(args.Data);
            trainer.OutputDataReceived += DataReceivedCallback;
            trainer.Start();
            trainer.BeginErrorReadLine();
            trainer.BeginOutputReadLine();
            trainer.StandardInput.WriteLine("cd training");
            trainer.StandardInput.WriteLine(condaStartScript);
            trainer.StandardInput.WriteLine("conda activate mlagents");
            trainer.StandardInput.WriteLine($"mlagents-learn {trainerName} --run-id {runId} --time-scale 1");
            //trainer.WaitForExit();
        }
    }

    public void StartTrainingShell() {
        using (trainer = new()) {
            trainer.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start.bat";
            trainer.StartInfo.UseShellExecute = true;
            trainer.StartInfo.Arguments = $"{condaStartScript} {trainerName} {runId}";
            trainer.Start();
        }
    }
    private void DataReceivedCallback(object sender, DataReceivedEventArgs args) {
        UnityEngine.Debug.Log(args.Data);
        if (args.Data.Contains("Start training by pressing the Play button in the Unity Editor.")) {
            OnReady?.Invoke();
        }
    }

    [MenuItem ("MLAgents/Train")]
    public static void ShowWindow() {
        GetWindow(typeof(TrainingManagerEditor));
    }
    
    void OnGUI () {
        GUILayout.Label("Environment Settings", EditorStyles.boldLabel);
        trainingTrack = (Track) EditorGUILayout.ObjectField("Track", trainingTrack, typeof(Track), allowSceneObjects: false);
        trackInstances = EditorGUILayout.IntField("Track instances", trackInstances);
        
        trainingAgent = (KartAgent) EditorGUILayout.ObjectField("Agent", trainingAgent, typeof(KartAgent), allowSceneObjects: false);
        agentInstances = EditorGUILayout.IntField("Agent instances", agentInstances);

        GUILayout.Label("Trainer Settings", EditorStyles.boldLabel);
        trainerName = EditorGUILayout.TextField("Trainer Filename", trainerName);
        runId = EditorGUILayout.TextField("Run ID", runId);

        GUILayout.Label("System Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Conda start script");
        EditorGUILayout.LabelField(condaStartScript);
        if(GUILayout.Button("Browse")) {
            condaStartScript = EditorUtility.OpenFilePanel("Select conda start script", "C:/", "bat");
        }
        EditorGUILayout.EndHorizontal();

        if(GUILayout.Button("Start Training")) {
            EditorCoroutineUtility.StartCoroutineOwnerless(DelayedEnterPlaymode(30f));
            StartTrainingShell();
            /*
            OnReady += () => {wantsToStartPlayMode = true;};
            Thread t = new Thread(new ThreadStart(StartTraining));
            t.Start();
            EditorCoroutineUtility.StartCoroutine(CheckForEnterPlaymode(), this);*/
        }
    }

    IEnumerator CheckForEnterPlaymode() {
        while(!EditorApplication.isPlaying) {
            if (wantsToStartPlayMode) {
                wantsToStartPlayMode = false;
                EditorApplication.EnterPlaymode();
            }
            yield return new EditorWaitForSeconds(10f);
        }
    }

    IEnumerator DelayedEnterPlaymode(float delay) {
        UnityEngine.Debug.Log("Before wait");
        yield return new EditorWaitForSeconds(delay);
        UnityEngine.Debug.Log("After wait");
        EditorApplication.EnterPlaymode();
    }
}
