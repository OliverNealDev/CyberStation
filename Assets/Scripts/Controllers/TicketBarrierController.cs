using UnityEngine;

public class TicketBarrierController : MonoBehaviour
{
    private BoxCollider triggerBox;
    
    void Awake()
    {
        triggerBox = GetComponent<BoxCollider>();
        if (triggerBox == null)
        {
            Debug.LogError("TicketBarrierController requires a BoxCollider component.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Passenger"))
        {
            if (other.GetComponent<Passenger>() != null)
            {
                Passenger passenger = other.GetComponent<Passenger>();
                
                if (passenger.hasTicket || passenger.assignedTrainService == null) // Allow access if they have a ticket or are not assigned to any train service (service likely manually ended)
                {
                    OpenBarrier();
                }
                else
                {
                    DenyAccess(passenger);
                }
            }
        }
        else
        {
            Debug.LogWarning("Non-passenger object entered the ticket barrier trigger.");
        }
    }
    
    private void OpenBarrier()
    {
        // Logic to open the barrier
    }
    
    private void DenyAccess(Passenger passenger)
    {
        if (passenger.isTicketEvader && !passenger.hasBypassedBarrier)
        {
            SecurityCoordinator.Instance.ReportEvader(passenger);
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt("[Unauthorised Access Detected]", transform.position + Vector3.up * 7f, Color.softRed);
        }
        PassengerManager.Instance.OnTicketBarrierDenial(passenger);
    }
}
