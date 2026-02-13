using System;
using UnityEngine;

public abstract class Staff : Person
{
    public int salaryPerMinute; // Set from the scriptable object
    public StaffMember staffType;
    
    private void Start()
    {
        InvokeRepeating("GetPaid", 60f, 60f); // Pay every 60 seconds
    }

    protected override void OnTick()
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
    
    private void GetPaid()
    {
        StaffManager.Instance.PaySalary(this);
    }
}