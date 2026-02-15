using UnityEngine;
using UnityEngine.Serialization;

public class TicketMachineController : QueuableObject // Inherits from Parent
{
    public Person currentPerson;
    public enum MachineState { Idle, Processing }
    public MachineState state = MachineState.Idle;
    
    public override bool IsAvailable => state == MachineState.Idle;

    void Start()
    {
        TicketMachineManager.Instance.RegisterTicketMachine(this);
    }
    
    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            state = MachineState.Processing;
            currentPerson = person;
            Invoke(nameof(FinishProcessing), 3f);
        }
    }

    private void FinishProcessing()
    {
        if (currentPerson != null)
        {
            Passenger passenger = (Passenger)currentPerson;
        
            PassengerManager.Instance.ReceiveTicket(passenger);

            int ticketPrice = 0;

            if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
            {
                ticketPrice = passenger.assignedTrainService.trainData.costPerRide;
                
                if (WorldSpacePromptCoordinator.Instance != null)
                {
                    WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                        "+$" + ticketPrice, 
                        transform.position + Vector3.up * 7f,
                        Color.darkGreen);
                }
            }
            else
            {
                ticketPrice = 0;
            }

            currentPerson = null;
        }
        state = MachineState.Idle;
    }
}