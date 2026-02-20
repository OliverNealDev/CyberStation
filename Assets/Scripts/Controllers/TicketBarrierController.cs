using UnityEngine;

public class TicketBarrierController : MonoBehaviour
{
    private BoxCollider triggerBox;
    [SerializeField] private GameObject floorIndicator;
    private MeshRenderer floorIndicatorRenderer;

    public Material neutral;
    public Material valid;
    public Material invalid;
    
    void Awake()
    {
        triggerBox = GetComponent<BoxCollider>();
        if (triggerBox == null)
        {
            Debug.LogError("TicketBarrierController requires a BoxCollider component.");
        }
    }

    void Start()
    {
        floorIndicatorRenderer = floorIndicator.GetComponent<MeshRenderer>();
        floorIndicatorRenderer.material = neutral;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Passenger"))
        {
            if (other.GetComponent<Passenger>() != null)
            {
                Passenger passenger = other.GetComponent<Passenger>();
                
                if (passenger.hasTicket || passenger.assignedTrainService == null || passenger.isBeingEscorted) // Allow access if they have a ticket or are not assigned to any train service (service likely manually ended)
                {
                    OpenBarrier();
                }
                else
                {
                    DenyAccess(passenger);
                    
                }
            }
        }
        else // Staff and other entities can pass freely
        {
            floorIndicatorRenderer.material = valid;
            CancelInvoke("ResetIndicator");
            Invoke("ResetIndicator", 1f);
        }
    }
    
    private void OpenBarrier()
    {
        floorIndicatorRenderer.material = valid;
        CancelInvoke("ResetIndicator");
        Invoke("ResetIndicator", 1f);
    }
    
    private void DenyAccess(Passenger passenger)
    {
        floorIndicatorRenderer.material = invalid;
        CancelInvoke("ResetIndicator");
        Invoke("ResetIndicator", 1f);
        
        if (passenger.isTicketEvader && !passenger.hasBypassedBarrier)
        {
            SecurityCoordinator.Instance.ReportEvader(passenger);
            //WorldSpacePromptCoordinator.Instance.CreateWorldPrompt("[Unauthorised Access Detected]", transform.position + Vector3.up * 7f, Color.softRed);
        }
        PassengerManager.Instance.OnTicketBarrierDenial(passenger);
    }
    
    private void ResetIndicator()
    {
        floorIndicatorRenderer.material = neutral;
    }
}
