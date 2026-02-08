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
            PassengerManager.Instance.ReceiveTicket((Passenger)currentPerson);
            currentPerson = null;
        }
        state = MachineState.Idle;
    }
}