using System.Collections.Generic;
using UnityEngine;

public class SecurityCoordinator : MonoBehaviour
{
    public static SecurityCoordinator Instance;
    
    public List<Passenger> knownEvaders = new List<Passenger>();
    public Dictionary<Passenger, SecurityGuard> currentPursuits = new Dictionary<Passenger, SecurityGuard>();
    public Dictionary<Passenger, SecurityGuard> currentInspections = new Dictionary<Passenger, SecurityGuard>();
    
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
            return;
        }

        Passenger assignedInspection = GetAvailableInspectionForGuard(securityGuard);
        if (assignedInspection != null)
        {
            securityGuard.AssignInspection(assignedInspection);
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

    public void ResolveInspection(Passenger passenger)
    {
        if (currentInspections.ContainsKey(passenger))
        {
            currentInspections.Remove(passenger);
        }
    }
    
    private Passenger GetAvailableEvaderForGuard(SecurityGuard securityGuard)
    {
        Passenger bestTarget = null;
        float closestDist = float.MaxValue;
        
        for (int i = knownEvaders.Count - 1; i >= 0; i--)
        {
            Passenger evader = knownEvaders[i];
            
            if (evader == null)
            {
                knownEvaders.Remove(evader);
                continue;
            }
            if (currentPursuits.ContainsKey(evader)) continue;
            if (evader.currentMasterState == Passenger.passengerMasterStates.OnTrain) continue;

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

    private Passenger GetAvailableInspectionForGuard(SecurityGuard securityGuard)
    {
        Passenger bestTarget = null;
        float closestDist = float.MaxValue;

        List<Passenger> uncheckedPassengers = PassengerManager.Instance.GetUncheckedPlatformPassengers();

        foreach (Passenger p in uncheckedPassengers)
        {
            if (p == null) continue;
            if (currentInspections.ContainsKey(p) || currentPursuits.ContainsKey(p)) continue;
            if (p.currentMasterState != Passenger.passengerMasterStates.OnPlatform) continue;
            if (p.hasBeenInspected) continue;

            float dist = Vector3.Distance(securityGuard.transform.position, p.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = p;
            }
        }

        if (bestTarget != null)
        {
            currentInspections.Add(bestTarget, securityGuard);
        }

        return bestTarget;
    }
}