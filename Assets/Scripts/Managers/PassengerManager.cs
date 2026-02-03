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

    private bool autoSpawnPassengers = false;
    
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
        if (activePassengers.Count < 1 && autoSpawnPassengers)
        {
            SpawnPassenger();
        }
        
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];

            NavMeshAgent agent = passenger.agent;
            
            switch (passenger.currentState)
            {
                case Passenger.passengerStates.LocatingTrainTicketSource:
                    if (passenger.targetTicketMachine == null)
                    {
                        //List<TicketMachineController> availableTicketMachines = TicketMachineManager.Instance.AvailableTicketMachines;
                        TicketMachineController bestMachine = TicketMachineManager.Instance.leastOccupiedTicketMachine;
                        if (bestMachine != null)
                        {
                            passenger.targetTicketMachine = bestMachine; /*availableTicketMachines[Random.Range(0, availableTicketMachines.Count)];*/
                            passenger.targetTicketMachine.AssignPassengerOnWay(passenger);
                            
                            Vector3 frontOfMachinePosition = passenger.targetTicketMachine.transform.position + passenger.targetTicketMachine.transform.forward * 2f;
                            
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
                        int queueIndex = passenger.targetTicketMachine.PassengersOnWay.IndexOf(passenger);
                        
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
                                passenger.currentState = Passenger.passengerStates.WaitingForTicket;
                                passenger.targetTicketMachine.ProcessTicketRequest(passenger); 
                            }
                        }
                    }
                    break;
                
                case Passenger.passengerStates.WaitingForTicket:
                    if (passenger.targetTicketMachine == null)
                    {
                        passenger.currentState = Passenger.passengerStates.LocatingTrainTicketSource;
                    }
                    break;
                
                case Passenger.passengerStates.GoingToPlatform:
                    if (passenger.trainWaitPosition == Vector3.zero)
                    {
                        float waitPositionX = Random.Range(0, platformLength);
                        float waitPositionY = 0;
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
                        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        {
                            passenger.currentState = Passenger.passengerStates.WaitingForTrain;
                        }
                    }
                    break;
                
                case Passenger.passengerStates.WaitingForTrain:

                    break;
                
                case Passenger.passengerStates.BoardingTrain:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        passenger.currentState = Passenger.passengerStates.OnTrain;
                    }
                    break;
                
                case Passenger.passengerStates.OnTrain:
                    UnregisterPassenger(passenger);
                    break;
            }
        }
    }
    
    public void ReceiveTicket(Passenger passenger)
    {
        if (passenger.currentState == Passenger.passengerStates.WaitingForTicket)
        {
            passenger.targetTicketMachine.RemovePassengerOnWay(passenger);
            passenger.currentState = Passenger.passengerStates.GoingToPlatform;
            passenger.targetTicketMachine = null;
        }
    }
    
    public void TrainArrived(TrainService arrivingService)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.currentState == Passenger.passengerStates.WaitingForTrain &&
                passenger.assignedTrainService == arrivingService)
            {
                passenger.currentState = Passenger.passengerStates.BoardingTrain;
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