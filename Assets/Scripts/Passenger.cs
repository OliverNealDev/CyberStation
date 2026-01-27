using UnityEngine;

public class Passenger : MonoBehaviour
{
    public TrainService assignedTrainService;

    public float patienceLevel = 1f; // Depletes during unnecessary waiting and may cause the passenger to leave
    
    public passengerStates currentState = passengerStates.GoingToPlatform;
    public enum passengerStates
    {
        NeedsTicket,
        GoingToPlatform,
        WaitingForTrain,
        BoardingTrain,
        OnTrain,
    }
    
    public Vector3 trainWaitPosition; // Position where the passenger waits for the train
}
