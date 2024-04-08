using System.Collections;
using System.Diagnostics;
using System.IO;
using KartGame.Custom;
using KartGame.Custom.AI;
using KartGame.Custom.Training;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TrainingSessionEditor : EditorWindow
{
    private enum SessionFSM {
        Stopped,
        Started,
        Training,
        Waiting
    }
    public TrainingSession session;
    private SerializedObject serializedSession;
    private Track[] instantiatedTracks;
    private SessionFSM state;

    [MenuItem ("MLAgents/Start Tensorboard", priority = 50)]
    public static void StartTensorboard() {
        string condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
        LaunchTensorboard(condaStartScript);
        LaunchLocalhost();
    }


    [MenuItem ("MLAgents/Setup Training", priority = 10)]
    public static void ShowWindow() {
        GetWindow(typeof(TrainingSessionEditor));
    }

    public void OnEnable() {
        EditorApplication.playModeStateChanged += (state) => {ManageFSM(state);UnityEngine.Debug.Log(state);};
        state = SessionFSM.Stopped;
        ScriptableObject target = this;
        serializedSession = new SerializedObject(target);
    }

    public void OnGUI() {
        GUILayout.Label("Training Session Settings", EditorStyles.boldLabel);
        serializedSession.Update();
        SerializedProperty settingsProperty = serializedSession.FindProperty("session");

        EditorGUILayout.PropertyField(settingsProperty, includeChildren: true);
        serializedSession.ApplyModifiedProperties();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save")) {
            string savefile = EditorUtility.SaveFilePanel("Save Training Session Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "config", "json");
            if (savefile != "") session.ToFile(savefile);
        }

        if (GUILayout.Button("Load")) {
            string savefile = EditorUtility.OpenFilePanel("Load Training Session Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "json");
            if (savefile != "") session = TrainingSession.FromFile(savefile);
            serializedSession.ApplyModifiedProperties();
        }
        if (GUILayout.Button("Clear")) {
            session.settings = null;
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Start Training")) {
            session.Check();
            EditorCoroutineUtility.StartCoroutine(ExecuteSession(), this);
        }
    }

    private IEnumerator ExecuteSession() {
        state = SessionFSM.Started;
        foreach (var settings in session) {
            SetupTrainingScene(settings);
            LaunchTrainer(session.GetCondaScript(), settings.trainer, settings.runId);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(DelayedEnterPlaymode(5f));
            UnityEngine.Debug.Log("After DelayedEnterPlaymode");
            yield return new WaitUntil(() => state == SessionFSM.Waiting);
            UnityEngine.Debug.Log("After WaitUntil");
        }
        state = SessionFSM.Stopped;
    }

    private void SetupTrainingScene(TrainingSettings settings) {
        EditorSceneManager.OpenScene("Assets/Scenes/Training.unity");   //Opens scene if not currently open, reloads scene otherwise
        InstantiateTracks(settings);
        InstantiateKarts(settings);
    }

    private void InstantiateTracks(TrainingSettings settings) {
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

    private void InstantiateKarts(TrainingSettings settings) {
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

    private IEnumerator DelayedEnterPlaymode(float delay) {
        UnityEngine.Debug.Log($"Entering play mode in {delay} seconds...");
        yield return new EditorWaitForSeconds(delay);
        EditorApplication.EnterPlaymode();
    }
    
    private void ManageFSM(PlayModeStateChange stateChange) {
        switch (stateChange) {
            case PlayModeStateChange.EnteredPlayMode:
                if (state == SessionFSM.Started || state == SessionFSM.Waiting) {
                    state = SessionFSM.Training;
                }
            break;
            case PlayModeStateChange.EnteredEditMode:
                if (state == SessionFSM.Training) {
                    state = SessionFSM.Waiting;
                }
            break;
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
