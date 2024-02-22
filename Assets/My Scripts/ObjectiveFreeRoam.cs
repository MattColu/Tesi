using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveFreeRoam : Objective
{
    protected override void ReachCheckpoint(int remaining) {
        if (isCompleted) return;
        
    }
}
