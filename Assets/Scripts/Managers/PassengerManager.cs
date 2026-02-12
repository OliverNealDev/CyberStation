using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    public List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    private Vector3 passengerSpawnPoint;
    
    public List<GameObject> litterPrefabs = new List<GameObject>();
    
    public int platformLength = 64; 

    private float tickLength = 0.1f; 
    private float tickTimer;

    public bool autoSpawnPassengers = false;
    public int minPassengers = 4;

    public bool spawnPerSecond = false;
    public int passengersPerSecond = 1;
    
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        passengerSpawnPoint = GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position;
        
        InvokeRepeating("DropLitter", 1, 1);
        
        InvokeRepeating("SpawnPassengers", 1, 1);
    }
    
        void SpawnPassengers()
        {
            if (spawnPerSecond)
            {
                for (int i = 0; i < passengersPerSecond; i++)
                {
                    SpawnPassenger();
                }
            }
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

            NavMeshAgent agent = passenger.navAgent;
            
            switch (passenger.currentSubState)
            {
                case Passenger.passengerSubStates.Idle:
                    DecideNextAction(passenger, 0); // Decide next state based on highest priority need
                    break;
                
                case Passenger.passengerSubStates.MovingToTarget:
                    if (passenger.currentSpecialTarget != Passenger.passengerSpecialTargets.None)
                    {
                        if (HasReachedTarget(passenger)) 
                        {
                            switch (passenger.currentSpecialTarget)
                            {
                                case Passenger.passengerSpecialTargets.Platform:
                                    passenger.timeOfLastPlatformWander = Time.time;
                                    passenger.currentMasterState = Passenger.passengerMasterStates.OnPlatform;
                                    passenger.currentSubState = Passenger.passengerSubStates.Idle;
                                    break;
                                case Passenger.passengerSpecialTargets.TrainDoor:
                                    UnregisterPassenger(passenger);
                                    break;
                                case Passenger.passengerSpecialTargets.Exit:
                                    EconomyManager.Instance.AddMoney(50);
                                    UnregisterPassenger(passenger);
                                    break;
                            }
                            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
                        }
        
                        break; 
                    }
                    
                    if (passenger.currentTarget == null) // Null check for safety (target is destroyed etc.)
                    {
                        DecideNextAction(passenger, 0);
                    }
                    else
                    {
                        UpdateQueuePosition(passenger); // pre-emptively update queue position
                        if (HasReachedTarget(passenger))
                        {
                            if (passenger.currentTarget != null)
                            {
                                int queueIndex = passenger.currentTarget.PeopleOnWay.IndexOf(passenger);
                                if (queueIndex == 0 && passenger.currentTarget.IsAvailable)
                                {
                                    passenger.currentTarget.ProcessInteraction(passenger);
                                    passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;
                                }
                            }
                        }
                    }
                    break;
                
                case Passenger.passengerSubStates.InteractingWithSomething:
                    UpdateQueuePosition(passenger);
                    break;
            }
        }
    }
    
    void DecideNextAction(Passenger passenger, int priorityNeed) // priorityNeed should be 0 but will increment if need can't be fulfilled
    {
        // RESET CODE - ensures decision is fresh without influence
        if (passenger.currentTarget != null)
        {
            Debug.LogWarning("Passenger " + passenger.name + " had a non-null target during decision reset. Removing passenger from target's list to prevent bugs.");
            Debug.LogWarning(passenger.currentTarget.name);
            passenger.currentTarget.RemovePerson(passenger);
            passenger.currentTarget = null;
        }
        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
        //
        
        var needsInPriorityOrder = passenger.GetNeedsInPriorityOrder();
        Passenger.NeedType nextNeed = needsInPriorityOrder[priorityNeed];
        
        switch (passenger.currentMasterState)
        {
            case Passenger.passengerMasterStates.InStation:
                if (!passenger.hasTicket && 
                    passenger.isTicketEvader)
                {
                    MoveToPlatformPosition(passenger);
                    return;
                }
                
                if (!passenger.hasTicket && 
                         !passenger.isTicketEvader)
                {
                    FindTicketMachine(passenger);
                    Debug.Log("Passenger " + passenger.name + " is looking for a ticket machine.");
                    return;
                }
                
                if (passenger.TimeToGoToPlatform < Time.time)
                {
                    MoveToPlatformPosition(passenger);
                    return;
                }

                switch (nextNeed)
                {
                    case Person.NeedType.Comfort:
                        break;
                    case Person.NeedType.Satiation:
                        break;
                    case Person.NeedType.Hydration:
                        break;
                    case Person.NeedType.Hygiene:
                        break;
                }
                
                MoveToPlatformPosition(passenger); // If needs are all fulfilled or no actionable needs, head to platform to wait for train, as that's the main purpose of passengers being in the station
                break;
            
            case Passenger.passengerMasterStates.OnPlatform:
                if (Time.time - passenger.timeOfLastPlatformWander > 20f) // If it's been more than 10 seconds since wandering, force wander again to prevent passengers just standing still on the platform
                {
                    MoveToPlatformPosition(passenger);
                }
                break;
        }
    }

    public void DropLitter()
    {
        if (litterPrefabs.Count == 0) return;
        if (activePassengers.Count == 0) return;
        
        List<Passenger> passengersLittered = new List<Passenger>();
        int passengersToLitter = Mathf.FloorToInt(Random.Range(0, activePassengers.Count / 100f)); // Up to 1% of passengers will drop litter per second
        if (passengersToLitter == 0)
        {
            if (Random.Range(0, 1000) < activePassengers.Count) // Up to 10% chance per second for 1 passenger to drop litter, to add some variability and ensure litter is dropped even with low passenger counts
            {
                passengersToLitter = 1;
            }
            else
            {
                return;
            }
        }
        
        for (int i = 0; i < passengersToLitter; i++) 
        { 
            Passenger randomPassenger = activePassengers[Random.Range(0, activePassengers.Count)];
            if (passengersLittered.Contains(randomPassenger)) continue; // prevents the same passenger from littering multiple times in the same drop event, which can look erroneous

            GameObject litterPrefab = litterPrefabs[Random.Range(0, litterPrefabs.Count)];
            Instantiate(litterPrefab, new Vector3(randomPassenger.transform.position.x, 1.05f, randomPassenger.transform.position.z), Quaternion.identity);
            passengersLittered.Add(randomPassenger);
        }
    }
    
    private void LeaveStation(Passenger passenger)
    {
        if (!passenger.navAgent) return; // safety check in case agent was destroyed or not assigned for some reason
        
        UnassignTarget(passenger);
        
        Vector3 exitPosition = NavMesh.SamplePosition(passengerSpawnPoint, out NavMeshHit validPosition, 4, NavMesh.AllAreas) ? validPosition.position : passengerSpawnPoint;
        passenger.navAgent.SetDestination(exitPosition);
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        passenger.navAgent.stoppingDistance = 1f;

        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.Exit;
        passenger.currentMasterState = Passenger.passengerMasterStates.InStation;
    }
    
    private void UnassignTarget(Passenger passenger)
    {
        if (passenger.currentTarget != null)
        {
            passenger.currentTarget.RemovePerson(passenger);
            passenger.currentTarget = null;
        }

        if (passenger.currentSpecialTarget == Passenger.passengerSpecialTargets.TrainDoor)
        {
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
        }
        if(passenger.navAgent != null) passenger.navAgent.ResetPath();
    }
    
    private void FindTicketMachine(Passenger passenger)
    {
        TicketMachineController bestMachine = TicketMachineManager.Instance.leastOccupiedTicketMachine;
        if (bestMachine != null)
        {
            passenger.currentTarget = bestMachine;
            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentTarget.AssignPerson(passenger);
                            
            Vector3 frontOfMachinePosition = passenger.currentTarget.transform.position + passenger.currentTarget.transform.forward * 2f;
                            
            NavMeshHit hit;
            if(NavMesh.SamplePosition(frontOfMachinePosition, out hit, 4, NavMesh.AllAreas))
            {
                passenger.navAgent.SetDestination(hit.position);
            }
            else
            {
                passenger.navAgent.SetDestination(frontOfMachinePosition);
            }
        }
    }
    
    private void UpdateQueuePosition(Passenger passenger)
    {
        if (passenger.currentTarget == null) return;
        
        int queueIndex = passenger.currentTarget.PeopleOnWay.IndexOf(passenger);
                        
        float baseDistance = 0.25f; 
        float queueSpacing = 1.5f; 
        float newStoppingDistance = baseDistance + (queueIndex * queueSpacing);
                        
        if (Mathf.Abs(passenger.navAgent.stoppingDistance - newStoppingDistance) > 0.1f)
        {
            passenger.navAgent.stoppingDistance = newStoppingDistance;
        }
    }
    
    private void MoveToPlatformPosition(Passenger passenger)
    {
        float waitPositionX = Random.Range(0, platformLength);
        float waitPositionZ = TrainManager.Instance.activePlatforms[0].trainStopPosition.z - 6f; 

        passenger.trainWaitPosition = new Vector3(waitPositionX, 0, waitPositionZ);
                        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(passenger.trainWaitPosition, out hit, 4, NavMesh.AllAreas))
        {
            passenger.trainWaitPosition = hit.position;
            passenger.navAgent.SetDestination(passenger.trainWaitPosition);
            passenger.navAgent.stoppingDistance = Random.Range(2f, 8f); 
        }
        else
        {
            passenger.navAgent.SetDestination(passenger.trainWaitPosition);
            passenger.navAgent.stoppingDistance = Random.Range(2f, 8f); 
        }
        
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.Platform;
    }
    
    public void ReceiveTicket(Passenger passenger)
    {
        passenger.currentTarget.RemovePerson(passenger);
        passenger.currentTarget = null;
        passenger.hasTicket = true;
        passenger.currentSubState = Passenger.passengerSubStates.Idle;
        EconomyManager.Instance.AddMoney(passenger.assignedTrainService.trainData.costPerRide);
    }

    public bool HasReachedTarget(Passenger passenger)
    {
        if (!passenger.navAgent.pathPending && 
            passenger.navAgent.hasPath &&
            passenger.navAgent.remainingDistance <= passenger.navAgent.stoppingDistance)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public void OnTicketBarrierDenial(Passenger passenger)
    {
        if (passenger.isTicketEvader)
        {
            passenger.hasBypassedBarrier = true;
            return;
        }

        if (!passenger.hasTicket)
        {
            LeaveStation(passenger);
        }
    }
    
    public void TrainArrived(TrainService arrivingService)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform &&
                passenger.assignedTrainService == arrivingService)
            {
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
                    if (passenger.currentTarget != null)
                    {
                        passenger.currentTarget.RemovePerson(passenger);
                        passenger.currentTarget = null;
                    }

                    passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
                    passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.TrainDoor;
                }
            }
        }
    }
    
    public void OnCaughtBySecurity(Passenger passenger)
    {
        UnassignTarget(passenger);
        
        passenger.hasBypassedBarrier = false; // untags passenger from being caught by security
        passenger.isBeingEscorted = true;
        
        passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;
        StartCoroutine(Person.ExecuteAfterDelay(4, () => LeaveStation(passenger)));
        StartCoroutine(Person.ExecuteAfterDelay(2, () => ReplyToBeingCaught(passenger)));
    }

    void ReplyToBeingCaught(Passenger passenger)
    {
        passenger.Dialogue(passenger, passenger.dialogueData.GetRandomLine(DialogueType.CaughtBySecurity), Color.white, 2);
    }

    void SpawnPassenger()
    {
        if (TrainManager.Instance.activeTrainServices.Count == 0) return;
        
        Passenger newPassenger = Instantiate(passengerPrefab, passengerSpawnPoint + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0), Quaternion.identity).GetComponent<Passenger>();
        newPassenger.transform.parent = transform;

        newPassenger.assignedTrainService = TrainManager.Instance.AssignTrainServiceToPassenger(); 
        newPassenger.isTicketEvader = Random.Range(1, 100) <= 5;
        
        RegisterPassenger(newPassenger);
        
        newPassenger.TimeToGoToPlatform = Time.time + Random.Range(10f, 60f); // Random time before passenger decides to go to platform, simulating time spent in station before heading to platform
        
        DecideNextAction(newPassenger, 0);
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
