using System.Collections.Generic;
using UnityEngine;

public class JanitorCoordinator : MonoBehaviour
{
    public static JanitorCoordinator Instance;

    [Header("Litter Icon")]
    [SerializeField] private Sprite litterIconSprite;
    
    public List<Litter> allLitter = new List<Litter>();
    public Dictionary<Litter, Janitor> assignedLitter = new Dictionary<Litter, Janitor>();

    public Sprite LitterIconSprite => litterIconSprite;
    
    void Awake()
    {
        Instance = this;
    }

    public void RequestAssignment(Janitor janitor)
    {
        Litter assignedLitter = GetAvailableLitterForJanitor(janitor);
        if (assignedLitter != null)
        {
            janitor.AssignLitter(assignedLitter);
        }
    }
    
    public void ResolveClean(Litter litter)
    {
        if (allLitter.Contains(litter)) 
        {
            allLitter.Remove(litter);
        }
        
        if (assignedLitter.ContainsKey(litter)) 
        {
            assignedLitter.Remove(litter);
        }
    }
    
    private Litter GetAvailableLitterForJanitor(Janitor janitor)
    {
        Litter bestTarget = null;
        float closestDist = float.MaxValue;
        
        foreach (var litter in allLitter)
        {
            if (litter == null)
            {
                continue;
            }
            if (assignedLitter.ContainsKey(litter)) continue;

            float dist = Vector3.Distance(janitor.transform.position, litter.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = litter;
            }
        }

        if (bestTarget != null)
        {
            assignedLitter.Add(bestTarget, janitor);
        }

        return bestTarget;
    }

    public void ReportLitter(Litter litter)
    {
        if (!allLitter.Contains(litter))
        {
            allLitter.Add(litter);
        }
    }
}
