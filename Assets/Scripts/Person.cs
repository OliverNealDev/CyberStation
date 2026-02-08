using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Person : MonoBehaviour
{
    public NavMeshAgent agent;
    
    // Needs
    public float comfort = 100f; // How comfortable the passenger feels, which can be affected by crowding, cleanliness, etc.
    public float satiation = 100f;
    public float hydration = 100f;
    public float hygiene = 100f;
    
    public List<NeedType> GetNeedsInPriorityOrder()
    {
        List<NeedType> needs = new List<NeedType>
        {
            NeedType.Comfort,
            NeedType.Satiation,
            NeedType.Hydration,
            NeedType.Hygiene
        };
        
        needs.Sort((a, b) =>
        {
            float valueA = GetNeedValue(a);
            float valueB = GetNeedValue(b);
            return valueA.CompareTo(valueB); // Sort in ascending order (lowest first)
        });
        
        return needs;
    }
    
    public float GetNeedValue(NeedType need)
    {
        return need switch
        {
            NeedType.Comfort => comfort,
            NeedType.Satiation => satiation,
            NeedType.Hydration => hydration,
            NeedType.Hygiene => hygiene,
            _ => 0f
        };
    }
    
    public enum NeedType
    {
        None,
        Comfort,
        Satiation,
        Hydration,
        Hygiene
    }
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Random.Range(3f, 4f);
    }
}
