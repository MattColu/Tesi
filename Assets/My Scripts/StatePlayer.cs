namespace KartGame.Custom.Demo {
    public class StatePlayer : Player<StateData>
    {
        protected override void Awake() {
            base.Awake();
            demoType = "state";
        }

        protected override void OnEnable() {
            base.OnEnable();
            kart.SetCanMove(false);
        }

        protected override void ExecuteStep(StateData queueElement) {
            base.ExecuteStep(queueElement);
            kart.Rigidbody.position = queueElement.position;
            kart.Rigidbody.rotation = queueElement.rotation;
            kart.Rigidbody.velocity = queueElement.velocity;
            kart.Rigidbody.angularVelocity = queueElement.angularVelocity;
        }

        protected override void Cleanup() {
            base.Cleanup();
            kart.Rigidbody.velocity = default;
            kart.Rigidbody.angularVelocity = default;
            kart.SetCanMove(true);
        }
    }
}