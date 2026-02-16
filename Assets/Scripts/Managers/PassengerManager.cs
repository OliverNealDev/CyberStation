using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    public List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    private Vector3 passengerSpawnPoint;
    
    [Header("Station Areas")]
    public Transform concourseCenter; 
    public float concourseWanderRadius = 8f;

    public List<GameObject> litterPrefabs = new List<GameObject>();
    
    public int platformLength = 64; 

    private float tickLength = 0.1f; 
    private float tickTimer;

    public bool autoSpawnPassengers = false;
    public int minPassengers = 4;

    public bool spawnPerSecond = false;
    public int passengersPerSecond = 1;
    
    public List<GameObject> passengerBodyModels = new List<GameObject>();
    public List<GameObject> passengerHairModels = new List<GameObject>();
    public List<GameObject> passengerHeadModels = new List<GameObject>();
    
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        passengerSpawnPoint = GameObject.FindGameObjectWithTag("PassengerSpawnPoint").transform.position;
        
        InvokeRepeating("DropLitter", 1, 1);
        InvokeRepeating("SpawnPassengers", 1, 1);

        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnPlatformAnnounced += HandlePlatformAnnouncement;
        }
    }

    void OnDestroy()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnPlatformAnnounced -= HandlePlatformAnnouncement;
        }
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
            if (passenger == null) continue;

            NavMeshAgent agent = passenger.navAgent;
            
            switch (passenger.currentSubState)
            {
                case Passenger.passengerSubStates.Idle:
                    DecideNextAction(passenger, 0); 
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
                                    UnregisterPassenger(passenger);
                                    break;
                            }
                            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
                        }
                        break; 
                    }
                    
                    if (passenger.currentTarget == null) 
                    {
                        DecideNextAction(passenger, 0);
                    }
                    else
                    {
                        UpdateQueuePosition(passenger); 
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
    
    void DecideNextAction(Passenger passenger, int priorityNeed)
    {
        if (passenger.currentTarget != null)
        {
            passenger.currentTarget.RemovePerson(passenger);
            passenger.currentTarget = null;
        }
        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;

        if (passenger.assignedTrainService == null)
        {
            LeaveStation(passenger);
            return;
        }

        if (!TrainManager.Instance.activeTrainServices.Contains(passenger.assignedTrainService))
        {
            passenger.assignedTrainService = null;
            LeaveStation(passenger);
            return;
        }
        
        var needsInPriorityOrder = passenger.GetNeedsInPriorityOrder();
        Passenger.NeedType nextNeed = needsInPriorityOrder[priorityNeed];
        
        switch (passenger.currentMasterState)
        {
            case Passenger.passengerMasterStates.InStation:
                if (!passenger.hasTicket && passenger.isTicketEvader)
                {
                    MoveToConcourse(passenger); 
                    return;
                }
                
                if (!passenger.hasTicket && !passenger.isTicketEvader)
                {
                    FindTicketMachine(passenger);
                    return;
                }
                
                // If they have a ticket, they wait in the concourse until announcement
                MoveToConcourse(passenger);
                break;
            
            case Passenger.passengerMasterStates.OnPlatform:
                if (Time.time - passenger.timeOfLastPlatformWander > 20f)
                {
                    // Wander slightly on platform
                    Vector3 randomOffset = Random.insideUnitSphere * 3f;
                    randomOffset.y = 0;
                    passenger.navAgent.SetDestination(passenger.transform.position + randomOffset);
                    passenger.timeOfLastPlatformWander = Time.time;
                }
                break;
        }
    }
    
    // Triggered by TrainManager when a train is 10s away
    private void HandlePlatformAnnouncement(ScheduledArrival arrival, int platformID)
    {
        TrainManager.Platform targetPlat = TrainManager.Instance.activePlatforms.Find(p => p.platformNumber == platformID);
        if (targetPlat == null) return;

        foreach (var passenger in activePassengers)
        {
            // If they have the ticket and are currently waiting in the station/concourse
            if (passenger.assignedTrainService == arrival.service && 
                passenger.currentMasterState == Passenger.passengerMasterStates.InStation)
            {
                SendPassengerToPlatform(passenger, targetPlat);
            }
        }
    }

    private void MoveToConcourse(Passenger passenger)
    {
        Vector3 wanderTarget = passengerSpawnPoint; // Default fallback

        if (concourseCenter != null)
        {
            Vector2 rand = Random.insideUnitCircle * concourseWanderRadius;
            wanderTarget = concourseCenter.position + new Vector3(rand.x, 0, rand.y);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(wanderTarget, out hit, 4, NavMesh.AllAreas))
        {
            passenger.navAgent.SetDestination(hit.position);
            passenger.navAgent.stoppingDistance = 1f;
        }
        else
        {
             passenger.navAgent.SetDestination(wanderTarget);
        }
        
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        // We treat Concourse wandering as "MovingToTarget" with no special target, 
        // so they will eventually go Idle and call DecideNextAction again (looping the wander).
    }

    private void SendPassengerToPlatform(Passenger passenger, TrainManager.Platform platform)
    {
        Vector3 targetPos = platform.trainStopPosition.position;

        if (platform.passengerWaitingArea != null)
        {
            Vector3 randomLocalPoint = new Vector3(
                Random.Range(-0.5f, 0.5f), 
                0f, 
                Random.Range(-0.5f, 0.5f)
            );
            
            targetPos = platform.passengerWaitingArea.TransformPoint(randomLocalPoint);
        }
        else
        {
            float waitPositionX = Random.Range(0, platformLength);
            float waitPositionZ = platform.trainStopPosition.position.z - 6f; 
            targetPos = new Vector3(waitPositionX, 0, waitPositionZ);
        }

        passenger.trainWaitPosition = targetPos;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(passenger.trainWaitPosition, out hit, 5, NavMesh.AllAreas))
        {
            passenger.trainWaitPosition = hit.position;
            passenger.navAgent.SetDestination(passenger.trainWaitPosition);
            passenger.navAgent.stoppingDistance = Random.Range(0.2f, 1.0f); 
        }
        else
        {
            passenger.navAgent.SetDestination(passenger.trainWaitPosition);
        }
        
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.Platform;
    }

    public void DropLitter()
    {
        if (litterPrefabs.Count == 0) return;
        if (activePassengers.Count == 0) return;
        
        List<Passenger> passengersLittered = new List<Passenger>();
        int passengersToLitter = Mathf.FloorToInt(Random.Range(0, activePassengers.Count / 100f)); 
        if (passengersToLitter == 0)
        {
            if (Random.Range(0, 1000) < activePassengers.Count) 
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
            if (passengersLittered.Contains(randomPassenger)) continue; 

            GameObject litterPrefab = litterPrefabs[Random.Range(0, litterPrefabs.Count)];
            Instantiate(litterPrefab, new Vector3(randomPassenger.transform.position.x, 1.05f, randomPassenger.transform.position.z), Quaternion.identity);
            passengersLittered.Add(randomPassenger);
        }
    }
    
    private void LeaveStation(Passenger passenger)
    {
        if (!passenger.navAgent) return; 
        
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
    
    public void ReceiveTicket(Passenger passenger)
    {
        if (passenger != null)
        {
            if (passenger.currentTarget != null)
            {
                passenger.currentTarget.RemovePerson(passenger);
                passenger.currentTarget = null;
            }
            passenger.hasTicket = true;
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
            if (passenger.assignedTrainService != null)
            {
                EconomyManager.Instance.AddMoney(passenger.assignedTrainService.trainData.costPerRide);
            }
        }
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
    
    public void TrainServiceEnded(TrainService concludingService)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
        
            if (passenger == null) continue;

            if (passenger.assignedTrainService == concludingService)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.RefundTicket(passenger);
                }

                LeaveStation(passenger);
                
                if (Random.Range(0f, 1f) < 0.25f) 
                {
                    if (passenger.dialogueData != null)
                    {
                        string line = passenger.dialogueData.GetRandomLine(DialogueType.TrainServiceEnded);
                        passenger.Dialogue(passenger, line, Color.softRed, Random.Range(3f, 8f));
                    }
                }
                
                passenger.assignedTrainService = null;
            }
        }
    }
    
    public void OnCaughtBySecurity(Passenger passenger, SecurityGuard securityGuard)
    {
        UnassignTarget(passenger);
        
        passenger.hasBypassedBarrier = false; 
        passenger.isBeingEscorted = true;
        
        passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;
        
        StartCoroutine(Person.ExecuteAfterDelay(4, () => LeaveStation(passenger)));
        StartCoroutine(Person.ExecuteAfterDelay(4, () => PayEvasionFine(passenger, securityGuard)));
        
        StartCoroutine(Person.ExecuteAfterDelay(2, () => ReplyToBeingCaught(passenger)));
    }
    
    void PayEvasionFine(Passenger passenger, SecurityGuard securityGuard)
    {
        EconomyManager.Instance.AddMoney(50);
        WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
            "+$50", 
            securityGuard.transform.position + Vector3.up * 7f, 
            Color.darkGreen);
    }

    void ReplyToBeingCaught(Passenger passenger)
    {
        passenger.Dialogue(passenger, passenger.dialogueData.GetRandomLine(DialogueType.CaughtBySecurity), Color.white, 2);
    }

    void CaughtEmoji(Passenger passenger)
    {
        passenger.Expression(passenger, passenger.expressionData.policeOfficer, 3600f);
    }

    void SpawnPassenger()
    {
        if (TrainManager.Instance.activeTrainServices.Count == 0) return;
        
        Passenger newPassenger = Instantiate(passengerPrefab, passengerSpawnPoint + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0), Quaternion.identity).GetComponent<Passenger>();
        newPassenger.transform.parent = transform;

        SpawnPassengerModels(newPassenger);

        newPassenger.assignedTrainService = TrainManager.Instance.AssignTrainServiceToPassenger(); 
        newPassenger.isTicketEvader = Random.Range(1, 100) <= 5;
        
        RegisterPassenger(newPassenger);
        
        newPassenger.TimeToGoToPlatform = Time.time + Random.Range(10f, 60f); 
        
        DecideNextAction(newPassenger, 0);
    }
    
    void SpawnPassengerModels(Passenger passenger)
    {
        GameObject bodyModel = passengerBodyModels[Random.Range(0, passengerBodyModels.Count)];
        GameObject hairModel = passengerHairModels[Random.Range(0, passengerHairModels.Count)];
        GameObject headModel = passengerHeadModels[Random.Range(0, passengerHeadModels.Count)];

        GameObject bodyInstance = Instantiate(bodyModel, passenger.transform);
        GameObject hairInstance = Instantiate(hairModel, passenger.transform);
        GameObject headInstance = Instantiate(headModel, passenger.transform);
        
        Material skinMaterial = GlobalPersonVisuals.Instance.GetRandomSkinMaterial();
        Material hairMaterial = GlobalPersonVisuals.Instance.GetRandomHairMaterial();

        foreach (Transform child in hairInstance.transform)
        {
            child.GetComponent<MeshRenderer>().material = hairMaterial;
        }
        
        headInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material = skinMaterial;
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