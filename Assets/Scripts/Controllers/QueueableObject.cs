using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class QueuableObject : MonoBehaviour
{
    public List<Person> PeopleOnWay = new List<Person>();

    public virtual float QueueSpacing => 1.5f; 
    public virtual float BaseStoppingDistance => 1.0f;
    
    public virtual void AssignPerson(Person person)
    {
        if (!PeopleOnWay.Contains(person)) PeopleOnWay.Add(person);
    }

    public virtual void RemovePerson(Person person)
    {
        if (PeopleOnWay.Contains(person)) PeopleOnWay.Remove(person);
    }

    public float GetStoppingDistanceFor(Person person)
    {
        int index = PeopleOnWay.IndexOf(person);
        if (index == -1) return -1f; // Error: Passenger not found in list

        return BaseStoppingDistance + (index * QueueSpacing);
    }
    
    public abstract void ProcessInteraction(Person person);
    public abstract bool IsAvailable { get; }
}