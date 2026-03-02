using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SecurityGuard : Staff
{
    private Passenger targetEvadingPassenger;
    private Passenger targetForInspection;
    
    public float defaultStoppingDistance = 2;
    private Vector3 startPosition;
    public float patrolRadius = 30f;
    
    public securitySubStates currentSubState = securitySubStates.Idle;
    
    public enum securitySubStates
    {
        Idle,
        Patrolling,
        ApproachingTarget,
        ApproachingForInspection,
        InspectingTicket,
        InteractingWithTarget,
        EscortingTarget
    }

    protected override void Awake() 
    {
        base.Awake();
        if(navAgent != null) navAgent.stoppingDistance = defaultStoppingDistance;
        startPosition = transform.position;
    }

    public override void PerformDuties()
    {
        switch (currentSubState)
        {
            case securitySubStates.Idle:
                SecurityCoordinator.Instance.RequestAssignment(this);
                
                if (targetEvadingPassenger != null)
                {
                    currentSubState = securitySubStates.ApproachingTarget;
                }
                else if (targetForInspection != null)
                {
                    currentSubState = securitySubStates.ApproachingForInspection;
                }
                else
                {
                    currentSubState = securitySubStates.Patrolling;
                }
                break;

            case securitySubStates.Patrolling:
                SecurityCoordinator.Instance.RequestAssignment(this);

                if (targetEvadingPassenger != null)
                {
                    if (targetForInspection != null)
                    {
                        SecurityCoordinator.Instance.ResolveInspection(targetForInspection);
                        targetForInspection = null;
                    }
                    currentSubState = securitySubStates.ApproachingTarget;
                    return;
                }

                if (targetForInspection != null)
                {
                    currentSubState = securitySubStates.ApproachingForInspection;
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
                
                if (Vector3.Distance(transform.position, targetEvadingPassenger.transform.position) <= defaultStoppingDistance)
                {
                    PassengerManager.Instance.OnCaughtBySecurity(targetEvadingPassenger, this);
                    SecurityCoordinator.Instance.ResolvePursuit(targetEvadingPassenger);
                    Dialogue(this, dialogueData.GetRandomLine(DialogueType.CaughtEvader), Color.cornflowerBlue, 2);
                        
                    currentSubState = securitySubStates.InteractingWithTarget;
                    Invoke("BeginEscort", 4f);
                }
                break;

            case securitySubStates.ApproachingForInspection:
                if (targetForInspection == null || targetForInspection.currentMasterState != Passenger.passengerMasterStates.OnPlatform)
                {
                    if (targetForInspection != null) SecurityCoordinator.Instance.ResolveInspection(targetForInspection);
                    ReturnToIdle();
                    return;
                }

                navAgent.SetDestination(targetForInspection.transform.position);

                if (Vector3.Distance(transform.position, targetForInspection.transform.position) <= defaultStoppingDistance)
                {
                    currentSubState = securitySubStates.InspectingTicket;
                    StartCoroutine(InspectionRoutine());
                }
                break;

            case securitySubStates.InspectingTicket:
                break;
            
            case securitySubStates.InteractingWithTarget:
                if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
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

    private IEnumerator InspectionRoutine()
    {
        if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }

        if (targetForInspection != null)
        {
            targetForInspection.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;
            
            if (targetForInspection.navAgent != null && targetForInspection.navAgent.isActiveAndEnabled && targetForInspection.navAgent.isOnNavMesh)
            {
                targetForInspection.navAgent.ResetPath();
                targetForInspection.navAgent.velocity = Vector3.zero;
            }
        }

        Dialogue(this, "Ticket inspection, please.", Color.cornflowerBlue, 2f);
        yield return new WaitForSeconds(1.5f);

        if (targetForInspection == null)
        {
            ReturnToIdle();
            yield break;
        }

        if (targetForInspection.hasTicket)
        {
            Dialogue(targetForInspection, "Here is my ticket.", Color.white, 2f);
            yield return new WaitForSeconds(1.5f);
            
            Dialogue(this, "Thank you. Have a good journey.", Color.cornflowerBlue, 2f);
            yield return new WaitForSeconds(1f);

            targetForInspection.hasBeenInspected = true;
            SecurityCoordinator.Instance.ResolveInspection(targetForInspection);
            
            targetForInspection.currentSubState = Passenger.passengerSubStates.Idle;
            targetForInspection = null;
            ReturnToIdle();
        }
        else
        {
            Dialogue(targetForInspection, "I don't have a ticket...", new Color(1f, 0.4f, 0.4f), 2f);
            yield return new WaitForSeconds(1.5f);

            targetForInspection.hasBeenInspected = true;
            SecurityCoordinator.Instance.ResolveInspection(targetForInspection);

            Passenger caughtEvader = targetForInspection;
            targetForInspection = null;
            
            AssignEvader(caughtEvader);
            
            PassengerManager.Instance.OnCaughtBySecurity(targetEvadingPassenger, this);
            SecurityCoordinator.Instance.ResolvePursuit(targetEvadingPassenger);
            Dialogue(this, dialogueData.GetRandomLine(DialogueType.CaughtEvader), Color.cornflowerBlue, 2);
            
            currentSubState = securitySubStates.InteractingWithTarget;
            Invoke("BeginEscort", 4f);
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
        navAgent.stoppingDistance = defaultStoppingDistance;
    }

    public void AssignInspection(Passenger passenger)
    {
        targetForInspection = passenger;
        currentSubState = securitySubStates.ApproachingForInspection;
        navAgent.stoppingDistance = defaultStoppingDistance;
    }
    
    void ReturnToIdle()
    {
        navAgent.stoppingDistance = defaultStoppingDistance;
        if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
        }
        
        targetEvadingPassenger = null;
        targetForInspection = null;
        currentSubState = securitySubStates.Idle;
    }
}