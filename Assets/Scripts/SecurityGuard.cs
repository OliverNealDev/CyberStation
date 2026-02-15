using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SecurityGuard : Staff
{
    private Passenger targetEvadingPassenger;
    private float defaultStoppingDistance;
    private Vector3 startPosition;

    public float detectionRadius = 4f;
    public float patrolRadius = 15f;
    
    public securitySubStates currentSubState = securitySubStates.Idle;
    public enum securitySubStates
    {
        Idle,
        Patrolling,
        ApproachingTarget,
        InteractingWithTarget,
        EscortingTarget
    }

    protected override void Awake() 
    {
        base.Awake();
        if(navAgent != null) defaultStoppingDistance = navAgent.stoppingDistance;
        startPosition = transform.position;
    }

    public override void PerformDuties()
    {
        switch (currentSubState)
        {
            case securitySubStates.Idle:
                //ReportNearbyEvaders(detectionRadius);
                SecurityCoordinator.Instance.RequestAssignment(this);
                
                if (targetEvadingPassenger != null)
                {
                    currentSubState = securitySubStates.ApproachingTarget;
                }
                else
                {
                    currentSubState = securitySubStates.Patrolling;
                }
                break;

            case securitySubStates.Patrolling:
                //ReportNearbyEvaders(detectionRadius);
                SecurityCoordinator.Instance.RequestAssignment(this);

                if (targetEvadingPassenger != null)
                {
                    currentSubState = securitySubStates.ApproachingTarget;
                    return;
                }

                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    SetRandomPatrolDestination();
                }
                break;
            
            case securitySubStates.ApproachingTarget:
                if (targetEvadingPassenger == null)
                {
                    ReturnToIdle();
                    return;
                }
                
                navAgent.SetDestination(targetEvadingPassenger.transform.position);
                
                if (Vector3.Distance(transform.position, targetEvadingPassenger.transform.position) <= 2.0f)
                {
                    PassengerManager.Instance.OnCaughtBySecurity(targetEvadingPassenger, this);
                    SecurityCoordinator.Instance.ResolvePursuit(targetEvadingPassenger);
                    Dialogue(this, dialogueData.GetRandomLine(DialogueType.CaughtEvader), Color.cornflowerBlue, 2);
                        
                    currentSubState = securitySubStates.InteractingWithTarget;
                    Invoke("BeginEscort", 4f);
                }
                break;
            
            case securitySubStates.InteractingWithTarget:
                navAgent.ResetPath();
                break;

            case securitySubStates.EscortingTarget:
                if (targetEvadingPassenger == null)
                {
                    ReturnToIdle();
                    return;
                }

                navAgent.SetDestination(targetEvadingPassenger.transform.position);
                break;
        }
    }

    void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            navAgent.SetDestination(hit.position);
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
        navAgent.ResetPath();
        
        targetEvadingPassenger = null;
        currentSubState = securitySubStates.Idle;
    }
    
    /*private void ReportNearbyEvaders(float detectionRadius)
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
    }*/
}