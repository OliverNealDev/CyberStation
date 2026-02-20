using UnityEngine;

public class Janitor : Staff
{
    private Litter targetLitter;
    private float defaultStoppingDistance;
    private Vector3 startPosition;
    
    public janitorSubStates currentSubState = janitorSubStates.Idle;
    public enum janitorSubStates
    {
        Idle,
        ApproachingLitter,
        InteractingWithLitter
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
            case janitorSubStates.Idle:
                JanitorCoordinator.Instance.RequestAssignment(this);
                
                if (targetLitter != null)
                {
                    currentSubState = janitorSubStates.ApproachingLitter;
                }
                break;
            
            case janitorSubStates.ApproachingLitter:
                if (targetLitter == null)
                {
                    ReturnToIdle();
                    return;
                }
                
                navAgent.SetDestination(targetLitter.transform.position);
                
                if (Vector3.Distance(transform.position, targetLitter.transform.position) <= defaultStoppingDistance)
                {
                    
                    JanitorCoordinator.Instance.ResolveClean(targetLitter);
                    currentSubState = janitorSubStates.InteractingWithLitter;
                    Invoke("ReturnToIdle", targetLitter.timeToClean);
                    //Dialogue(this, dialogueData.GetRandomLine(DialogueType.StartingClean), Color.yellow, 2);
                }
                break;
            
            case janitorSubStates.InteractingWithLitter:
                if (navAgent.hasPath) navAgent.ResetPath(); 
                break;
        }
    }
    
    public void AssignLitter(Litter litter)
    {
        targetLitter = litter;
        currentSubState = janitorSubStates.ApproachingLitter;
    }
    
    void ReturnToIdle()
    {
        navAgent.stoppingDistance = defaultStoppingDistance;
        navAgent.ResetPath();
        
        if (targetLitter != null) Destroy(targetLitter.gameObject);
        currentSubState = janitorSubStates.Idle;
    }
}
