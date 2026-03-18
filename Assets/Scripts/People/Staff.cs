using System;
using UnityEngine;

public abstract class Staff : Person
{
    public int salaryPerMinute; 
    public StaffMember staffType;
    
    private void Start()
    {
        InvokeRepeating("GetPaid", 60f, 60f); 
    }

    protected override void OnTick(float tickLength)
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
    
    private void GetPaid()
    {
        StaffManager.Instance.PaySalary(this);
    }
}