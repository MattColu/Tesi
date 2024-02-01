using KartGame.KartSystems;
using UnityEngine;

namespace KartGame.Custom.Demo
{
    public class InputRecorder : Recorder<InputData>
    {
        protected override void Awake() {
            base.Awake();
            demoType = "input";
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();
            Record(kart.Input);
        }
    }
}