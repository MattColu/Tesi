namespace KartGame.Custom.Demo {
    public class StatePlayer : Player<StateData>
    {
        protected override void Awake() {
            base.Awake();
            demoType = "state";
        }

        protected override void Start() {
            base.Start();
            kart.SetCanMove(false);
        }

        protected override void ExecuteStep(StateData queueElement) {
            base.ExecuteStep(queueElement);
            kart.Rigidbody.position = queueElement.position;
            kart.Rigidbody.rotation = queueElement.rotation;
        }

        protected override void Cleanup() {
            base.Cleanup();
            kart.SetCanMove(true);
        }
    }
}