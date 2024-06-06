#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine;
using KartGame.Custom.Demo;
using KartGame.Custom.AI;
using Unity.VisualScripting;
using Unity.MLAgents;
using UnityEditor;
using System.IO;

namespace KartGame.Custom {

    public class ModelEvaluator : MonoBehaviour
    {
        private struct EvaluationResult {
            public string fileName;
            public int numberOfEvaluations;
            public int splitAmount;
            public int splitLength;
            public float result;

            public EvaluationResult(string fileName, int numberOfEvaluations, int splitAmount, int splitLength, float result) {
                this.fileName = fileName;
                this.numberOfEvaluations = numberOfEvaluations;
                this.splitAmount = splitAmount;
                this.splitLength = splitLength;
                this.result = result;
            }
        }

        [SerializeField] private string demoFilepath;
        [SerializeField] private KartAgent MLAgentPrefab;
        [SerializeField] private ModelAsset MLAgentTrainedModel;
        [SerializeField] private Track track;

        [SerializeField] private int numberOfEvaluations;

        [SerializeField] private float evaluationTimeScale;

        [Tooltip("Number of sub-trajectories to evaluate")]
        [SerializeField] private int splitAmount;
        [Tooltip("Length (in timesteps) of each sub-trajectory")]
        [SerializeField] private int splitLength;

        [SerializeField] private Color originalSubtrajectoryColor; 
        [SerializeField] private Color agentSubtrajectoryColor;
        [SerializeField] private bool drawOriginalFullTrajectory;
        [SerializeField] private Color originalTrajectoryColor;
        
        private KartAgent MLAgent;
        private KartAgent kartAgent;
        private StateRecorder MLASR;
        
        private float oldTimeScale;
        private int splitTimer;
        private bool isEvaluating;
        private int evaluationCounter;
        [SerializeField, HideInInspector] private bool standalone;

        private Trajectory originalTrajectory;

        private Trajectory[] subTrajectories;
        private Trajectory[] AISubtrajectories;
        private float[] evaluations;
        private int subtrajectoryIndex;

        public void Setup(string demoFilepath, KartAgent MLAgentPrefab, ModelAsset MLAgentTrainedModel, Track track, int numberOfEvaluations, float evaluationTimeScale, int splitAmount, int splitLength, Color? originalSubtrajectoryColor = null, Color? agentSubtrajectoryColor = null, bool drawOriginalFullTrajectory = false, Color? originalTrajectoryColor = null, bool standalone = false) {
            this.demoFilepath = demoFilepath;
            this.MLAgentPrefab = MLAgentPrefab;
            this.MLAgentTrainedModel = MLAgentTrainedModel;
            this.track = track;
            this.numberOfEvaluations = numberOfEvaluations;
            this.evaluationTimeScale = evaluationTimeScale;
            this.splitAmount = splitAmount;
            this.splitLength = splitLength;
            this.originalSubtrajectoryColor = originalSubtrajectoryColor ?? Color.red; 
            this.agentSubtrajectoryColor = agentSubtrajectoryColor ?? Color.blue;
            this.drawOriginalFullTrajectory = drawOriginalFullTrajectory;
            this.originalTrajectoryColor = originalTrajectoryColor ?? Color.red;
            this.standalone = standalone;
        }

        private void Awake() {           
            if (!StatePlayer.CheckValidFile(demoFilepath)) {
                enabled = false;
                return;
            }

            evaluationCounter = 0;
            
            originalTrajectory = new Trajectory(StatePlayer.ReadFromFile(demoFilepath));
            evaluations = new float[numberOfEvaluations * splitAmount];
        }

        private void Start() {
            subtrajectoryIndex = 0;
            subTrajectories = new Trajectory[splitAmount];
            AISubtrajectories = new Trajectory[splitAmount];

            var empty = new GameObject();
            empty.transform.parent = transform.parent;
            empty.SetActive(false);
            MLAgent = Instantiate(MLAgentPrefab, empty.transform);

            kartAgent = MLAgent.GetComponent<KartAgent>();
            kartAgent.Mode = AgentMode.Evaluating;
            kartAgent.Track = track;

            DecisionRequester MLADR = MLAgent.GetComponent<DecisionRequester>();
            MLADR.DecisionPeriod = 1;

            MLASR = MLAgent.AddComponent<StateRecorder>();
            MLASR.toDisk = false;
            MLASR.OnWriteQueue += (queue) => {
                Queue<StateData> tQueue = new();
                tQueue.Enqueue(subTrajectories[subtrajectoryIndex].points[0]);  //First point is skipped for some reason
                AISubtrajectories[subtrajectoryIndex] = new(tQueue.Concat(queue).ToArray());
                subtrajectoryIndex++;
                InitEvaluation();
            };

            kartAgent.LazyInitialize();
            
            empty.SetActive(true);
            kartAgent.transform.parent = empty.transform.parent;
            Destroy(empty);

            GenerateSubtrajectories();
            FirstInit();
        }

