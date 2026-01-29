using UnityEngine;

public class TicketMachineController : MonoBehaviour
{
    public TicketMachineStates currentTicketMachineState;
    public enum TicketMachineStates
    {
        Idle,
        ProcessingTicket,
    }

    void Start()
    {
        currentTicketMachineState = TicketMachineStates.Idle;
        TicketMachineManager.Instance.RegisterTicketMachine(this);
    }
    
    public void ProcessTicketRequest(Passenger passenger)
    {
        if (currentTicketMachineState == TicketMachineStates.Idle)
        {
            currentTicketMachineState = TicketMachineStates.ProcessingTicket;
            Invoke(nameof(FinishProcessingTicket), 3f); // Simulate ticket processing time
        }
    }
    
    private void FinishProcessingTicket()
    {
        currentTicketMachineState = TicketMachineStates.Idle;
    }
    
    // passenger pings ticketmachine for ticket - changes state to waiting for ticket
    // ticketmachine assigns the waiting passenger, starts the processing timer and changes state
    // when ticket is processed, ping passenger to say ticket is processed and change state back to idle
}
