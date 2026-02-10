using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SecurityGuard : Staff
{
    private Passenger targetEvadingPassenger;
    private float defaultStoppingDistance;

    public float detectionRadius = 4f;
    
    public securitySubStates currentSubState = securitySubStates.Idle;
    public enum securitySubStates
    {
        Idle,
        ApproachingTarget,
        InteractingWithTarget,
        EscortingTarget
    }

    protected override void Awake() 
    {
        base.Awake();
        if(navAgent != null) defaultStoppingDistance = navAgent.stoppingDistance;
    }

    public override void PerformDuties()
    {
        switch (currentSubState)
        {
            case securitySubStates.Idle:
                ReportNearbyEvaders(detectionRadius);
                SecurityCoordinator.Instance.RequestAssignment(this);
                break;
            
            case securitySubStates.ApproachingTarget:
                if (targetEvadingPassenger == null)
                {
                    ReturnToIdle();
                    return;
                }
                
                navAgent.SetDestination(targetEvadingPassenger.transform.position);
                
                // Use a slightly larger catch radius than stopping distance to prevent jitters
                if (Vector3.Distance(transform.position, targetEvadingPassenger.transform.position) <= 2.0f)
                {
                    PassengerManager.Instance.OnCaughtBySecurity(targetEvadingPassenger);
                    SecurityCoordinator.Instance.ResolvePursuit(targetEvadingPassenger);
                        
                    currentSubState = securitySubStates.InteractingWithTarget;
                    Invoke("BeginEscort", 2f);
                }
                break;
            
            case securitySubStates.InteractingWithTarget:
                navAgent.ResetPath();
                break;

            case securitySubStates.EscortingTarget:
                if (targetEvadingPassenger != null)
                {
                    navAgent.SetDestination(targetEvadingPassenger.transform.position);
                    
                    if(targetEvadingPassenger == null) ReturnToIdle();
                }
                else
                {
                    ReturnToIdle();
                }
                break;
        }
    }
    
    void BeginEscort()
    {
        if (targetEvadingPassenger != null)
        {
            targetEvadingPassenger.isBeingEscorted = true;
            currentSubState = securitySubStates.EscortingTarget;
            
            navAgent.stoppingDistance = 2.5f; 
            
            navAgent.SetDestination(targetEvadingPassenger.transform.position);
        }
        else
        {
            ReturnToIdle();
        }
    }
    
    public void AssignEvader(Passenger evader)
    {
        targetEvadingPassenger = evader;
        currentSubState = securitySubStates.ApproachingTarget;
    }
    
    void ReturnToIdle()
    {
        navAgent.stoppingDistance = defaultStoppingDistance;
        
        targetEvadingPassenger = null;
        currentSubState = securitySubStates.Idle;
    }
    
    private void ReportNearbyEvaders(float detectionRadius)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider collider in nearbyColliders)
        {
            if (collider.CompareTag("Passenger"))
            {
                Passenger passenger = collider.GetComponent<Passenger>();
                if (passenger != null && passenger.hasBypassedBarrier && !passenger.isBeingEscorted)
                {
                    SecurityCoordinator.Instance.ReportEvader(passenger);
                }
            }
        }
    }
}