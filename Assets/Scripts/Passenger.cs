using UnityEngine;
using UnityEngine.AI;

public class Passenger : MonoBehaviour
{
    public TrainService assignedTrainService;

    public float patienceLevel = 1f; // Depletes during unnecessary waiting and may cause the passenger to leave
    
    public TicketMachineController targetTicketMachine;
    
    public NavMeshAgent agent;
    
    public passengerStates currentState = passengerStates.LocatingTrainTicketSource;
    public enum passengerStates
    {
        LocatingTrainTicketSource,
        WaitingForTicket,
        GoingToPlatform,
        WaitingForTrain,
        BoardingTrain,
        OnTrain,
    }
    
    public Vector3 trainWaitPosition; // Position where the passenger waits for the train
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }
}
