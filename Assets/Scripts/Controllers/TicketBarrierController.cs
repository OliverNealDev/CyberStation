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
                if (passenger.hasTicket)
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
        PassengerManager.Instance.OnTicketBarrierDenial(passenger);
        if (passenger.isTicketEvader)
        {
            SecurityCoordinator.Instance.ReportEvader(passenger);
        }
    }
}
