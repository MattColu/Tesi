#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using KartGame.Custom.AI;
using KartGame.KartSystems;
using Unity.Sentis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace KartGame.Custom.Training {
    [Serializable]
    public struct SessionStep {
        public SessionStepType stepType;
        public TrainingSettings trainingSettings;
        public EvaluationSettings evaluationSettings;
    }

    [Serializable]
    public enum SessionStepType {
        Training,
        Evaluation
    }
    
    [Serializable]
    public struct TrainingSettings {
        public Track track;
        public int trackInstances;
        public KartAgent agent;
        public int agentInstances;
        public string trainer;
        public string runId;
        public string initializeFrom;

        public override readonly string ToString() {
            return $"Track: {track.name} - {trackInstances} instances\nAgent: {agent.name} - {agentInstances} instances\nTrainer: {trainer}\nRunId: {runId}";
        }
    }

    [Serializable]
    public struct EvaluationSettings {
        public string demoFolder;
        public string modelRunId;
        public int numberOfEvaluations;
        public int splitAmount;
        public int splitLength;

        public override readonly string ToString() {
            return $"Demo Folder: {demoFolder}\nModel Run Id: {modelRunId}\nSplit Amount: {splitAmount}\nSplit Length: {splitLength}";
        }
    }

    [Serializable]
    public struct TrainingSession: IEnumerable<SessionStep> {
        public SessionStep[] steps;
        private string condaStartScript;

        public int Length {
            get {
                if (steps == null) {
                    return 0;
                } else {
                    return steps.Length;
                }
            }
        }

        public SessionStep this[int index] {
            get => steps[index];
            set => steps[index] = value;
        }

        public TrainingSession(SessionStep[] steps, string condaStartScript) {
            this.steps = steps;
            this.condaStartScript = condaStartScript;
        }

        public TrainingSession(TrainingSession other) {
            this.steps = (SessionStep[]) other.steps.Clone();
            this.condaStartScript = other.condaStartScript;
        }

        public bool Check() {
            if (condaStartScript == "") {
                condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
            }
            if (condaStartScript == "") throw new ArgumentNullException("Conda activation script");

            for (int i = 0; i < steps.Length; i++) {
                if (steps[i].stepType == SessionStepType.Training) {
                    if (steps[i].trainingSettings.agent == null) {
                        steps[i].trainingSettings.agent = (KartAgent)DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultAgent").objectReferenceValue;
                    }
                    if (steps[i].trainingSettings.trackInstances == 0) {
                        steps[i].trainingSettings.trackInstances = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultTrackInstances").intValue;
                    }
                    if (steps[i].trainingSettings.agentInstances == 0) {
                        steps[i].trainingSettings.agentInstances = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultAgentInstances").intValue;
                    }
                    if (steps[i].trainingSettings.trainer == "") {
                        steps[i].trainingSettings.trainer = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultTrainer").stringValue;
                    }

                    if (steps[i].trainingSettings.track == null) throw new ArgumentNullException($"Step {i} Track");
                    if (steps[i].trainingSettings.agent == null) throw new ArgumentNullException($"Step {i} Agent");
                    if (steps[i].trainingSettings.trackInstances == 0) throw new ArgumentNullException($"Step {i} Track Instances");
                    if (steps[i].trainingSettings.agentInstances == 0) throw new ArgumentNullException($"Step {i} Agent Instances");
                    if (steps[i].trainingSettings.trainer == "") throw new ArgumentNullException($"Step {i} Trainer");
                    if (steps[i].trainingSettings.runId == "") throw new ArgumentNullException($"Step {i} RunID");
                    
                    if (steps[i].trainingSettings.initializeFrom != "") {
                        if (!Directory.Exists(Path.Join($"{Directory.GetParent(Application.dataPath)}/Training/results", steps[i].trainingSettings.initializeFrom))) {
                            string nameToMatch = steps[i].trainingSettings.initializeFrom;
                            if (!steps[..i].Any(step => step.trainingSettings.runId == nameToMatch)) {
                                throw new DirectoryNotFoundException($"Step {i} Initialize from {steps[i].trainingSettings.initializeFrom}");
                            }
                        } 
                    }
                    if (!File.Exists(Path.Join($"{Directory.GetParent(Application.dataPath)}/Training/trainers", steps[i].trainingSettings.trainer))) throw new FileNotFoundException($"Step {i} Trainer: {steps[i].trainingSettings.trainer}");
                    if (!File.Exists(condaStartScript)) throw new FileNotFoundException($"Conda activation script: {condaStartScript}");
                
                } else {

                    if (steps[i].evaluationSettings.numberOfEvaluations == 0) {
                        steps[i].evaluationSettings.numberOfEvaluations = DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultNumberOfEvaluations").intValue;
                    }
                    if (steps[i].evaluationSettings.splitAmount == 0) {
                        steps[i].evaluationSettings.splitAmount = DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultSplitAmount").intValue;
                    }
                    if (steps[i].evaluationSettings.splitLength == 0) {
                        steps[i].evaluationSettings.splitLength = DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultSplitLength").intValue;
                    }
                    if (steps[i].evaluationSettings.demoFolder == "") throw new ArgumentNullException($"Step {i} Demo File");
                    if (steps[i].evaluationSettings.modelRunId == "") throw new ArgumentNullException($"Step {i} Model Run Id");
                    if (steps[i].evaluationSettings.numberOfEvaluations == 0) throw new ArgumentNullException($"Step {i} Number of Evaluations");
                    if (steps[i].evaluationSettings.splitAmount == 0) throw new ArgumentNullException($"Step {i} Split Amount");
                    if (steps[i].evaluationSettings.splitLength == 0) throw new ArgumentNullException($"Step {i} Split Length");
                    
                    if (!Directory.Exists($"{Directory.GetParent(Application.dataPath)}/Training/demos/replays/{steps[i].evaluationSettings.demoFolder}")) throw new DirectoryNotFoundException($"Step {i} Demo Folder: {steps[i].evaluationSettings.demoFolder}");
                    if (Directory.EnumerateFiles($"{Directory.GetParent(Application.dataPath)}/Training/demos/replays/{steps[i].evaluationSettings.demoFolder}", "*.state").Count() == 0) throw new FileLoadException($"Step {i} Demo Folder {steps[i].evaluationSettings.demoFolder} is empty");
                }
            }
            return true;
        }

        public string GetCondaScript() {
            if (condaStartScript == "") {
                condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
            }
            return condaStartScript;
        }

        public void ResetCondaScript() {
            condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
        }

        public void InjectData(string commonName = "", string commonTrainer = "", Track track = null, string trackNumber = "") {
            string commonPrefix = "";
            if (commonName != "") commonPrefix = commonName;
            if (trackNumber != "") commonPrefix += $"_Track{trackNumber}";
            for (int i = 0; i < steps.Length; i++) {
                switch (steps[i].stepType) {
                    case SessionStepType.Training:
                        if (track != null) steps[i].trainingSettings.track = track;
                        
                        if (commonTrainer != "") {
                            steps[i].trainingSettings.trainer = AddSuffix(commonTrainer, trackNumber);
                        } else {
                            steps[i].trainingSettings.trainer = AddSuffix(steps[i].trainingSettings.trainer, trackNumber);
                        }

                        steps[i].trainingSettings.runId = AddPrefix(commonPrefix, steps[i].trainingSettings.runId);
                        
                        if (steps[i].trainingSettings.initializeFrom != "") {
                            steps[i].trainingSettings.initializeFrom = AddPrefix(commonPrefix, steps[i].trainingSettings.initializeFrom);
                        }
                    break;
                    case SessionStepType.Evaluation:
                        if (trackNumber != "") steps[i].evaluationSettings.demoFolder = trackNumber;
                        steps[i].evaluationSettings.modelRunId = AddPrefix(commonPrefix, steps[i].evaluationSettings.modelRunId);
                    break;
                }
            }
        }

        private string AddPrefix(string prefix, string value) {
            if (prefix == "" || value == "") return value;
            if (value.StartsWith(prefix)) return value;
            return prefix + "_" + value;
        }

        private string AddSuffix(string value, string suffix) {
            if (value == "" || suffix == "") return value;
            string[] splitValue = value.Split(".");
            if (splitValue[0].EndsWith(suffix)) return value;
            return splitValue[0] + "_" + suffix + "." + string.Join('.', splitValue[1..]);
        }

        public static TrainingSession FromFile(string file) {
            TrainingSession s = new();
            object boxedS = s;          //https://docs.unity3d.com/2023.2/Documentation/ScriptReference/EditorJsonUtility.FromJsonOverwrite.html
            try {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(file), boxedS);
            } catch (Exception e) {
                Debug.LogError($"Error reading from \"{file}\": {e}");
            }
            return (TrainingSession)boxedS;
        }

        public readonly void ToFile(string file) {
            try {
                File.WriteAllText(file, EditorJsonUtility.ToJson(this));
            } catch (Exception e) {
                Debug.LogError($"Error writing to \"{file}\": {e}");
                return;
            }
        }

        public IEnumerator<SessionStep> GetEnumerator() {
            return ((IEnumerable<SessionStep>)steps).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        
        public static Track[] InstantiateTracks(TrainingSettings settings) {
            Track[] instantiatedTracks = new Track[settings.trackInstances];
            Vector3 trackBounds = settings.track.GetBoundingBox().size;
            int side = Mathf.CeilToInt(Mathf.Sqrt(settings.trackInstances));
            for (int row = 0, index = 0; row < side && index < settings.trackInstances; row++) {
                for (int col = 0; col < side && index < settings.trackInstances; col++, index++) {
                    instantiatedTracks[index] = GameObject.Instantiate(settings.track, new Vector3(col * trackBounds.x, 0, row * trackBounds.z), Quaternion.identity);
                    instantiatedTracks[index].name = $"{settings.track.name} {index}";
                    instantiatedTracks[index].SetSpawnpoint(); 
                }
            }
            return instantiatedTracks;
        }

        public static KartAgent[] InstantiateKarts(TrainingSettings settings, Track[] instantiatedTracks) {
            KartAgent[] instantiatedKarts = new KartAgent[settings.trackInstances * settings.agentInstances];
            for (int t = 0; t < settings.trackInstances; t++) {
                Track track = instantiatedTracks[t];
                Transform spawnpoint = track.GetSpawnpoint();
                
                for (int i = 0; i < settings.agentInstances; i++) {
                    KartAgent instance = GameObject.Instantiate(settings.agent, spawnpoint.position, spawnpoint.rotation);
                    instance.name = $"{settings.agent.name} {t}-{i}";
                    instance.GetComponent<KartAgent>().Track = track;
                }
            }
            return instantiatedKarts;
        }

        public static void SetupTrainingScene(TrainingSettings settings) {
            EditorSceneManager.OpenScene("Assets/Scenes/Training.unity");   //Opens scene if not currently open, reloads scene otherwise
            Track[] instantiatedTracks = InstantiateTracks(settings);
            InstantiateKarts(settings, instantiatedTracks);
        }

        public static void SetupEvaluationScene(EvaluationSettings settings) {
            string demoFolder = $"{Directory.GetParent(Application.dataPath)}/Training/demos/replays/{settings.demoFolder}";
            string modelPath = $"Assets/ML-Agents/Trained Models/{settings.modelRunId}.onnx";
            ModelAsset model = AssetDatabase.LoadAssetAtPath<ModelAsset>(modelPath);
            if (model == null) throw new FileNotFoundException(modelPath);
            float evaluationTimescale = DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultTimescale").floatValue;
            SetupEvaluationScene(demoFolder,
                                model,
                                settings.numberOfEvaluations,
                                evaluationTimescale,
                                settings.splitAmount,
                                settings.splitLength);
        }

        public static void SetupEvaluationScene(string demoFolder, ModelAsset model, int numberOfEvaluations, float evaluationTimeScale, int splitAmount, int splitLength, Color? originalSubtrajectoryColor = null, Color? agentSubtrajectoryColor = null, bool drawOriginalFullTrajectory = false, Color? originalTrajectoryColor = null, bool standalone = false) {
            if (model == null) throw new NullReferenceException("Model is null");
            foreach (string demoFile in Directory.EnumerateFiles(demoFolder, "*.state")) {
                if (Replay.SetupAndOpenReplayScene(demoFile, replay: false)) {
                    DestroyKarts();
                    
                    Track trackInstance = GameObject.FindObjectOfType<Track>();
                    GameObject.DestroyImmediate(GameObject.FindObjectOfType<CinemachineVirtualCamera>().gameObject);
                    GameObject.DestroyImmediate(GameObject.FindObjectOfType<CinemachineBrain>());
                    
                    KartAgent agentPrefab = (KartAgent) DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultAgent").objectReferenceValue;
                    ModelEvaluator evaluatorPrefab = (ModelEvaluator) DefaultEvaluationSettings.GetSerializedSettings().FindProperty("m_DefaultEvaluator").objectReferenceValue;
                    
                    if (evaluatorPrefab == null) throw new FileNotFoundException("Default Evaluator Prefab was not defined");
                    
                    InstantiateEvaluator(evaluatorPrefab,
                                        agentPrefab,
                                        demoFile,
                                        model,
                                        trackInstance,
                                        numberOfEvaluations,
                                        evaluationTimeScale,
                                        splitAmount,
                                        splitLength,
                                        originalSubtrajectoryColor,
                                        agentSubtrajectoryColor,
                                        drawOriginalFullTrajectory,
                                        originalTrajectoryColor,
                                        standalone);
                }
            }
        }

        public static void DestroyKarts() {
            foreach (var kart in GameObject.FindObjectsOfType<ArcadeKart>()) {
                GameObject.DestroyImmediate(kart.gameObject);
            }
        }

        public static ModelEvaluator InstantiateEvaluator(ModelEvaluator evaluatorPrefab, KartAgent agentPrefab, string demoFilepath, ModelAsset model, Track trackInstance, int numberOfEvaluations, float evaluationTimescale, int splitAmount, int splitDuration, Color? originalSubtrajectoryColor = null, Color? agentSubtrajectoryColor = null, bool drawOriginalFullTrajectory = false, Color? originalTrajectoryColor = null, bool standalone = false) {
            var empty = new GameObject();
            empty.SetActive(false);
            ModelEvaluator evaluatorInstance = GameObject.Instantiate(evaluatorPrefab, empty.transform);
            evaluatorInstance.Setup(demoFilepath,
                                    agentPrefab,
                                    model,
                                    trackInstance,
                                    numberOfEvaluations,
                                    evaluationTimescale,
                                    splitAmount,
                                    splitDuration,
                                    originalSubtrajectoryColor,
                                    agentSubtrajectoryColor,
                                    drawOriginalFullTrajectory,
                                    originalTrajectoryColor,
                                    standalone);
            empty.SetActive(true);
            evaluatorInstance.transform.parent = empty.transform.parent;
            GameObject.DestroyImmediate(empty);
            return evaluatorInstance;
        }
    }
}
#endif