using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace KartGame.Custom.Demo
{
    [Serializable]
    public struct StateData: ISerializable {

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

        public StateData(SerializationInfo info, StreamingContext context) {
            position = new Vector3(info.GetSingle("posx"), info.GetSingle("posy"), info.GetSingle("posz"));
            rotation = new Quaternion(info.GetSingle("rotx"), info.GetSingle("roty"), info.GetSingle("rotz"), info.GetSingle("rotw"));
            velocity = new Vector3(info.GetSingle("velx"), info.GetSingle("vely"), info.GetSingle("velz"));
            angularVelocity = new Vector3(info.GetSingle("angvelx"), info.GetSingle("angvely"), info.GetSingle("angvelz"));
        }

        public readonly void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("posx", position.x);
            info.AddValue("posy", position.y);
            info.AddValue("posz", position.z);
            info.AddValue("rotx", rotation.x);
            info.AddValue("roty", rotation.y);
            info.AddValue("rotz", rotation.z);
            info.AddValue("rotw", rotation.w);
            info.AddValue("velx", velocity.x);
            info.AddValue("vely", velocity.y);
            info.AddValue("velz", velocity.z);
            info.AddValue("angvelx", angularVelocity.x);
            info.AddValue("angvely", angularVelocity.y);
            info.AddValue("angvelz", angularVelocity.z);
        }

        public override readonly string ToString() {
            return $"Pos: {position} Rot: {rotation} Vel: {velocity} AngVel: {angularVelocity}";
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