using System;
using UnityEngine;

public abstract class Staff : Person
{
    public int salaryPerMinute; 
    public StaffMember staffType;

    protected override void OnTick(float tickLength)
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
}
