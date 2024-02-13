using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine;
using KartGame.Custom.Demo;
using KartGame.KartSystems;
using KartGame.Custom.AI;
using System.IO;
using Unity.VisualScripting;
using Unity.MLAgents;
using UnityEditor;

namespace KartGame.Custom {

    public class ModelEvaluator : MonoBehaviour
    {
        [SerializeField] private string demoName;
        [SerializeField] private ArcadeKart MLAgentPrefab;
        [SerializeField] private ModelAsset MLAgentTrainedModel;
        [SerializeField] private Track track;

        [SerializeField] private float evaluationTimeScale;

        [Tooltip("Number of sub-trajectories to evaluate")]
        [SerializeField] private int splitAmount;
        [Tooltip("Length (in timesteps) of each sub-trajectory")]
        [SerializeField] private int splitDuration;

        [SerializeField] private Color originalSubtrajectoryColor; 
        [SerializeField] private Color agentSubtrajectoryColor;
        [SerializeField] private bool drawOriginalFullTrajectory;
        [SerializeField] private Color originalTrajectoryColor;
        
        private string filepath;
        private ArcadeKart MLAgent;
        private KartAgent kartAgent;
        private StateRecorder MLASR;
        
        private float oldTimeScale;
        private int splitTimer;
        private bool isEvaluating;

        private Trajectory originalTrajectory;

        private Trajectory[] subTrajectories;
        private Trajectory[] AISubtrajectories;
        private Vector3[] lastVelocities;
        private float[] evaluations;
        private int subtrajectoryIndex = 0;

        private void Awake() {
            filepath = Application.persistentDataPath + "/demos/";
            
            if (!StatePlayer.CheckValidFile(Path.Join(filepath, demoName))) {
                enabled = false;
                return;
            }
            originalTrajectory = new Trajectory(StatePlayer.ReadFromFile(Path.Join(filepath, demoName)));
            
            subTrajectories = new Trajectory[splitAmount];
            AISubtrajectories = new Trajectory[splitAmount];
            lastVelocities = new Vector3[splitAmount];
            evaluations = new float[splitAmount];
        }

        private void Start() {
            var empty = new GameObject();
            empty.transform.parent = transform.parent;
            empty.SetActive(false);
            MLAgent = Instantiate(MLAgentPrefab, empty.transform);

            kartAgent = MLAgent.GetComponent<KartAgent>();
            kartAgent.Mode = AgentMode.Inferencing;
            kartAgent.Track = track;

            DecisionRequester MLADR = MLAgent.GetComponent<DecisionRequester>();
            MLADR.DecisionPeriod = 1;

            MLASR = MLAgent.AddComponent<StateRecorder>();
            MLASR.toDisk = false;
            MLASR.OnWriteQueue += (queue) => {
                Queue<StateData> tQueue = new();
                tQueue.Enqueue(subTrajectories[subtrajectoryIndex].points[0]);  //First point is skipped for some re
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

        private void GenerateSubtrajectories() {
            for (int s=0; s<splitAmount; s++) {
                StateData[] t = new StateData[splitDuration];
                int index = s * originalTrajectory.points.Length/splitAmount - 1;       //Last point of previous split
                Vector3 lastPos = originalTrajectory.points[index < 0 ? 0 : index].position;    //If this is the first split, lastPos = 0
                for (int d=0; d<splitDuration; d++) {
                    index++;
                    if (index < originalTrajectory.points.Length) {                     //Take splitDuration points from original trajectory
                        t[d] = originalTrajectory.points[index];
                    } else {                                                            //If partial trajectory, discard, break
                        t = null;
                        break;
                    }
                }
                if (t == null) return;
                subTrajectories[s] = new Trajectory(t);

                lastVelocities[s] = (t[0].position - lastPos)/Time.fixedDeltaTime;
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
                kartAgent.OnEpisodeBegin(track.GetNextCheckpoint(subTrajectories[subtrajectoryIndex].points[0].position),
                                        subTrajectories[subtrajectoryIndex].points[0].position,
                                        subTrajectories[subtrajectoryIndex].points[0].rotation,
                                        lastVelocities[subtrajectoryIndex]);
                isEvaluating = true;
            } else {
                Destroy(MLAgent.gameObject);
                Time.timeScale = oldTimeScale;
                Evaluate();
                enabled = false;
            }
        }

        private void Evaluate() {
            for (int s = 0; s < splitAmount; s++) {
                evaluations[s] = Trajectory.Evaluate(subTrajectories[s], AISubtrajectories[s]);
            }
            Debug.Log($"Evaluated {splitAmount} subtrajectories of {splitDuration*Time.fixedDeltaTime*1000} ms each. Average dissimilarity: {evaluations.Average()}");
        }

        private void FixedUpdate() {
            if (isEvaluating) {
                splitTimer++;
                if (splitTimer >= splitDuration) {
                    isEvaluating = false;
                    MLASR.enabled = false;
                    kartAgent.EndEpisode();
                }
            }
        }
    }
}