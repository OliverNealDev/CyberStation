using UnityEngine;

public abstract class Staff : Person
{
    protected override void OnTick()
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
}