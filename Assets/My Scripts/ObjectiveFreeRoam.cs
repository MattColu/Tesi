namespace KartGame.Custom {
    public class ObjectiveFreeRoam : Objective
    {
        private void Start() {
            Register();
        }
        protected override void ReachCheckpoint(int remaining) {
            if (isCompleted) return;
            CompleteObjective(string.Empty, "", "");
        }
    }
}