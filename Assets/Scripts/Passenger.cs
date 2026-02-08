using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Passenger : Staff
{
    public TrainService assignedTrainService;

    public float TimeToGoToPlatform;
    
    public bool hasTicket = false;
    public bool isTicketEvader = false;
    public bool hasBypassedBarrier = false;
    
    public QueuableObject currentTarget; // The current target the passenger is moving towards (e.g., ticket machine, vending machine)
    public Vector3 trainWaitPosition; // Position where the passenger waits for the train
    public float timeOfLastPlatformWander; // Time since the passenger started wandering on the platform
    
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
        InteractingWithTarget
    }
    
    public passengerSpecialTargets currentSpecialTarget = passengerSpecialTargets.None;
    public enum passengerSpecialTargets // None-QueuableObject targets, these are for things like wandering on the platform or moving to the exit
    {
        None,
        Platform,
        TrainDoor,
        Exit
    }
}
