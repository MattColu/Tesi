#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using KartGame.Custom;
using KartGame.Custom.Training;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Manages advancing the Training Session and all interactions with the Unity Editor.
/// <para>
/// State changes:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="SessionFSM"/> enumerates all possible execution states; 
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="SessionStepType"/> (defined in <see cref="TrainingSession"/>) enumerates the two possible step types (<see cref="SessionStepType.Training"/> or <see cref="SessionStepType.Evaluation"/>);
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="EditorApplication.playModeStateChanged"/> is an event that gets subscribed by this editor window, signals every change from Play to Edit Mode and vice versa.
/// </description>
/// </item>
/// </list>
/// </para>
/// </summary>
public class TrainingSessionEditor : EditorWindow
{
    private enum SessionFSM {
        Stopped,
        Started,
        Training,
        Evaluating,
        Waiting
    }

    public TrainingSession sessionTemplate;
    private TrainingSession session;
    private SerializedObject serializedSession;
    private SessionFSM state;
    private int sessionStepIndex;

    private string commonName;
    private string commonTrainer;
    private bool trackOverride;
    private Track[] tracks;
    private bool customNumbering;
    private string[] numbering;
    private int sessionIndex;
    private int internalEvaluationIndex;
    private int internalEvaluationNumber;

    Vector2 scrollPosition = Vector2.zero;

    [MenuItem ("Kart/Start Tensorboard", priority = 50)]
    public static void StartTensorboard() {
        string condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
        LaunchTensorboard(condaStartScript);
        LaunchLocalhost();
    }


    [MenuItem ("Kart/Setup Training", priority = 10)]
    public static void ShowWindow() {
        GetWindow<TrainingSessionEditor>();
    }

    public void Awake() {
        state = SessionFSM.Stopped;
        sessionStepIndex = 0;
        sessionIndex = 0;
        internalEvaluationIndex = 0;
        tracks = new Track[0];
        numbering = new string[0];
    }

    public void OnEnable() {
        EditorApplication.playModeStateChanged += (state) => ManageStateChange(state);
        ScriptableObject target = this;
        serializedSession = new SerializedObject(target);
    }

    public void OnGUI() {
        commonName = EditorGUILayout.TextField(new GUIContent("Session Username", "This prefix will be applied to all RunId, InitializeFrom and ModelRunId fields"), commonName);
        commonTrainer = EditorGUILayout.TextField(new GUIContent("Session Trainer", "This trainer will override all Trainer fields (suffix is added as usual, if needed)"), commonTrainer);
        trackOverride = EditorGUILayout.BeginFoldoutHeaderGroup(trackOverride, new GUIContent("Track Override", "The entire session will be executed for each of these tracks"));
            if (trackOverride) {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+")) {
                        System.Array.Resize(ref tracks, tracks.Length + 1);
                        System.Array.Resize(ref numbering, numbering.Length + 1);
                    }
                    if (GUILayout.Button("-")) {
                        if (tracks.Length > 0) {
                            System.Array.Resize(ref tracks, tracks.Length - 1);
                            System.Array.Resize(ref numbering, numbering.Length - 1);
                        }
                    }
                EditorGUILayout.EndHorizontal();
                for (int i = 0; i < tracks.Length; i++) {
                    tracks[i] = (Track) EditorGUILayout.ObjectField($"Track {i}:{GetLabel(i)}", tracks[i], typeof(Track), allowSceneObjects: false);
                }
                EditorGUI.indentLevel--;
            }
        EditorGUILayout.EndFoldoutHeaderGroup();

        customNumbering = EditorGUILayout.BeginToggleGroup(new GUIContent("Custom Track Number Suffix", "Wheter the automatic track number suffix should follow regular ordinal numbering or a custom numbering (supports non numerical suffixes)"), customNumbering);
            if (customNumbering) {
                EditorGUI.indentLevel++;
                for (int i = 0; i < numbering.Length; i++) {
                    numbering[i] = EditorGUILayout.TextField($"Track {i} suffix:{GetLabel(i)}", numbering[i]);
                }
                EditorGUI.indentLevel--;
            }
        EditorGUILayout.EndToggleGroup();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            serializedSession.Update();
            SerializedProperty settingsProperty = serializedSession.FindProperty("sessionTemplate");

            EditorGUILayout.PropertyField(settingsProperty, includeChildren: true);
            serializedSession.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save")) {
                    string savefile = EditorUtility.SaveFilePanel("Save Training Session Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "config", "json");
                    if (savefile != "") sessionTemplate.ToFile(savefile);
                }

