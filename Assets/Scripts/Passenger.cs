using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Passenger : MonoBehaviour
{
    public TrainService assignedTrainService;
    
    public bool hasTicket = false;
    public bool isTicketEvader = false;
    public bool hasBypassedBarrier = false;

    // Needs
    public float patience = 1f;
    public float satiation = 1f;
    public float hydration = 1f;
    public float hygiene = 1f;
    
    public QueuableObject currentTarget;
    
    public NavMeshAgent agent;
    
    public passengerStates currentState = passengerStates.Ticket_FindingMachine;
    public enum passengerStates
    {
        Ticket_FindingMachine,
        Ticket_Queueing,
        
        Platform_Travelling,
        Platform_Waiting,
        
        Train_Boarding,
        Train_Seated,
        
        LeaveStation,
        LeavingStation
    }
    
    public Vector3 trainWaitPosition; // Position where the passenger waits for the train
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Random.Range(3f, 4f);
    }
}
