using KartGame.Custom.AI;
using UnityEngine;

namespace KartGame.Custom {
    /// <summary>
    /// An extension of <see cref="ObjectiveCompleteLaps"/> that allows splitting recordings at each lap.
    /// </summary>
    public class ObjectiveCompleteLapsCustom : ObjectiveCompleteLaps
    {
        [SerializeField]
        private KartAgent agent;
        protected override void ReachCheckpoint(int remaining) {
            base.ReachCheckpoint(remaining);
            if (isCompleted) {
                return;
            }
            agent.SplitRecorders();
        }
    }
}