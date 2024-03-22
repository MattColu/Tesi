using KartGame.KartSystems;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using KartGame.Custom.Demo;
using Unity.MLAgents.Demonstrations;
using UnityEngine.SceneManagement;

namespace KartGame.Custom.AI
{
    /// <summary>
    /// We only want certain behaviours when the agent runs.
    /// Training would allow certain functions such as OnAgentReset() be called and execute, while Inferencing will
    /// assume that the agent will continuously run and not reset.
    /// </summary>
    public enum AgentMode
    {
        Training,
        Inferencing
    }

    /// <summary>
    /// The KartAgent will drive the inputs for the KartController.
    /// </summary>
    public class KartAgent : Agent, IInput
    {
#region Training Modes
        [Tooltip("Are we training the agent or is the agent production ready?")]
        public AgentMode Mode = AgentMode.Training;

#endregion

#region Senses
        [Header("Observation Params")]
        [Header("Track"), Tooltip("A reference to the track the agent is on")]
        public Track Track;

        [Space]
        [Tooltip("Would the agent need a custom transform to be able to raycast and hit the track? " +
            "If not assigned, then the root transform will be used.")]
        public Transform AgentSensorTransform;
#endregion

#region Rewards
        [Header("Rewards"), Tooltip("What penatly is given when the agent crashes?")]
        public float HitPenalty = -1f;
        [Tooltip("What penatly is given when the agent bumps into another agent?")]
        public float KartHitPenalty = -0.5f;
        [Tooltip("How much reward is given when the agent successfully passes the checkpoints?")]
        public float PassCheckpointReward;
        [Tooltip("Should typically be a small value, but we reward the agent for moving in the right direction.")]
        public float TowardsCheckpointReward;
        [Tooltip("Typically if the agent moves faster, we want to reward it for finishing the track quickly.")]
        public float SpeedReward;
        [Tooltip("Reward the agent when it keeps accelerating")]
        public float AccelerationReward;
        #endregion

        #region ResetParams
        [Header("Inference Reset Params")]
        [Tooltip("What is the unique mask that the agent should detect when it falls out of the track?")]
        public LayerMask OutOfBoundsMask;
        [Tooltip("What are the layers we want to detect for the track and the ground?")]
        public LayerMask TrackMask;
        [Tooltip("How far should the ray be when casted? For larger karts - this value should be larger too.")]
        public float GroundCastDistance;
#endregion

#region Debugging
        [Header("Debug Option")] [Tooltip("Should we visualize the rays that the agent draws?")]
        public bool ShowRaycasts;
#endregion

        ArcadeKart m_Kart;
        Rigidbody m_Rigidbody;
        KeyboardInput m_Keyboard;
        bool m_Acceleration;
        bool m_Brake;
        float m_Steering;

        bool m_EndEpisode;

        private int? lastCheckpoint = null;

        private int lapCount;

        public override void Initialize()
        {
            m_Kart = GetComponent<ArcadeKart>();
            m_Rigidbody = GetComponent<Rigidbody>();
            if (AgentSensorTransform == null) AgentSensorTransform = transform;
            m_Keyboard = GetComponent<KeyboardInput>();
            lapCount = 0;
        }

        void Start()
        {
            // If the agent is training, then at the start of the simulation, pick a random checkpoint to train the agent.
            OnEpisodeBegin();
        }

        void Update()
        {
            if (Input.GetButtonDown("Reset")) {
                Recover();
            }
            if (m_EndEpisode) {
                m_EndEpisode = false;
                EndEpisode();
                OnEpisodeBegin();
            }
        }

        void LateUpdate()
        {
            switch (Mode)
            {
                case AgentMode.Inferencing:
                    if (ShowRaycasts) 
                        Debug.DrawRay(transform.position, Vector3.down * GroundCastDistance, Color.cyan);
                    /*
                    // We want to place the agent back on the track if the agent happens to launch itself outside of the track.
                    if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, GroundCastDistance, TrackMask)
                        && ((1 << hit.collider.gameObject.layer) & OutOfBoundsMask) > 0)
                    {
                        // Reset the agent back to its last known agent checkpoint
                        var checkpoint = Track.Checkpoints[lastCheckpoint ?? 0].transform;
                        transform.localRotation = checkpoint.rotation;
                        transform.position = checkpoint.position;
                        m_Rigidbody.velocity = default;
                        m_Steering = 0f;
						m_Acceleration = m_Brake = false; 
                    }
                    */
                    break;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Checkpoint")) {
                int checkpointNumber = Array.IndexOf(Track.Checkpoints, other);
            
                if ((lastCheckpoint == null && checkpointNumber == 0) ||
                    (lastCheckpoint == Track.Checkpoints.Length - 1 && checkpointNumber == 0) ||
                    (lastCheckpoint == checkpointNumber - 1)) {
                    
                    lastCheckpoint = checkpointNumber;
                    AddReward(PassCheckpointReward);
                } else {
                    AddReward(-PassCheckpointReward);
                }
            }
        }

