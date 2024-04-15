using System;
using System.IO;
using Cinemachine;
using KartGame.Custom;
using KartGame.Custom.AI;
using KartGame.Custom.Training;
using KartGame.KartSystems;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;

public class TrainingEvaluator: EditorWindow
{
    private ModelEvaluator evaluatorPrefab;
    private KartAgent agentPrefab;
    private ModelAsset model;
    private Track track;

    private float evaluationTimeScale;

    [Tooltip("Number of sub-trajectories to evaluate")]
    private int splitAmount;
    [Tooltip("Length (in timesteps) of each sub-trajectory")]
    private int splitDuration;

    private Color originalSubtrajectoryColor; 
    private Color agentSubtrajectoryColor;
    private bool drawOriginalFullTrajectory;
    private Color originalTrajectoryColor;

    private ModelEvaluator evaluatorInstance;
    private Track trackInstance;
    private string modelName;
    private string demoFilepath;
    
    [MenuItem ("MLAgents/Evaluate Model", priority = 11)]
    public static void ShowWindow() {
        GetWindow(typeof(TrainingEvaluator));
    }
    
    void OnGUI () {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Demo File", demoFilepath);
        if (GUILayout.Button("Browse")) {
            demoFilepath = EditorUtility.OpenFilePanel("Choose a demo file", "", "state");
        }
        EditorGUILayout.EndHorizontal();
        modelName = EditorGUILayout.TextField("Model Run Id", modelName);
        evaluationTimeScale = EditorGUILayout.FloatField("Evaluation Timescale", evaluationTimeScale);
        splitAmount = EditorGUILayout.IntField("Number of Splits", splitAmount);
        splitDuration = EditorGUILayout.IntField("Duration of a Split", splitDuration);
        
        EditorGUILayout.Separator();
        
        originalSubtrajectoryColor = EditorGUILayout.ColorField("Original Subtrajectory", originalSubtrajectoryColor);
        agentSubtrajectoryColor = EditorGUILayout.ColorField("Agent Subtrajectory", agentSubtrajectoryColor);
        
        drawOriginalFullTrajectory = EditorGUILayout.BeginToggleGroup("Draw Entire Original Trajectory", drawOriginalFullTrajectory);
        originalTrajectoryColor = EditorGUILayout.ColorField("Original Trajectory", originalTrajectoryColor);
        EditorGUILayout.EndToggleGroup();

        if (GUILayout.Button("Evaluate")) {
            CheckInput();
            SetupEvaluationScene();
            EditorApplication.EnterPlaymode();
        }
    }

    void CheckInput() {
        if (!File.Exists($"{Application.dataPath}/ML-Agents/Training/results/{modelName}/Kart.onnx")) throw new ArgumentNullException("Model");
        evaluatorPrefab = AssetDatabase.LoadAssetAtPath<ModelEvaluator>("Assets/My Prefabs/Model Evaluator.prefab");
        agentPrefab = (KartAgent)DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultAgent").objectReferenceValue;
    }

    void SetupEvaluationScene() {
        if (Replay.SetupAndOpenReplayScene(demoFilepath, replay: false)) {
            DestroyKarts();
            trackInstance = FindObjectOfType<Track>();
            DestroyImmediate(FindObjectOfType<CinemachineVirtualCamera>().gameObject);
            DestroyImmediate(FindObjectOfType<CinemachineBrain>());
            model = AssetDatabase.LoadAssetAtPath<ModelAsset>($"Assets/ML-Agents/Training/results/{modelName}/Kart.onnx");
            InstantiateEvaluator();
        }
    }

    void DestroyKarts() {
        foreach (var kart in FindObjectsOfType<ArcadeKart>()) {
            DestroyImmediate(kart.gameObject);
        }
    }

    void InstantiateEvaluator() {
        var empty = new GameObject();
        empty.SetActive(false);
        evaluatorInstance = Instantiate(evaluatorPrefab, empty.transform);
        evaluatorInstance.Setup(demoFilepath,
                                agentPrefab,
                                model,
                                trackInstance,
                                evaluationTimeScale,
                                splitAmount,
                                splitDuration,
                                originalSubtrajectoryColor,
                                agentSubtrajectoryColor,
                                drawOriginalFullTrajectory,
                                originalTrajectoryColor);
        empty.SetActive(true);
        evaluatorInstance.transform.parent = empty.transform.parent;
        DestroyImmediate(empty);
    }
}