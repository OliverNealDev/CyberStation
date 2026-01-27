using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    private List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    private Vector3 passengerSpawnPoint;
    
    public int platformLength = 64; // Length of the platform in units from 0

    private float tickLength = 0.05f; // 20 ticks per second
    private float tickTimer;
    
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        passengerSpawnPoint = GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position;
        
        InvokeRepeating("ok", 1, 1);
    }

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickLength)
        {
            LogicUpdate();
            tickTimer = 0f;
        }
    }

    void ok()
    {
        SpawnPassenger();
    }

    void LogicUpdate()
    {
        if (activePassengers.Count < 5)
        {
            SpawnPassenger();
        }
        
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            NavMeshAgent agent = passenger.gameObject.GetComponent<NavMeshAgent>();
            
            switch (passenger.currentState)
            {
                case Passenger.passengerStates.NeedsTicket:

                    break;
                
                case Passenger.passengerStates.GoingToPlatform:
                    if (passenger.trainWaitPosition == Vector3.zero)
                    {
                        float waitPositionX = Random.Range(0, platformLength);
                        float waitPositionY = 0;
                        float waitPositionZ = TrainManager.Instance.activePlatforms[0].trainStopPosition.z - 6f; // Prevents passengers waiting directly on the track

                        passenger.trainWaitPosition = new Vector3(waitPositionX, 0, waitPositionZ);
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(passenger.trainWaitPosition, out hit, 4, NavMesh.AllAreas))
                        {
                            passenger.trainWaitPosition = hit.position;
                            agent.SetDestination(passenger.trainWaitPosition);
                            agent.stoppingDistance = Random.Range(2f, 8f); // Scatters passenger distance to platform edge
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

        newPassenger.assignedTrainService = TrainManager.Instance.AssignTrainServiceToPassenger(); // Assign train based on train capacity weightings
        
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