        private void OnCollisionEnter(Collision collision) {
            if (Mode == AgentMode.Training) {
                if (collision.collider.CompareTag("Wall")) {
                    AddReward(HitPenalty);
                    m_EndEpisode = true;
                } else if (collision.collider.CompareTag("Kart")) {
                    AddReward(KartHitPenalty);
                }
            }
        }

        private void OnCollisionStay(Collision collision) {
            if (Mode == AgentMode.Training) {
                m_EndEpisode = true;
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            int nextCheckpointNumber = (lastCheckpoint + 1) % Track.Checkpoints.Length ?? 0;
            Collider nextCheckpoint = Track.Checkpoints[nextCheckpointNumber];
            Vector3 toCheckpoint = nextCheckpoint.bounds.ClosestPoint(transform.position) - transform.position;
            
            if (ShowRaycasts) {
                Debug.DrawRay(nextCheckpoint.bounds.center, nextCheckpoint.transform.forward, Color.blue);
                Debug.DrawRay(transform.position, toCheckpoint.normalized, Color.green);
            }

            sensor.AddObservation(m_Kart.LocalSpeed()/m_Kart.GetMaxSpeed());        //1
            sensor.AddObservation(m_Rigidbody.rotation);                            //4
            sensor.AddObservation(toCheckpoint.normalized);                         //3
            sensor.AddObservation(Vector3.Dot(nextCheckpoint.transform.forward, transform.forward));//1
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);
            InterpretDiscreteActions(actions);

            // Find the next checkpoint when registering the current checkpoint that the agent has passed.
            var next = (lastCheckpoint + 1) % Track.Checkpoints.Length ?? 0;
            var nextCollider = Track.Checkpoints[next];
            var direction = (nextCollider.transform.position - m_Kart.transform.position).normalized;
            var reward = Vector3.Dot(m_Rigidbody.velocity.normalized, direction);

            if (ShowRaycasts) Debug.DrawRay(AgentSensorTransform.position, m_Rigidbody.velocity, Color.blue);

            // Add rewards if the agent is heading in the right direction
            AddReward(reward * TowardsCheckpointReward);
            AddReward((m_Acceleration && !m_Brake ? 1.0f : 0.0f) * AccelerationReward);
            AddReward(m_Kart.LocalSpeed() * SpeedReward);
        }

        public override void OnEpisodeBegin()
        {
            switch (Mode) {
                case AgentMode.Training:
                    lastCheckpoint = Random.Range(0, Track.Checkpoints.Length - 1);
                    var collider = Track.Checkpoints[(int)lastCheckpoint];
                    transform.localRotation = collider.transform.rotation;
                    transform.position = collider.transform.position;
                    m_Rigidbody.rotation = collider.transform.rotation;
                    m_Rigidbody.position = collider.transform.position;                    
                    break;
                case AgentMode.Inferencing:
                    lastCheckpoint = null;
                    Transform spawnpoint = Track.GetSpawnpoint();
                    transform.rotation = spawnpoint.rotation;
                    transform.position = spawnpoint.position;
                    m_Rigidbody.rotation = spawnpoint.rotation;
                    m_Rigidbody.position = spawnpoint.position;
                    break;
                default:
                    break;
            }
            m_Rigidbody.velocity = default;
            m_Rigidbody.angularVelocity = default;
            m_Acceleration = false;
            m_Brake = false;
            m_Steering = 0f;
        }

        public void OnEpisodeBegin(int nextCheckpoint, Vector3 position, Quaternion rotation, Vector3 initialVelocity, Vector3 initialAngVelocity) {
            lastCheckpoint = (nextCheckpoint - 1) % Track.Checkpoints.Length;
            transform.SetPositionAndRotation(position, rotation);
            m_Rigidbody.velocity = initialVelocity;
            m_Rigidbody.angularVelocity = initialAngVelocity;
            m_Acceleration = false;
            m_Brake = false;
            m_Steering = 0f;
        }

