using UnityEngine;

namespace KartGame.Custom.Demo
{
    public struct StateData {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public StateData(Vector3 pos, Quaternion rot) {
            position = pos;
            rotation = rot;
            velocity = default;
            angularVelocity = default;
        }

        public StateData(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel) {
            position = pos;
            rotation = rot;
            velocity = vel;
            angularVelocity = angVel;
        }
    }

    public class StateRecorder : Recorder<StateData>
    {
        protected override void Awake() {
            base.Awake();
            demoType = "state";
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();
            Record(new StateData(kart.transform.position, kart.transform.rotation, kart.Rigidbody.velocity, kart.Rigidbody.angularVelocity));
        }
    }
}