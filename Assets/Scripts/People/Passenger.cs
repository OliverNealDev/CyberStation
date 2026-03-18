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
    
    public bool needsWarmth;
    public bool needsCold;
    public bool needsStabilisation;
    
    public passengerMasterStates currentMasterState = passengerMasterStates.InStation;
    public enum passengerMasterStates { InStation, OnPlatform, OnTrain }
    
    public passengerSubStates currentSubState = passengerSubStates.Idle;
    public enum passengerSubStates { Idle, MovingToTarget, InteractingWithSomething }
    
    public passengerSpecialTargets currentSpecialTarget = passengerSpecialTargets.None;
    public enum passengerSpecialTargets { None, Platform, TrainDoor, Exit }
    
    public enum NeedType { None, Warmth, Cold, Stabilisation }

    protected override void OnTick(float tickLength) { }

    protected override void Awake()
    {
        base.Awake();
        
        needsStabilisation = Random.value > 0.5f;
    }

    public void SetThermalNeeds(bool isWarmTrain)
    {
        if (isWarmTrain)
        {
            needsWarmth = Random.value > 0.5f;
            needsCold = false;
        }
        else
        {
            needsCold = Random.value > 0.5f;
            needsWarmth = false;
        }
    }
    
    public NeedType GetNextNeed()
    {
        if (needsWarmth) return NeedType.Warmth;
        if (needsCold) return NeedType.Cold;
        if (needsStabilisation) return NeedType.Stabilisation;
        
        return NeedType.None;
    }

    public void ClearNeed(NeedType need)
    {
        if (need == NeedType.Warmth) needsWarmth = false;
        if (need == NeedType.Cold) needsCold = false;
        if (need == NeedType.Stabilisation) needsStabilisation = false;
    }
}