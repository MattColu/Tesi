using UnityEngine;

namespace KartGame.Custom.Demo
{
    public struct StateData {
        public Vector3 position;
        public Quaternion rotation;

        public StateData(Vector3 pos, Quaternion rot) {
            position = pos;
            rotation = rot;
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
            Record(new StateData(kart.transform.position, kart.transform.rotation));
        }
    }
}