        void InterpretDiscreteActions(ActionBuffers actions)
        {
            m_Steering = actions.ContinuousActions[0];
            m_Acceleration = actions.DiscreteActions[0] == 1;
            m_Brake = actions.DiscreteActions[0] == 2;
        }

        public InputData GenerateInput()
        {
            return new InputData
            {
                Accelerate = m_Acceleration,
                Brake = m_Brake,
                TurnInput = m_Steering
            };
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            if (m_Keyboard != null) {
                InputData keyboardInput = m_Keyboard.GenerateInput();
                if (keyboardInput.Accelerate) actionsOut.DiscreteActions.Array[0] = 1;
                if (keyboardInput.Brake) actionsOut.DiscreteActions.Array[0] = 2;
                actionsOut.ContinuousActions.Array[0] = keyboardInput.TurnInput;
            }
        }

        void Recover() {
            Transform lastCheckpointTransform = Track.Checkpoints[lastCheckpoint??0].transform;
            transform.position = lastCheckpointTransform.position;
            transform.rotation = lastCheckpointTransform.rotation;
            m_Rigidbody.position = lastCheckpointTransform.position;
            m_Rigidbody.rotation = lastCheckpointTransform.rotation;
            m_Rigidbody.velocity = default;
            m_Rigidbody.angularVelocity = default;
        }

        public void SetupRecorders() {
            if (MenuOptions.Instance != null) {
                if (GetRecorders(out var stateRecorder, out var demonstrationRecorder, out var streamDemonstrationRecorder)) {
                    if (RESTManager.Instance != null) {
                        string trackName = SceneManager.GetActiveScene().name;

                        stateRecorder.toDisk = false;
                        streamDemonstrationRecorder = gameObject.AddComponent<StreamDemonstrationRecorder>();
                        stateRecorder.OnWriteQueue += queue => StartCoroutine(RESTManager.UploadRecording(StateRecorder.ToByteArray(queue), "state", trackName, lapCount));
                        streamDemonstrationRecorder.OnRecorderClosed += demo => StartCoroutine(RESTManager.UploadRecording(demo, "demo", trackName, lapCount));
                    } else {
                        string demoName = $"{MenuOptions.Instance.name}{MenuOptions.Instance.UID}/{Track.name}";
                        stateRecorder.userFilename = demoName;
                        demonstrationRecorder.DemonstrationName = demoName;
                    }
                }
            }
        }

        public void StartRecorders() {
            if (GetRecorders(out var stateRecorder, out var demonstrationRecorder, out var streamDemonstrationRecorder)) {
                stateRecorder.enabled = true;
                if (streamDemonstrationRecorder != null) {
                    streamDemonstrationRecorder.StartRecording();
                    demonstrationRecorder.Record = !streamDemonstrationRecorder.substitutesFileWriter;
                }
            } else {
                demonstrationRecorder.Record = true;
            }
        }

        public void StopRecorders() {
            if (GetRecorders(out var stateRecorder, out var demonstrationRecorder, out var streamDemonstrationRecorder)) {
                stateRecorder.enabled = false;
                if (streamDemonstrationRecorder != null) {
                    streamDemonstrationRecorder.enabled = false;
                }
                demonstrationRecorder.Record = false;
                demonstrationRecorder.Close();
            }
        }

        public void SplitRecorders() {
            if (GetRecorders(out var stateRecorder, out var demonstrationRecorder, out var streamDemonstrationRecorder)) {
                if (streamDemonstrationRecorder != null) {
                    streamDemonstrationRecorder.SplitRecording();
                }
                stateRecorder.Split();
                demonstrationRecorder.Close();
            }
            lapCount++;
        }

        public bool GetRecorders(out StateRecorder stateRecorder, out DemonstrationRecorder demonstrationRecorder, out StreamDemonstrationRecorder streamDemonstrationRecorder) {
            stateRecorder = GetComponent<StateRecorder>();
            demonstrationRecorder = GetComponent<DemonstrationRecorder>();
            streamDemonstrationRecorder = GetComponent<StreamDemonstrationRecorder>();
            return stateRecorder && demonstrationRecorder;
        }
    }
}
