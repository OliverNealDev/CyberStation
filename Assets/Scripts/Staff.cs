using UnityEngine;

public abstract class Staff : Person
{
    public float hiringCost;
    public float salaryPerMinute;

    protected override void OnTick()
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
}