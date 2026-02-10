using UnityEngine;

public class Passenger : Person
{
    public TrainService assignedTrainService;
    public float TimeToGoToPlatform;
    public GameObject personalCanvas;
    
    public bool hasTicket = false;
    public bool isTicketEvader = false;
    public bool hasBypassedBarrier = false;
    
    public QueuableObject currentTarget;
    public Vector3 trainWaitPosition;
    public float timeOfLastPlatformWander;
    
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

    protected override void OnTick()
    {
        
    }
}