                if (GUILayout.Button("Load")) {
                    string savefile = EditorUtility.OpenFilePanel("Load Training Session Configuration", $"{Directory.GetParent(Application.dataPath)}/Training/configs", "json");
                    if (savefile != "") sessionTemplate = TrainingSession.FromFile(savefile);
                    serializedSession.ApplyModifiedProperties();
                }
                if (GUILayout.Button("Clear")) {
                    sessionTemplate.steps = null;
                }
            EditorGUILayout.EndHorizontal();
            
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Separator();
        
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Conda: {sessionTemplate.GetCondaScript()}");
            if (GUILayout.Button("Edit")) {
                EditorUtility.OpenPropertyEditor(AssetDatabase.LoadAssetAtPath<Object>("Assets/My Scripts/Editor/DefaultTrainingSettings.asset"));
            }
            if (GUILayout.Button("↻")) {
                sessionTemplate.ResetCondaScript();
                serializedSession.Update();
            }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField($"State: {state}");
        EditorGUILayout.LabelField($"Session: {sessionIndex}");
        EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Step: {sessionStepIndex}");
            if (state == SessionFSM.Evaluating) {
                EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"Evaluation: {internalEvaluationIndex+1}/{internalEvaluationNumber}");
                EditorGUI.indentLevel--;
            }
        EditorGUI.indentLevel--;
        EditorGUILayout.Separator();
        
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start")) {
                Inject();
                if (session.Check()) {
                    state = SessionFSM.Started;
                }
            }
            if (GUILayout.Button("STOP")) {
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                state = SessionFSM.Stopped;
                sessionStepIndex = 0;
                sessionIndex = 0;
                internalEvaluationIndex = 0;
            }
        EditorGUILayout.EndHorizontal();
    }

    private void Update() {
        switch (state) {
            case SessionFSM.Training:
            case SessionFSM.Evaluating:
            case SessionFSM.Waiting:
            case SessionFSM.Stopped:
                return;
            case SessionFSM.Started:
                if (sessionStepIndex >= sessionTemplate.Length) {
                    sessionIndex++;
                    if (sessionIndex >= tracks.Length) {
                        Awake();
                        return;
                    }
                    sessionStepIndex = 0;
                    Inject();
                    session.Check();
                }
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
                state = SessionFSM.Waiting;
            break;
        }
    }

    private void Inject() {
        string trackNumber;
        Track trackToBeInjected;
        
        session = new(sessionTemplate);

        if (tracks.Length == 0) {
            trackToBeInjected = null;
            trackNumber = "";
        } else {
            trackToBeInjected = tracks[sessionIndex];
            if (customNumbering) {
                trackNumber = numbering[sessionIndex];
            } else {
                trackNumber = sessionIndex.ToString();
            }
        }

        session.InjectData(commonName, commonTrainer, trackToBeInjected, trackNumber);
    }

    private IEnumerator Execute() {
        switch (session[sessionStepIndex].stepType) {
            case SessionStepType.Training:
                TrainingSession.SetupTrainingScene(session[sessionStepIndex].trainingSettings);
                LaunchTrainer(session.GetCondaScript(),
                                session[sessionStepIndex].trainingSettings.trainer,
                                session[sessionStepIndex].trainingSettings.runId,
                                session[sessionStepIndex].trainingSettings.initializeFrom);
                yield return EditorCoroutineUtility.StartCoroutine(DelayedEnterPlaymode(10f), this);
            break;
            case SessionStepType.Evaluation:
                internalEvaluationNumber = session[sessionStepIndex].evaluationSettings.GetFileCount();
                TrainingSession.SetupEvaluationScene(session[sessionStepIndex].evaluationSettings, internalEvaluationIndex);
                EditorApplication.EnterPlaymode();
            break;
        }
    }

    private void ManageStateChange(PlayModeStateChange stateChange) {
        switch (stateChange) {
            case PlayModeStateChange.ExitingEditMode:
                if (state == SessionFSM.Waiting) {
                    switch (session[sessionStepIndex].stepType) {
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
                    MoveTrainedModel(session[sessionStepIndex].trainingSettings.runId);
                    sessionStepIndex++;
                    state = SessionFSM.Started;
                } else if (state == SessionFSM.Evaluating) {
                    internalEvaluationIndex++;  
                    if (internalEvaluationIndex >= internalEvaluationNumber) {
                        sessionStepIndex++;
                        internalEvaluationIndex = 0;
                    }
                    state = SessionFSM.Started;
                }
            break;
        }
    }

    private string GetLabel(int i) {
        if (i == sessionIndex && state != SessionFSM.Stopped) {
            return "▶";
        } else {
            return "";
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