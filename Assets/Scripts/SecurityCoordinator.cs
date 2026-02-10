using System.Collections.Generic;
using UnityEngine;

public class SecurityCoordinator : MonoBehaviour
{
    public static SecurityCoordinator Instance;
    
    public List<Passenger> knownEvaders = new List<Passenger>();
    public Dictionary<Passenger, SecurityGuard> currentPursuits = new Dictionary<Passenger, SecurityGuard>();
    
    void Awake()
    {
        Instance = this;
    }
    
    public void ReportEvader(Passenger evader)
    {
        if (!knownEvaders.Contains(evader))
        {
            knownEvaders.Add(evader);
        }
    }

    public void RequestAssignment(SecurityGuard securityGuard)
    {
        Passenger assignedEvader = GetAvailableEvaderForGuard(securityGuard);
        if (assignedEvader != null)
        {
            securityGuard.AssignEvader(assignedEvader);
        }
    }
    
    public void ResolvePursuit(Passenger passenger)
    {
        if (knownEvaders.Contains(passenger)) 
        {
            knownEvaders.Remove(passenger);
        }
        
        if (currentPursuits.ContainsKey(passenger)) 
        {
            currentPursuits.Remove(passenger);
        }
    }
    
    private Passenger GetAvailableEvaderForGuard(SecurityGuard securityGuard)
    {
        Passenger bestTarget = null;
        float closestDist = float.MaxValue;
        
        foreach (var evader in knownEvaders)
        {
            if (currentPursuits.ContainsKey(evader)) continue;

            float dist = Vector3.Distance(securityGuard.transform.position, evader.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = evader;
            }
        }

        if (bestTarget != null)
        {
            currentPursuits.Add(bestTarget, securityGuard);
        }

        return bestTarget;
    }
}