        void OnDrawGizmosSelected() {
            if(Application.isPlaying) {
                if (drawOriginalFullTrajectory) {
                    originalTrajectory.Draw(originalTrajectoryColor);
                }
                for (int s = 0; s < subtrajectoryIndex; s++) {
                    subTrajectories[s].Draw(originalSubtrajectoryColor);
                    AISubtrajectories[s].Draw(agentSubtrajectoryColor);
                    Handles.Label(AISubtrajectories[s].points[0].position + 1f*Vector3.up, evaluations[s].ToString());
                }
            }
        }

        // Trajectory splitting formula:
        // - Subtract the length of a subtraj. from the overall traj. length 
        //   (Last subtraj. is now taken care of)
        // - Divide the remaining part of the traj. into splitAmount-1 parts
        // - Those are the starting points for the subtraj.s (first starts at 0)
        private void GenerateSubtrajectories() {
            int totalLength = originalTrajectory.points.Length;
            if (splitLength >= totalLength) throw new InvalidDataException($"Subtrajectory length ({splitLength}) is longer than full trajectory ({totalLength})");
            for (int s=0; s<splitAmount; s++) {
                StateData[] t = new StateData[splitLength];
                int index = s * (totalLength - splitLength)/(splitAmount - 1);  //Absolute index of first point of current split
                for (int d=0; d<splitLength; d++, index++) {
                    if (index < totalLength) {                                  //Take splitDuration points from original trajectory
                        t[d] = originalTrajectory.points[index];
                    } else {                                                    //If partial trajectory, discard subtrajectory (should not happen)
                        Debug.LogError($"Accessed index {index} out of {totalLength}");
                        splitAmount--;
                        return;
                    }
                }
                subTrajectories[s] = new Trajectory(t);
            }
        }

        private void FirstInit() {
            oldTimeScale = Time.timeScale;
            Time.timeScale = evaluationTimeScale;
            
            kartAgent.SetModel("Kart", MLAgentTrainedModel);
            BehaviorParameters MLABP = MLAgent.GetComponent<BehaviorParameters>();
            MLABP.BehaviorType = BehaviorType.InferenceOnly;
            MLABP.DeterministicInference = true;

            InitEvaluation();
        }

        private void InitEvaluation() {
            splitTimer = 0;
            if (subtrajectoryIndex < splitAmount) {
                MLASR.enabled = true;
                StateData initStateData = subTrajectories[subtrajectoryIndex].points[0];
                kartAgent.OnEpisodeBegin(track.GetNextCheckpoint(initStateData.position),
                                        initStateData.position,
                                        initStateData.rotation,
                                        initStateData.velocity,
                                        initStateData.angularVelocity);
                isEvaluating = true;
            } else {
                Destroy(MLAgent.gameObject);
                Time.timeScale = oldTimeScale;
                Evaluate();
                evaluationCounter++;
                if (evaluationCounter < numberOfEvaluations) {
                    Start();
                } else {
                    if (standalone) {
                        enabled = false;
                        Debug.Log($"Evaluated model \"{MLAgentTrainedModel.name}\" on {splitAmount} trajectories of {splitLength} timesteps each.\nAverage: {evaluations.Average()}");
                    } else {
                        SaveToFile();
                        EditorApplication.ExitPlaymode();
                    }
                }
            }
        }

        private void Evaluate() {
            int startingIndex = evaluationCounter * splitAmount;
            for (int i = 0; i < splitAmount; i++) {
                evaluations[startingIndex + i] = Trajectory.Evaluate(subTrajectories[i], AISubtrajectories[i]);
            }
        }

        private void FixedUpdate() {
            if (isEvaluating) {
                splitTimer++;
                if (splitTimer >= splitLength) {
                    isEvaluating = false;
                    MLASR.enabled = false;
                    kartAgent.EndEpisode();
                }
            }
        }

        private void SaveToFile() {
            string demoFile = Path.GetFileNameWithoutExtension(demoFilepath);
            string filePath = $"{Directory.GetParent(Application.dataPath)}/Training/results/{MLAgentTrainedModel.name}/evaluations.jsonl";
            EvaluationResult result = new(demoFile, numberOfEvaluations, splitAmount, splitLength, evaluations.Average());
            File.AppendAllLines(filePath, new[] {JsonUtility.ToJson(result)});
        }
    }
}
#endif