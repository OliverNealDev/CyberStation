using System.Collections.Generic;
using UnityEngine;

public class SecurityDrone : Staff
{
    public float scanRadius = 1.5f;
    public float scanCooldown = 1.5f;
    public float hoverHeight = 8f;
    public float moveSpeed = 8f; 
    public float turnSpeed = 8f;
    
    public GameObject scanBeam;
    public GameObject dematerializeBeam;
    public float scanBeamDuration = 0.5f;
    public float dematerializeBeamDuration = 0.6f;
    
    private Passenger currentTarget;
    private bool isPursuingEvader = false;
    private bool isCurrentlyScanning = false;
    
    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    protected override void Awake()
    {
        base.Awake();
        
        if (navAgent != null)
        {
            navAgent.enabled = false;
            Destroy(navAgent);
        }
        
        targetPosition = transform.position;

        if (scanBeam != null) scanBeam.SetActive(false);
        if (dematerializeBeam != null) dematerializeBeam.SetActive(false);
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
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
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
        
        Passenger p = currentTarget; 
        
        if (scanBeam != null) scanBeam.SetActive(true);
        yield return new WaitForSeconds(scanBeamDuration);
        if (scanBeam != null) scanBeam.SetActive(false);

        if (p != null)
        {
            p.hasBeenInspected = true;
            SecurityCoordinator.Instance.ResolveInspection(p); 
            
            if (!p.hasTicket && p.isTicketEvader)
            {
                if (dematerializeBeam != null) dematerializeBeam.SetActive(true);
                yield return new WaitForSeconds(dematerializeBeamDuration);
                if (dematerializeBeam != null) dematerializeBeam.SetActive(false);

                if (p != null) 
                {
                    PassengerManager.Instance.OnCaughtByDrone(p);
                    SecurityCoordinator.Instance.ResolvePursuit(p);
                }
            }
        }

        ClearCurrentTarget();
        isCurrentlyScanning = false;
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