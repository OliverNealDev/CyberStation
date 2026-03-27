using System.Collections.Generic;
using UnityEngine;

public enum QueueStyle
{
    FrontOnly,
    FourSided   
}

public enum QueueLineMode
{
    OrderlyLines,
    StoppingDistance
}

public abstract class QueuableObject : MonoBehaviour
{
    [Header("Queue Settings")]
    public QueueLineMode queueLineMode = QueueLineMode.OrderlyLines;
    public QueueStyle queueStyle = QueueStyle.FrontOnly;
    
    [Header("Orderly Lines")]
    [Tooltip("How far away the first person stands from the machine's center when using orderly lines.")]
    [Min(0.1f)]
    public float baseDistance = 1.5f; 
    [Tooltip("How much space is between each person in the line when using orderly lines.")]
    [Min(0.1f)]
    public float queueSpacing = 1.5f; 

    [Header("Stopping Distance Lines")]
    [Tooltip("Shared point passengers move toward when using stopping distance lines.")]
    [Min(0.1f)]
    public float stoppingDistanceTargetDistance = 1.1f;
    [Tooltip("How close the first passenger gets to the shared target.")]
    [Min(0f)]
    public float stoppingDistanceBase = 0.35f;
    [Tooltip("Extra stopping distance added for each passenger further back in the queue.")]
    [Min(0f)]
    public float stoppingDistanceStep = 1f;

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
        
        if (queueLineMode == QueueLineMode.StoppingDistance)
        {
            return transform.position + (transform.forward * stoppingDistanceTargetDistance);
        }

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

    public virtual float GetStoppingDistanceFor(Person person)
    {
        if (queueLineMode != QueueLineMode.StoppingDistance)
        {
            return 0.1f;
        }

        int index = PeopleOnWay.IndexOf(person);
        return stoppingDistanceBase + (Mathf.Max(0, index) * stoppingDistanceStep);
    }
    
    public abstract void ProcessInteraction(Person person);
    public abstract bool IsAvailable { get; }
}
