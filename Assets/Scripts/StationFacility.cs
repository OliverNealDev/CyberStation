using System.Collections.Generic;
using UnityEngine;

public enum FacilityType
{
    TicketMachine,
    NutrientExtruder,
    SnackPrinter,
    HydratingObelisk,
    CleansingShower,
    PrivateLavatory,
    RestPad,
    MolecularScrubber
}

public abstract class StationFacility : QueuableObject 
{
    protected static readonly Color IdleNeedIconColor = Color.white;
    protected static readonly Color HungerNeedIconColor = new Color(1f, 0.64f, 0f);
    protected static readonly Color ThirstNeedIconColor = Color.cyan;
    protected static readonly Color HygieneNeedIconColor = new Color(1f, 0.41f, 0.71f);

    [Header("Facility Settings")]
    [Tooltip("Select what kind of machine this is from the dropdown!")]
    public FacilityType facilityType; 

    [Header("Litter")]
    public List<GameObject> potentialLitterPrefabs = new List<GameObject>();

    [Header("Queue Capacity")]
    [SerializeField] private int passengerQueueCapacity = 6;

    public Person currentPerson;
    
    public enum MachineState { Idle, Processing }
    public MachineState state = MachineState.Idle;
    
    public override bool IsAvailable => state == MachineState.Idle;
    public virtual float EstimatedServiceDuration => 3f;
    public int PassengerQueueCapacity => Mathf.Max(1, passengerQueueCapacity);

    protected virtual void Start()
    {
        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.RegisterFacility(this);
        }
    }

    protected virtual void OnDestroy()
    {
        ReleaseAssignedPassengers();

        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.DeregisterFacility(this);
        }
    }

    public override bool CanAcceptPerson(Person person)
    {
        return person != null && (PeopleOnWay.Contains(person) || PeopleOnWay.Count < PassengerQueueCapacity);
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
            HandleCompletedServiceLitter(passenger);
            
            currentPerson = null;
        }
        
        state = MachineState.Idle;
    }

    private void ReleaseAssignedPassengers()
    {
        if (PassengerManager.Instance == null)
        {
            return;
        }

        PassengerManager.Instance.HandleDestroyedQueueTarget(this);
        PeopleOnWay.Clear();
        currentPerson = null;
    }

    public float GetEstimatedQueueWaitTime()
    {
        int queuedCount = Mathf.Max(PeopleOnWay.Count, currentPerson != null ? 1 : 0);
        return queuedCount * EstimatedServiceDuration;
    }

    protected void SetNeedIconIdle(SpriteRenderer icon)
    {
        if (icon != null)
        {
            icon.color = IdleNeedIconColor;
        }
    }

    protected void SetNeedIconActive(SpriteRenderer icon, Passenger.NeedType needType)
    {
        if (icon != null)
        {
            icon.color = GetNeedIconColor(needType);
        }
    }

    protected SpriteRenderer ResolveNeedIcon(SpriteRenderer assignedIcon)
    {
        if (assignedIcon != null)
        {
            return assignedIcon;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "NeedIcon")
            {
                SpriteRenderer resolvedIcon = child.GetComponentInChildren<SpriteRenderer>(true);
                if (resolvedIcon != null)
                {
                    return resolvedIcon;
                }
            }
        }

        return null;
    }

    protected Color GetNeedIconColor(Passenger.NeedType needType)
    {
        switch (needType)
        {
            case Passenger.NeedType.Hunger:
                return HungerNeedIconColor;
            case Passenger.NeedType.Thirst:
                return ThirstNeedIconColor;
            case Passenger.NeedType.Hygiene:
                return HygieneNeedIconColor;
            default:
                return IdleNeedIconColor;
        }
    }

    protected virtual void HandleCompletedServiceLitter(Passenger passenger)
    {
        if (PassengerManager.Instance == null || passenger == null || potentialLitterPrefabs == null || potentialLitterPrefabs.Count == 0)
        {
            return;
        }

        PassengerManager.Instance.AddPotentialLitterToPassenger(passenger, potentialLitterPrefabs);
    }

    protected abstract void DeliverService(Passenger passenger);
}
