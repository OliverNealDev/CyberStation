using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Staff : Person
{
    public StaffMasterState masterState = StaffMasterState.Idle;
    public enum StaffMasterState
    {
        Idle,
        MovingToTarget,
        InteractingWithTarget
    }
}
