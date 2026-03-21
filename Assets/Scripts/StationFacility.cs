using UnityEngine;

public enum FacilityType
{
    TicketMachine,
    NutrientExtruder,
    CaloricInjectionPort,
    HydratingObelisk,
    CleansingShower,
    RestPad,
    MolecularScrubber
}

public abstract class StationFacility : QueuableObject 
{
    [Header("Facility Settings")]
    [Tooltip("Select what kind of machine this is from the dropdown!")]
    public FacilityType facilityType; 
    
    public Person currentPerson;
    
    public enum MachineState { Idle, Processing }
    public MachineState state = MachineState.Idle;
    
    public override bool IsAvailable => state == MachineState.Idle;

    protected virtual void Start()
    {
        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.RegisterFacility(this);
        }
    }

    protected virtual void OnDestroy()
    {
        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.DeregisterFacility(this);
        }
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
            
            DeliverService(passenger); 
            
            currentPerson = null;
        }
        
        state = MachineState.Idle;
    }

    protected abstract void DeliverService(Passenger passenger);
}