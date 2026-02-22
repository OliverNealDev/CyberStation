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
                
                Vector3 flatJanitor = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatLitter = new Vector3(targetLitter.transform.position.x, 0, targetLitter.transform.position.z);
                
                if (Vector3.Distance(flatJanitor, flatLitter) <= defaultStoppingDistance + 0.2f)
                {
                    JanitorCoordinator.Instance.ResolveClean(targetLitter);
                    currentSubState = janitorSubStates.InteractingWithLitter;
                    
                    Invoke("ReturnToIdle", targetLitter.timeToClean);
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
        
        targetLitter = null; 
        
        currentSubState = janitorSubStates.Idle;
    }
}
