public class TicketMachineController : QueuableObject // Inherits from Parent
{
    public Passenger currentPassenger;
    public enum MachineState { Idle, Processing }
    public MachineState state = MachineState.Idle;
    
    public override bool IsAvailable => state == MachineState.Idle;

    void Start()
    {
        TicketMachineManager.Instance.RegisterTicketMachine(this);
    }
    
    public override void ProcessInteraction(Passenger passenger)
    {
        if (state == MachineState.Idle)
        {
            state = MachineState.Processing;
            currentPassenger = passenger;
            Invoke(nameof(FinishProcessing), 3f);
        }
    }

    private void FinishProcessing()
    {
        if (currentPassenger != null)
        {
            PassengerManager.Instance.ReceiveTicket(currentPassenger);
            currentPassenger = null;
        }
        state = MachineState.Idle;
    }
}