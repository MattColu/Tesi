using KartGame.KartSystems;

namespace KartGame.Custom.Demo
{

    public class InputPlayer : Player<InputData>, IInput
    {
        private InputData lastInput = default;

        protected override void Awake() {
            base.Awake();
            demoType = "input";
        }

        protected override void ExecuteStep(InputData queueElement) {
            lastInput = queueElement;
        }

        protected override void Cleanup() {
            lastInput = default;
            Destroy(this);
        }

        public InputData GenerateInput() {
            return lastInput;
        }
    }
}