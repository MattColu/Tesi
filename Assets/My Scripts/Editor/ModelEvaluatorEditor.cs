using System;
using System.IO;
using KartGame.Custom;
using KartGame.Custom.AI;
using KartGame.Custom.Training;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A standalone implementation of the evaluation system.
/// <para>
/// Unlike an Evaluation Step from <see cref="TrainingSessionEditor"/>, this does not save to file and results persist until <c>Play Mode</c> is stopped.
/// </para>
/// </summary>
public class ModelEvaluatorEditor: EditorWindow
{
    private ModelEvaluator evaluatorPrefab;
    private KartAgent agentPrefab;
    private ModelAsset model;

    private int numberOfEvaluations;

    private float evaluationTimeScale;

    [Tooltip("Number of sub-trajectories to evaluate")]
    private int splitAmount;
    [Tooltip("Length (in timesteps) of each sub-trajectory")]
    private int splitDuration;

    private Color originalSubtrajectoryColor; 
    private Color agentSubtrajectoryColor;
    private bool drawOriginalFullTrajectory;
    private Color originalTrajectoryColor;

    private string modelName;
    private string demoFilepath;
    
    [MenuItem ("Kart/Evaluate Model", priority = 11)]
    public static void ShowWindow() {
        GetWindow(typeof(ModelEvaluatorEditor));
    }
    
    void OnGUI () {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Demo File", demoFilepath);
        if (GUILayout.Button("Browse")) {
            demoFilepath = EditorUtility.OpenFilePanel("Choose a demo file", "", "state");
        }
        EditorGUILayout.EndHorizontal();
        modelName = EditorGUILayout.TextField("Model Run Id", modelName);

        numberOfEvaluations = EditorGUILayout.IntField("Number of Evaluations", numberOfEvaluations);
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
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            TrainingSession.SetupEvaluationScene(
                demoFilepath,
                0,
                model,
                numberOfEvaluations,
                evaluationTimeScale,
                splitAmount,
                splitDuration,
                originalSubtrajectoryColor,
                agentSubtrajectoryColor,
                drawOriginalFullTrajectory,
                originalTrajectoryColor,
                true);
            EditorApplication.EnterPlaymode();
        }
    }

    void CheckInput() {
        if (!File.Exists($"{Application.dataPath}/ML-Agents/Trained Models/{modelName}.onnx")) throw new ArgumentNullException("Model");
        evaluatorPrefab = (ModelEvaluator) DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultEvaluator").objectReferenceValue;
        agentPrefab = (KartAgent) DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultAgent").objectReferenceValue;
        model = AssetDatabase.LoadAssetAtPath<ModelAsset>($"Assets/ML-Agents/Trained Models/{modelName}.onnx");
    }
}