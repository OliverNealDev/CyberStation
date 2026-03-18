using System.Collections.Generic;
using UnityEngine;

public class SecurityCoordinator : MonoBehaviour
{
    public static SecurityCoordinator Instance;
    
    public List<Passenger> knownEvaders = new List<Passenger>();
    public Dictionary<Passenger, SecurityDrone> currentPursuits = new Dictionary<Passenger, SecurityDrone>();
    public Dictionary<Passenger, SecurityDrone> currentInspections = new Dictionary<Passenger, SecurityDrone>();
    
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

    public void RequestAssignment(SecurityDrone securityDrone)
    {
        Passenger assignedEvader = GetAvailableEvaderForGuard(securityDrone);
        if (assignedEvader != null)
        {
            securityDrone.AssignEvader(assignedEvader);
            return;
        }

        Passenger assignedInspection = GetAvailableInspectionForGuard(securityDrone);
        if (assignedInspection != null)
        {
            securityDrone.AssignInspection(assignedInspection);
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
    
    private Passenger GetAvailableEvaderForGuard(SecurityDrone securityDrone)
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

            float dist = Vector3.Distance(securityDrone.transform.position, evader.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = evader;
            }
        }

        if (bestTarget != null)
        {
            currentPursuits.Add(bestTarget, securityDrone);
        }

        return bestTarget;
    }

    private Passenger GetAvailableInspectionForGuard(SecurityDrone securityDrone)
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

            float dist = Vector3.Distance(securityDrone.transform.position, p.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = p;
            }
        }

        if (bestTarget != null)
        {
            currentInspections.Add(bestTarget, securityDrone);
        }

        return bestTarget;
    }
}