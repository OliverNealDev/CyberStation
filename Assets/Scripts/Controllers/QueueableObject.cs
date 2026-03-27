using System.Collections.Generic;
using UnityEngine;

public enum QueueStyle
{
    FrontOnly,
    FourSided   
}

public abstract class QueuableObject : MonoBehaviour
{
    [Header("Queue Settings")]
    public QueueStyle queueStyle = QueueStyle.FrontOnly;
    
    [Header("Spacing")]
    [Tooltip("How far away the first person stands from the machine's center.")]
    public float baseDistance = 1.5f; 
    [Tooltip("How much space is between each person in the line.")]
    public float queueSpacing = 1.5f; 

    public List<Person> PeopleOnWay = new List<Person>();

    public virtual void AssignPerson(Person person)
    {
        if (!PeopleOnWay.Contains(person)) PeopleOnWay.Add(person);
    }

    public virtual void RemovePerson(Person person)
    {
        if (PeopleOnWay.Contains(person)) PeopleOnWay.Remove(person);
    }

    public virtual Vector3 GetQueuePositionFor(Person person)
    {
        int index = PeopleOnWay.IndexOf(person);
        if (index == -1) return transform.position; 

        if (queueStyle == QueueStyle.FrontOnly)
        {
            float distance = baseDistance + (index * queueSpacing);
            return transform.position + (transform.forward * distance);
        }
        else
        {
            int side = Mathf.Abs(person.GetInstanceID()) % 4;
            int depth = 0;
            
            for (int i = 0; i < index; i++)
            {
                if (Mathf.Abs(PeopleOnWay[i].GetInstanceID()) % 4 == side)
                {
                    depth++;
                }
            }

            Vector3 direction = transform.forward; 

            switch (side)
            {
                case 0: direction = transform.forward; break;  
                case 1: direction = transform.right; break;    
                case 2: direction = -transform.forward; break; 
                case 3: direction = -transform.right; break;   
            }

            float distance = baseDistance + (depth * queueSpacing);
            return transform.position + (direction * distance);
        }
    }
    
    public abstract void ProcessInteraction(Person person);
    public abstract bool IsAvailable { get; }
}
