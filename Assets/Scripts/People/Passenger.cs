using System.Collections.Generic;
using UnityEngine;

public class Passenger : Person
{
    public TrainService assignedTrainService;
    public float TimeToGoToPlatform;
    
    public bool hasTicket = false;
    public bool isTicketEvader = false;
    public bool hasBypassedBarrier = false;
    public bool isBeingEscorted = false;
    public bool hasBeenInspected = false;
    
    public QueuableObject currentTarget;
    public Vector3 trainWaitPosition;
    public float timeOfLastPlatformWander;
    
    public float comfort = 100f;
    public float satiation = 100f;
    public float hydration = 100f;
    public float hygiene = 100f;
    
    public passengerMasterStates currentMasterState = passengerMasterStates.InStation;
    public enum passengerMasterStates
    {
        InStation,
        OnPlatform,
        OnTrain
    }
    
    public passengerSubStates currentSubState = passengerSubStates.Idle;
    public enum passengerSubStates
    {
        Idle,
        MovingToTarget,
        InteractingWithSomething
    }
    
    public passengerSpecialTargets currentSpecialTarget = passengerSpecialTargets.None;
    public enum passengerSpecialTargets
    {
        None,
        Platform,
        TrainDoor,
        Exit
    }
    
    public enum NeedType
    {
        None,
        Comfort,
        Satiation,
        Hydration,
        Hygiene
    }

    protected override void OnTick(float tickLength)
    {
    }

    protected override void Awake()
    {
        base.Awake();
        
        comfort = Random.Range(50f, 100f);
        satiation = Random.Range(50f, 100f);
        hydration = Random.Range(50f, 100f);
        hygiene = Random.Range(50f, 100f);
    }
    
    public void CalculateNeeds(float delta)
    {
        comfort = Mathf.Max(0f, comfort - delta * needReductionRate);
        satiation = Mathf.Max(0f, satiation - delta * needReductionRate);
        hydration = Mathf.Max(0f, hydration - delta * needReductionRate);
        hygiene = Mathf.Max(0f, hygiene - delta * needReductionRate);
    }
    
    public NeedType GetMostUrgentNeed()
    {
        NeedType mostUrgent = NeedType.None;
        float lowestValue = 100f;

        if (comfort < lowestValue) { lowestValue = comfort; mostUrgent = NeedType.Comfort; }
        if (satiation < lowestValue) { lowestValue = satiation; mostUrgent = NeedType.Satiation; }
        if (hydration < lowestValue) { lowestValue = hydration; mostUrgent = NeedType.Hydration; }
        if (hygiene < lowestValue) { lowestValue = hygiene; mostUrgent = NeedType.Hygiene; }

        return mostUrgent;
    }
}