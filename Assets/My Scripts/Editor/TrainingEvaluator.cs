using System;
using KartGame.Custom;
using KartGame.KartSystems;
using Unity.Sentis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TrainingEvaluator: EditorWindow
{
    private ModelEvaluator evaluatorPrefab;

    private string demoName;
    private ArcadeKart agentPrefab;
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
    
    [MenuItem ("MLAgents/Evaluate Model", priority = 11)]
    public static void ShowWindow() {
        GetWindow(typeof(TrainingEvaluator));
    }
    
    void OnGUI () {
        evaluatorPrefab = (ModelEvaluator) EditorGUILayout.ObjectField("Evaluator Prefab", evaluatorPrefab, typeof(ModelEvaluator), allowSceneObjects: false);
        demoName = EditorGUILayout.TextField("Demo Name", demoName);
        agentPrefab = (ArcadeKart) EditorGUILayout.ObjectField("Agent Prefab", agentPrefab, typeof(ArcadeKart), allowSceneObjects: false);
        model = (ModelAsset) EditorGUILayout.ObjectField("Trained Model", model, typeof(ModelAsset), allowSceneObjects: false);
        track = (Track) EditorGUILayout.ObjectField("Track", track, typeof(Track), allowSceneObjects: false);
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
        if (track == null) throw new ArgumentNullException("Track");
        if (model == null) throw new ArgumentNullException("Model");

    }

    void SetupEvaluationScene() {
        EditorSceneManager.OpenScene("Assets/Scenes/Evaluation.unity");   //Opens scene if not currently open, reloads scene otherwise
        InstantiateTrack();
        InstantiateEvaluator();
    }

    void InstantiateTrack() {
        trackInstance = Instantiate(track);
    }

    void InstantiateEvaluator() {
        var empty = new GameObject();
        empty.SetActive(false);
        evaluatorInstance = Instantiate(evaluatorPrefab, empty.transform);
        evaluatorInstance.Setup(demoName, agentPrefab, model, trackInstance, evaluationTimeScale, splitAmount, splitDuration, originalSubtrajectoryColor, agentSubtrajectoryColor, drawOriginalFullTrajectory, originalTrajectoryColor);
        empty.SetActive(true);
        evaluatorInstance.transform.parent = empty.transform.parent;
        DestroyImmediate(empty);
    }
}