namespace KartGame.Custom {
    /// <summary>
    /// A simple objective to allow for free roaming until a checkpoint is reached.
    /// </summary>
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