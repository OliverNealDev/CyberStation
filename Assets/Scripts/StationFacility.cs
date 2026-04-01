using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FacilityType
{
    TicketMachine,
    NutrientExtruder,
    SnackPrinter,
    HydratingObelisk,
    BottleDispenser,
    CleansingShower,
    PrivateLavatory,
    RestPad,
    MolecularScrubber
}

public abstract class StationFacility : QueuableObject 
{
    private const float QueueReshuffleIntervalSeconds = 5f;

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

        StartCoroutine(QueueReshuffleLoop());
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

    private IEnumerator QueueReshuffleLoop()
    {
        WaitForSeconds delay = new WaitForSeconds(QueueReshuffleIntervalSeconds);

        while (true)
        {
            yield return delay;
            ReshuffleQueue();
        }
    }

    private void ReshuffleQueue()
    {
        if (PeopleOnWay == null || PeopleOnWay.Count < 2)
        {
            return;
        }

        PeopleOnWay.RemoveAll(person => person == null);
        if (PeopleOnWay.Count < 2)
        {
            return;
        }

        Person processingPerson = currentPerson;
        bool keepProcessingPersonAtFront = processingPerson != null && PeopleOnWay.Remove(processingPerson);
        Vector3 queueFrontPosition = GetQueueFrontPosition();

        PeopleOnWay.Sort((left, right) =>
        {
            float leftDistance = GetFlatDistanceSqr(left, queueFrontPosition);
            float rightDistance = GetFlatDistanceSqr(right, queueFrontPosition);
            return leftDistance.CompareTo(rightDistance);
        });

        if (keepProcessingPersonAtFront)
        {
            PeopleOnWay.Insert(0, processingPerson);
        }
    }

    private Vector3 GetQueueFrontPosition()
    {
        if (queueLineMode == QueueLineMode.StoppingDistance)
        {
            return transform.position + (transform.forward * stoppingDistanceTargetDistance);
        }

        if (queueStyle == QueueStyle.FrontOnly)
        {
            return transform.position + (transform.forward * baseDistance);
        }

        return transform.position;
    }

    private float GetFlatDistanceSqr(Person person, Vector3 targetPosition)
    {
        if (person == null)
        {
            return Mathf.Infinity;
        }

        Vector3 offset = person.transform.position - targetPosition;
        offset.y = 0f;
        return offset.sqrMagnitude;
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
