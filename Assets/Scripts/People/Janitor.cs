using UnityEngine;

public class Janitor : Staff
{
    private Litter targetLitter;
    private float defaultStoppingDistance;
    private Vector3 startPosition;

    public MeshRenderer indicatorRenderer;
    public Material idleLightMat;
    public Material actionLightMat;
    public Material successLightMat;
    
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

        if (indicatorRenderer != null && idleLightMat != null)
        {
            indicatorRenderer.material = idleLightMat;
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
                    if (indicatorRenderer != null && idleLightMat != null)
                    {
                        indicatorRenderer.material = idleLightMat;
                    }
                }
                break;
            
            case janitorSubStates.ApproachingLitter:
                if (targetLitter == null)
                {
                    ReturnToIdle();
                    return;
                }
                
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.stoppingDistance = 0.01f;
                    navAgent.SetDestination(targetLitter.transform.position);
                }
                
                Vector3 flatJanitor = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatLitter = new Vector3(targetLitter.transform.position.x, 0, targetLitter.transform.position.z);
                
                if (Vector3.Distance(flatJanitor, flatLitter) <= 0.2f)
                {
                    JanitorCoordinator.Instance.ResolveClean(targetLitter);
                    currentSubState = janitorSubStates.InteractingWithLitter;

                    if (indicatorRenderer != null && actionLightMat != null)
                    {
                        indicatorRenderer.material = actionLightMat;
                    }
                    
                    StartCoroutine(ShrinkAndCleanPuddle(targetLitter));
                }
                break;
            
            case janitorSubStates.InteractingWithLitter:
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh && navAgent.hasPath) 
                {
                    navAgent.ResetPath(); 
                }
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

        if (indicatorRenderer != null && successLightMat != null)
        {
            indicatorRenderer.material = successLightMat;
        }

        yield return new WaitForSeconds(0.5f);

        ReturnToIdle();
    }
    
    public void AssignLitter(Litter litter)
    {
        targetLitter = litter;
        currentSubState = janitorSubStates.ApproachingLitter;

        if (indicatorRenderer != null && idleLightMat != null)
        {
            indicatorRenderer.material = idleLightMat;
        }
    }
    
    void ReturnToIdle()
    {
        if(navAgent != null) navAgent.stoppingDistance = defaultStoppingDistance;
        if(navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh) navAgent.ResetPath();
        
        if (targetLitter != null) Destroy(targetLitter.gameObject);
        
        targetLitter = null; 
        
        currentSubState = janitorSubStates.Idle;

        if (indicatorRenderer != null && idleLightMat != null)
        {
            indicatorRenderer.material = idleLightMat;
        }
    }
}