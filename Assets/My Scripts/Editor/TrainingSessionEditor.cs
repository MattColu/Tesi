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
    private int sessionIndex;

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

    public void Awake() {
        state = SessionFSM.Stopped;
        sessionIndex = 0;
    }

    public void OnEnable() {
        EditorApplication.playModeStateChanged += (state) => ManageStateChange(state);
        ScriptableObject target = this;
        serializedSession = new SerializedObject(target);
    }

    public void OnGUI() {
        EditorGUILayout.BeginVertical();
        GUILayout.Label("Training Session Settings", EditorStyles.boldLabel);
        serializedSession.Update();
        SerializedProperty settingsProperty = serializedSession.FindProperty("session");

        EditorGUILayout.PropertyField(settingsProperty, includeChildren: true);
        serializedSession.ApplyModifiedProperties();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Conda: {session.GetCondaScript()}");
        if (GUILayout.Button("Edit")) {
            EditorUtility.OpenPropertyEditor(AssetDatabase.LoadAssetAtPath<Object>("Assets/My Scripts/Editor/DefaultTrainingSettings.asset"));
        }
        if (GUILayout.Button("↻")) {
            session.ResetCondaScript();
            serializedSession.Update();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField($"State: {state}");
        EditorGUILayout.LabelField($"Index: {sessionIndex}");
        
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
            if (session.Check()) {
                state = SessionFSM.Started;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void Update() {
        switch (state) {
            case SessionFSM.Training:
            case SessionFSM.Stopped:
            case SessionFSM.Waiting:
                return;
            case SessionFSM.Started:
                if (sessionIndex >= session.Length) {
                    Awake();
                    return;
                }
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
                state = SessionFSM.Waiting;
            break;
        }
    }

    private IEnumerator Execute() {
        SetupTrainingScene(session[sessionIndex]);
        LaunchTrainer(session.GetCondaScript(), session[sessionIndex].trainer, session[sessionIndex].runId);
        yield return EditorCoroutineUtility.StartCoroutineOwnerless(DelayedEnterPlaymode(10f));
    }

    private void ManageStateChange(PlayModeStateChange stateChange) {
        switch (stateChange) {
            case PlayModeStateChange.ExitingEditMode:
                if (state == SessionFSM.Waiting) {
                    state = SessionFSM.Training;
                }
            break;
            case PlayModeStateChange.ExitingPlayMode:
                if (state == SessionFSM.Training) {
                    sessionIndex++;
                    state = SessionFSM.Started;
                }
            break;
        }
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
    
    public static void LaunchTrainer(string condaStartScript, string trainerName, string runId) {
        using (Process trainer = new()) {
            trainer.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start_training.bat";
            trainer.StartInfo.UseShellExecute = true;
            trainer.StartInfo.Arguments = $"{condaStartScript} trainers/{trainerName} {runId}";
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
