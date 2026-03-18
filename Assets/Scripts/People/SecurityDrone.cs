using System.Collections.Generic;
using UnityEngine;

public class SecurityDrone : Staff
{
    public float scanRadius = 1.5f;
    public float scanCooldown = 1.5f;
    public float hoverHeight = 8f;
    public float moveSpeed = 8f; 
    public float turnSpeed = 8f;
    public float maxTiltAngle = 20f;
    public float tiltSpeed = 5f;
    
    public GameObject scanBeam;
    public GameObject dematerializeBeam;
    public float scanBeamDuration = 0.5f;
    public float dematerializeBeamDuration = 0.6f;

    public MeshRenderer mainLightRenderer;
    public Material defaultMainMat;
    public Material scanMainMat;
    public Material approveMainMat;
    public Material dematMainMat;

    public MeshRenderer[] outerLightRenderers;
    public Material outerGreenMat;
    public Material outerYellowMat;
    
    private Passenger currentTarget;
    private bool isPursuingEvader = false;
    private bool isCurrentlyScanning = false;
    
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Quaternion baseRotation;

    protected override void Awake()
    {
        base.Awake();
        
        if (navAgent != null)
        {
            navAgent.enabled = false;
            Destroy(navAgent);
        }
        
        targetPosition = transform.position;
        baseRotation = transform.rotation;

        if (scanBeam != null) scanBeam.SetActive(false);
        if (dematerializeBeam != null) dematerializeBeam.SetActive(false);

        if (mainLightRenderer != null && defaultMainMat != null)
        {
            mainLightRenderer.material = defaultMainMat;
        }

        if (outerLightRenderers != null)
        {
            foreach (MeshRenderer r in outerLightRenderers)
            {
                StartCoroutine(BlinkOuterLight(r));
            }
        }
    }

    private void LateUpdate()
    {
        if (!isCurrentlyScanning)
        {
            MoveDrone();
        }
    }

    public override void PerformDuties()
    {
        if (isCurrentlyScanning) return; 

        UpdateTargetPosition();
        CheckDistanceToTarget();
    }

    private void UpdateTargetPosition()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            ClearCurrentTarget();
            SecurityCoordinator.Instance.RequestAssignment(this);

            if (currentTarget == null)
            {
                Vector2 flatPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 flatTarget = new Vector2(targetPosition.x, targetPosition.z);
                
                if (Vector2.Distance(flatPos, flatTarget) < 1f)
                {
                    targetPosition = transform.position + new Vector3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f));
                }
            }
        }
        else
        {
            targetPosition = currentTarget.transform.position;
        }
    }

    private void MoveDrone()
    {
        Vector3 desiredPosition = new Vector3(targetPosition.x, hoverHeight + (Mathf.Sin(Time.time * 2f) * 0.5f), targetPosition.z);
        
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 0.3f, moveSpeed);

        Vector3 direction = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        
        if (direction.sqrMagnitude > 0.01f)
        {
            baseRotation = Quaternion.Slerp(baseRotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }

        float speedMagnitude = new Vector2(currentVelocity.x, currentVelocity.z).magnitude;
        float tiltAmount = Mathf.Clamp(speedMagnitude / moveSpeed, 0f, 1f) * maxTiltAngle;
        
        Quaternion tiltRotation = Quaternion.Euler(tiltAmount, 0f, 0f);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation * tiltRotation, Time.deltaTime * tiltSpeed);
    }

    private void CheckDistanceToTarget()
    {
        if (currentTarget == null) return;

        Vector2 flatPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 flatTarget = new Vector2(currentTarget.transform.position.x, currentTarget.transform.position.z);

        if (Vector2.Distance(flatPos, flatTarget) <= scanRadius)
        {
            StartCoroutine(ScanAndProcessTarget());
        }
    }

    private System.Collections.IEnumerator ScanAndProcessTarget()
    {
        isCurrentlyScanning = true;
        currentVelocity = Vector3.zero; 
        
        float straightenDuration = 0.5f;
        float straightenElapsed = 0f;
        Quaternion startRot = transform.rotation;
        
        while (straightenElapsed < straightenDuration)
        {
            float t = straightenElapsed / straightenDuration;
            transform.rotation = Quaternion.Slerp(startRot, baseRotation, t);
            straightenElapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = baseRotation;
        
        Passenger p = currentTarget; 
        
        if (mainLightRenderer != null && scanMainMat != null)
        {
            mainLightRenderer.material = scanMainMat;
        }

        if (scanBeam != null) scanBeam.SetActive(true);
        yield return new WaitForSeconds(scanBeamDuration);
        if (scanBeam != null) scanBeam.SetActive(false);

        if (p != null)
        {
            p.hasBeenInspected = true;
            SecurityCoordinator.Instance.ResolveInspection(p); 
            
            if (!p.hasTicket && p.isTicketEvader)
            {
                if (mainLightRenderer != null && dematMainMat != null)
                {
                    mainLightRenderer.material = dematMainMat;
                }

                if (dematerializeBeam != null) dematerializeBeam.SetActive(true);
                yield return new WaitForSeconds(dematerializeBeamDuration);
                if (dematerializeBeam != null) dematerializeBeam.SetActive(false);

                if (p != null) 
                {
                    PassengerManager.Instance.OnCaughtByDrone(p);
                    SecurityCoordinator.Instance.ResolvePursuit(p);
                }
            }
            else
            {
                if (mainLightRenderer != null && approveMainMat != null)
                {
                    mainLightRenderer.material = approveMainMat;
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (mainLightRenderer != null && defaultMainMat != null)
        {
            mainLightRenderer.material = defaultMainMat;
        }

        ClearCurrentTarget();
        isCurrentlyScanning = false;
    }

    private System.Collections.IEnumerator BlinkOuterLight(MeshRenderer renderer)
    {
        if (renderer == null) yield break;

        while (true)
        {
            renderer.material = Random.value > 0.5f ? outerGreenMat : outerYellowMat;
            yield return new WaitForSeconds(Random.Range(0.1f, 0.6f));
        }
    }

    public void AssignEvader(Passenger evader)
    {
        currentTarget = evader;
        isPursuingEvader = true;
    }

    public void AssignInspection(Passenger passenger)
    {
        currentTarget = passenger;
        isPursuingEvader = false;
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            if (isPursuingEvader)
            {
                SecurityCoordinator.Instance.ResolvePursuit(currentTarget);
            }
            else
            {
                SecurityCoordinator.Instance.ResolveInspection(currentTarget);
            }
        }
        currentTarget = null;
        isPursuingEvader = false;
    }
}