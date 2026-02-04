using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    private List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    private Vector3 passengerSpawnPoint;
    
    public int platformLength = 64; 

    private float tickLength = 0.05f; 
    private float tickTimer;

    public bool autoSpawnPassengers = false;
    public int minPassengers = 4;
    
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        passengerSpawnPoint = GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position;
        
        //InvokeRepeating("ok", 1, 2);
    }
    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickLength)
        {
            LogicUpdate();
            tickTimer = 0f;
        }
        
        if (Keyboard.current.pKey.wasPressedThisFrame) 
        {
            SpawnPassenger();
        }
        
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            autoSpawnPassengers = !autoSpawnPassengers; 
        }
    }
    void LogicUpdate()
    {
        if (activePassengers.Count < minPassengers && autoSpawnPassengers)
        {
            SpawnPassenger();
        }
        
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];

            NavMeshAgent agent = passenger.agent;
            
            switch (passenger.currentState)
            {
                case Passenger.passengerStates.Ticket_FindingMachine:
                    if (passenger.currentTarget == null)
                    {
                        //List<TicketMachineController> availableTicketMachines = TicketMachineManager.Instance.AvailableTicketMachines;
                        TicketMachineController bestMachine = TicketMachineManager.Instance.leastOccupiedTicketMachine;
                        if (bestMachine != null)
                        {
                            passenger.currentTarget = bestMachine; /*availableTicketMachines[Random.Range(0, availableTicketMachines.Count)];*/
                            passenger.currentTarget.AssignPassenger(passenger);
                            
                            Vector3 frontOfMachinePosition = passenger.currentTarget.transform.position + passenger.currentTarget.transform.forward * 2f;
                            
                            NavMeshHit hit;
                            if(NavMesh.SamplePosition(frontOfMachinePosition, out hit, 4, NavMesh.AllAreas))
                            {
                                agent.SetDestination(hit.position);
                            }
                            else
                            {
                                agent.SetDestination(frontOfMachinePosition);
                            }
                        }
                    }
                    else
                    {
                        int queueIndex = passenger.currentTarget.PassengersOnWay.IndexOf(passenger);
                        
                        float baseDistance = 0.25f; 
                        float queueSpacing = 1.5f; 
                        float newStoppingDistance = baseDistance + (queueIndex * queueSpacing);
                        
                        if (Mathf.Abs(agent.stoppingDistance - newStoppingDistance) > 0.1f)
                        {
                            agent.stoppingDistance = newStoppingDistance;
                        }

                        if (queueIndex == 0)
                        {
                            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                            {
                                passenger.currentState = Passenger.passengerStates.Ticket_Queueing;
                                passenger.currentTarget.ProcessInteraction(passenger); 
                            }
                        }
                    }
                    break;
                case Passenger.passengerStates.Ticket_Queueing:
                    if (passenger.currentTarget == null)
                    {
                        passenger.currentState = Passenger.passengerStates.Ticket_FindingMachine;
                    }
                    break;
                
                case Passenger.passengerStates.Platform_Travelling:
                    if (passenger.trainWaitPosition == Vector3.zero)
                    {
                        float waitPositionX = Random.Range(0, platformLength);
                        float waitPositionZ = TrainManager.Instance.activePlatforms[0].trainStopPosition.z - 6f; 

                        passenger.trainWaitPosition = new Vector3(waitPositionX, 0, waitPositionZ);
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(passenger.trainWaitPosition, out hit, 4, NavMesh.AllAreas))
                        {
                            passenger.trainWaitPosition = hit.position;
                            agent.SetDestination(passenger.trainWaitPosition);
                            agent.stoppingDistance = Random.Range(2f, 8f); 
                        }
                    }
                    else
                    {
                        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
                        {
                            passenger.currentState = Passenger.passengerStates.Platform_Waiting;
                        }
                    }
                    break;
                case Passenger.passengerStates.Platform_Waiting:

                    break;
                
                case Passenger.passengerStates.Train_Boarding:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        passenger.currentState = Passenger.passengerStates.Train_Seated;
                    }
                    break;
                case Passenger.passengerStates.Train_Seated:
                    UnregisterPassenger(passenger);
                    break;
                
                case Passenger.passengerStates.LeaveStation:
                    Vector3 exitPosition = NavMesh.SamplePosition(passengerSpawnPoint, out NavMeshHit validPosition, 4, NavMesh.AllAreas) ? validPosition.position : passengerSpawnPoint;
                    agent.SetDestination(exitPosition);
                    passenger.currentState = Passenger.passengerStates.LeavingStation;
                    agent.stoppingDistance = 1f;
                    break;
                case Passenger.passengerStates.LeavingStation:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        UnregisterPassenger(passenger);
                    }
                    break;
            }
        }
    }
    
    public void ReceiveTicket(Passenger passenger)
    {
        if (passenger.currentState == Passenger.passengerStates.Ticket_Queueing)
        {
            passenger.currentTarget.RemovePassenger(passenger);
            passenger.currentState = Passenger.passengerStates.Platform_Travelling;
            passenger.currentTarget = null;
            passenger.hasTicket = true;
        }
    }
    
    public void OnTicketBarrierDenial(Passenger passenger)
    {
        switch (passenger.currentState)
        {
            // Innocent passengers will leave the station
            // Ticket evaders will bypass the barrier
            // "Rage Quit" behaviour to provide feedback of player design flaw
            case Passenger.passengerStates.Ticket_FindingMachine:
            case Passenger.passengerStates.Ticket_Queueing:
            case Passenger.passengerStates.Platform_Travelling:
            case Passenger.passengerStates.Platform_Waiting:
            case Passenger.passengerStates.Train_Boarding:
                if (passenger.isTicketEvader)
                {
                    passenger.hasBypassedBarrier = true;
                    return;
                }
                if (!passenger.hasTicket) passenger.currentState = Passenger.passengerStates.LeaveStation;
                break;
        }
    }
    
    public void TrainArrived(TrainService arrivingService)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.currentState == Passenger.passengerStates.Platform_Waiting &&
                passenger.assignedTrainService == arrivingService)
            {
                passenger.currentState = Passenger.passengerStates.Train_Boarding;
                passenger.GetComponent<NavMeshAgent>().stoppingDistance = 1f;
                
                float closestDist = Mathf.Infinity;
                Vector3 closestDoor = Vector3.zero;
                foreach(Vector3 doorPos in arrivingService.physicalTrainInstance.GetDoorPositions())
                {
                    float distToDoor = Vector3.Distance(passenger.transform.position, doorPos);
                    if (distToDoor < closestDist)
                    {
                        closestDist = distToDoor;
                        closestDoor = doorPos;
                    }
                }
                
                if (closestDoor != Vector3.zero)
                {
                    passenger.GetComponent<NavMeshAgent>().SetDestination(closestDoor);
                }
            }
        }
    }
    
    void SpawnPassenger()
    {
        Passenger newPassenger = Instantiate(passengerPrefab, passengerSpawnPoint, Quaternion.identity).GetComponent<Passenger>();
        newPassenger.transform.parent = transform;

        newPassenger.assignedTrainService = TrainManager.Instance.AssignTrainServiceToPassenger(); 
        newPassenger.isTicketEvader = Random.Range(1, 100) <= 5;
        if (newPassenger.isTicketEvader)
        {
            newPassenger.currentState = Passenger.passengerStates.Platform_Travelling;
        }
        
        RegisterPassenger(newPassenger);
    }
    
    public void RegisterPassenger(Passenger passenger)
    {
        if (!activePassengers.Contains(passenger))
        {
            activePassengers.Add(passenger);
        }
    }
    public void UnregisterPassenger(Passenger passenger)
    {
        if (activePassengers.Contains(passenger))
        {
            activePassengers.Remove(passenger);
            Destroy(passenger.gameObject);
        }
    }
}