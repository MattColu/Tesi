
using KartGame.Custom.AI;
using UnityEngine;

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
