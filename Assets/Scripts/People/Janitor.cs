using UnityEngine;

public class Janitor : Staff
{
    private Litter targetLitter;
    private float defaultStoppingDistance;
    private Vector3 startPosition;

    public MeshRenderer mainLightRenderer;
    public Material defaultMainMat;
    public Material movingMainMat;
    public Material cleaningMainMat;
    
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

        if (mainLightRenderer != null && defaultMainMat != null)
        {
            mainLightRenderer.material = defaultMainMat;
        }
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
                    if (mainLightRenderer != null && movingMainMat != null)
                    {
                        mainLightRenderer.material = movingMainMat;
                    }
                }
                break;
            
            case janitorSubStates.ApproachingLitter:
                if (targetLitter == null)
                {
                    ReturnToIdle();
                    return;
                }
                
                navAgent.stoppingDistance = 0.01f;
                navAgent.SetDestination(targetLitter.transform.position);
                
                Vector3 flatJanitor = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatLitter = new Vector3(targetLitter.transform.position.x, 0, targetLitter.transform.position.z);
                
                if (Vector3.Distance(flatJanitor, flatLitter) <= 0.2f)
                {
                    JanitorCoordinator.Instance.ResolveClean(targetLitter);
                    currentSubState = janitorSubStates.InteractingWithLitter;

                    if (mainLightRenderer != null && cleaningMainMat != null)
                    {
                        mainLightRenderer.material = cleaningMainMat;
                    }
                    
                    StartCoroutine(ShrinkAndCleanPuddle(targetLitter));
                }
                break;
            
            case janitorSubStates.InteractingWithLitter:
                if (navAgent.hasPath) navAgent.ResetPath(); 
                break;
        }
    }
    
    private System.Collections.IEnumerator ShrinkAndCleanPuddle(Litter litterObj)
    {
        float duration = litterObj.timeToClean;
        float elapsed = 0f;
        Vector3 initialScale = litterObj.transform.localScale;

        while (elapsed < duration)
        {
            if (litterObj == null) break;
            
            float t = elapsed / duration;
            litterObj.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnToIdle();
    }
    
    public void AssignLitter(Litter litter)
    {
        targetLitter = litter;
        currentSubState = janitorSubStates.ApproachingLitter;

        if (mainLightRenderer != null && movingMainMat != null)
        {
            mainLightRenderer.material = movingMainMat;
        }
    }
    
    void ReturnToIdle()
    {
        if(navAgent != null) navAgent.stoppingDistance = defaultStoppingDistance;
        if(navAgent != null && navAgent.isOnNavMesh) navAgent.ResetPath();
        
        if (targetLitter != null) Destroy(targetLitter.gameObject);
        
        targetLitter = null; 
        
        currentSubState = janitorSubStates.Idle;

        if (mainLightRenderer != null && defaultMainMat != null)
        {
            mainLightRenderer.material = defaultMainMat;
        }
    }
}