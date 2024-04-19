#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using KartGame.Custom.Training;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public class TrainingSessionEditor : EditorWindow
{
    private enum SessionFSM {
        Stopped,
        Started,
        Training,
        Evaluating,
        Waiting
    }
    public TrainingSession session;
    private SerializedObject serializedSession;
    private SessionFSM state;
    private int sessionIndex;

    Vector2 scrollPosition = Vector2.zero;

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
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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
            session.steps = null;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Training")) {
            if (session.Check()) {
                state = SessionFSM.Started;
            }
        }
        if (GUILayout.Button("STOP")) {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            state = SessionFSM.Stopped;
            sessionIndex = 0;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void Update() {
        switch (state) {
            case SessionFSM.Training:
            case SessionFSM.Evaluating:
            case SessionFSM.Waiting:
            case SessionFSM.Stopped:
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
        switch (session[sessionIndex].stepType) {
            case SessionStepType.Training:
                TrainingSession.SetupTrainingScene(session[sessionIndex].trainingSettings);
                LaunchTrainer(session.GetCondaScript(),
                                session[sessionIndex].trainingSettings.trainer,
                                session[sessionIndex].trainingSettings.runId,
                                session[sessionIndex].trainingSettings.initializeFrom);
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(DelayedEnterPlaymode(10f));
            break;
            case SessionStepType.Evaluation:
                TrainingSession.SetupEvaluationScene(session[sessionIndex].evaluationSettings);
                EditorApplication.EnterPlaymode();
            break;
        }
    }

    private void ManageStateChange(PlayModeStateChange stateChange) {
        switch (stateChange) {
            case PlayModeStateChange.ExitingEditMode:
                if (state == SessionFSM.Waiting) {
                    switch (session[sessionIndex].stepType) {
                        case SessionStepType.Training:
                            state = SessionFSM.Training;
                        break;
                        case SessionStepType.Evaluation:
                            state = SessionFSM.Evaluating;
                        break;
                    }
                }
            break;
            case PlayModeStateChange.ExitingPlayMode:
                if (state == SessionFSM.Training) {
                    MoveTrainedModel(session[sessionIndex].trainingSettings.runId);
                    sessionIndex++;
                    state = SessionFSM.Started;
                } else if (state == SessionFSM.Evaluating) {
                    sessionIndex++;
                    state = SessionFSM.Started;
                }
            break;
        }
    }

    public static void MoveTrainedModel(string runId) {
        string srcPath = $"{Directory.GetParent(Application.dataPath)}/Training/results/{runId}/Kart.onnx";
        string dstPath = $"{Application.dataPath}/ML-Agents/Trained Models/{runId}.onnx";

        File.Copy(srcPath, dstPath, true);
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"Moved model from {srcPath} to {dstPath}");
    }

    public static IEnumerator DelayedEnterPlaymode(float delay) {
        UnityEngine.Debug.Log($"Entering play mode in {delay} seconds...");
        yield return new EditorWaitForSeconds(delay);
        EditorApplication.EnterPlaymode();
    }
    
    public static void LaunchTrainer(string condaStartScript, string trainerName, string runId, string initializeFrom = "") {
        using (Process trainer = new()) {
            trainer.StartInfo.UseShellExecute = true;
            if (initializeFrom == "") {
                trainer.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start_training.bat";
                trainer.StartInfo.Arguments = $"{condaStartScript} trainers/{trainerName} {runId}";
            } else {
                trainer.StartInfo.FileName = $"{Directory.GetParent(Application.dataPath)}/Training/start_training_initialized.bat";
                trainer.StartInfo.Arguments = $"{condaStartScript} trainers/{trainerName} {runId} {initializeFrom}";
            }
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
#endif