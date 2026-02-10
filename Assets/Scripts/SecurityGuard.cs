using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SecurityGuard : Staff
{
    private Passenger targetEvadingPassenger;

    public float detectionRadius = 4f;
    
    public securitySubStates currentSubState = securitySubStates.Idle;
    public enum securitySubStates
    {
        Idle,
        MovingToTarget,
        InteractingWithTarget
    }

    public override void PerformDuties()
    {
        switch (currentSubState)
        {
            case securitySubStates.Idle:
                ReportNearbyEvaders(detectionRadius);
                SecurityCoordinator.Instance.RequestAssignment(this);
                break;
            
            case securitySubStates.MovingToTarget:
                if (targetEvadingPassenger != null)
                {
                    navAgent.SetDestination(targetEvadingPassenger.transform.position);
                    if (Vector3.Distance(transform.position, targetEvadingPassenger.transform.position) < 1.5f)
                    {
                        PassengerManager.Instance.OnCaughtBySecurity(targetEvadingPassenger);
                        
                        SecurityCoordinator.Instance.ResolvePursuit(targetEvadingPassenger);
                        
                        currentSubState = securitySubStates.InteractingWithTarget;
                        Invoke("ReturnToIdle", 2f);
                    }
                }
                else
                {
                    currentSubState = securitySubStates.Idle;
                }
                break;
            
            case securitySubStates.InteractingWithTarget:
                // Handle interaction with the evading passenger (e.g., confront them, call for backup, etc.)
                break;
        }
    }
    
    public void AssignEvader(Passenger evader)
    {
        targetEvadingPassenger = evader;
        currentSubState = securitySubStates.MovingToTarget;
    }
    
    void ReturnToIdle()
    {
        targetEvadingPassenger = null;
        currentSubState = securitySubStates.Idle;
    }
    
    private void ReportNearbyEvaders(float detectionRadius)
    {
        Debug.Log("Security guard is scanning for evaders...");
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        Debug.Log($"Found {nearbyColliders.Length} colliders within detection radius.");
        foreach (Collider collider in nearbyColliders)
        {
            if (collider.CompareTag("Passenger"))
            {
                Passenger passenger = collider.GetComponent<Passenger>();
                if (passenger != null && passenger.hasBypassedBarrier)
                {
                    SecurityCoordinator.Instance.ReportEvader(passenger);
                }
            }
        }
    }
}
