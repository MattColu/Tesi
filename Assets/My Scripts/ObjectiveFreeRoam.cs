using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
