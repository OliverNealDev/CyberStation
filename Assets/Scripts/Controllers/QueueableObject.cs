using System.Collections.Generic;
using UnityEngine;

public abstract class QueuableObject : MonoBehaviour
{
    public List<Passenger> PassengersOnWay = new List<Passenger>();

    public virtual float QueueSpacing => 1.5f; 
    public virtual float BaseStoppingDistance => 1.0f;
    
    public virtual void AssignPassenger(Passenger passenger)
    {
        if (!PassengersOnWay.Contains(passenger)) PassengersOnWay.Add(passenger);
    }

    public virtual void RemovePassenger(Passenger passenger)
    {
        if (PassengersOnWay.Contains(passenger)) PassengersOnWay.Remove(passenger);
    }

    public float GetStoppingDistanceFor(Passenger passenger)
    {
        int index = PassengersOnWay.IndexOf(passenger);
        if (index == -1) return -1f; // Error: Passenger not found in list

        return BaseStoppingDistance + (index * QueueSpacing);
    }
    
    public abstract void ProcessInteraction(Passenger passenger);
    public abstract bool IsAvailable { get; }
}