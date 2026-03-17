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
    
    public bool hasFailedNeed = false;
    
    public QueuableObject currentTarget;
    public Vector3 trainWaitPosition;
    
    public bool needsComfort;
    public bool needsSatiation;
    public bool needsHydration;
    public bool needsHygiene;
    
    public passengerMasterStates currentMasterState = passengerMasterStates.InStation;
    public enum passengerMasterStates { InStation, OnPlatform, OnTrain }
    
    public passengerSubStates currentSubState = passengerSubStates.Idle;
    public enum passengerSubStates { Idle, MovingToTarget, InteractingWithSomething }
    
    public passengerSpecialTargets currentSpecialTarget = passengerSpecialTargets.None;
    public enum passengerSpecialTargets { None, Platform, TrainDoor, Exit }
    
    public enum NeedType { None, Comfort, Satiation, Hydration, Hygiene }

    protected override void OnTick(float tickLength) { }

    protected override void Awake()
    {
        base.Awake();
        
        needsComfort = Random.value > 0.5f;
        needsSatiation = Random.value > 0.5f;
        needsHydration = Random.value > 0.5f;
        needsHygiene = Random.value > 0.5f;
    }
    
    public NeedType GetNextNeed()
    {
        if (needsComfort) return NeedType.Comfort;
        if (needsSatiation) return NeedType.Satiation;
        if (needsHydration) return NeedType.Hydration;
        if (needsHygiene) return NeedType.Hygiene;
        
        return NeedType.None;
    }

    public void ClearNeed(NeedType need)
    {
        if (need == NeedType.Comfort) needsComfort = false;
        if (need == NeedType.Satiation) needsSatiation = false;
        if (need == NeedType.Hydration) needsHydration = false;
        if (need == NeedType.Hygiene) needsHygiene = false;
    }
}