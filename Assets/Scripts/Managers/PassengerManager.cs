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
            
            if (passenger.navAgent == null || !passenger.navAgent.isActiveAndEnabled) continue;

            passenger.CalculateNeeds(tickLength);

            NavMeshAgent agent = passenger.navAgent;
            
            switch (passenger.currentSubState)
            {
                case Passenger.passengerSubStates.Idle:
                    DecideNextAction(passenger);
                    
                    if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform)
                    {
                        // Assuming the tracks are to the 'forward' direction of the platform
                        if (passenger.assignedTrainService != null && passenger.assignedTrainService.assignedPlatform != null)
                        {
                            Vector3 lookPos = passenger.transform.position + passenger.assignedTrainService.assignedPlatform.transform.forward;
                            passenger.FaceTarget(lookPos);
                        }
                    }
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
                                    
                                    if (passenger.assignedTrainService.physicalTrainInstance != null && 
                                        passenger.assignedTrainService.physicalTrainInstance.IsAtStation())
                                    {
                                        SendPassengerToTrainDoor(passenger, passenger.assignedTrainService);
                                    }
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
                        else if (passenger.currentSpecialTarget == Passenger.passengerSpecialTargets.Exit)
                        {
                            if (!passenger.navAgent.pathPending && !passenger.navAgent.hasPath)
                            {
                                UnregisterPassenger(passenger);
                            }
                        }
                        break; 
                    }
                    
                    if (passenger.currentTarget == null) 
                    {
                        DecideNextAction(passenger);
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
                                    
                                    // Safety check in case BoardTrain() destroyed the passenger
                                    if (passenger != null && passenger.gameObject != null)
                                    {
                                        passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;
                                    }
                                }
                            }
                        }
                    }
                    break;
                
                case Passenger.passengerSubStates.InteractingWithSomething:
                    UpdateQueuePosition(passenger);
                    
                    if (passenger.currentTarget != null)
                    {
                        passenger.FaceTarget(passenger.currentTarget.transform.position);
                    }
                    break;
            }
            
            if (passenger.navAgent.isOnOffMeshLink && passenger.currentSubState != Passenger.passengerSubStates.InteractingWithSomething)
            {
                StartCoroutine(TraverseBarrierSmoothly(passenger));
            }
        }
    }
    
    public List<Passenger> GetUncheckedPlatformPassengers()
    {
        List<Passenger> uncheckedPassengers = new List<Passenger>();
        
        foreach (Passenger p in activePassengers) 
        {
            if (p != null && p.currentMasterState == Passenger.passengerMasterStates.OnPlatform && !p.hasBeenInspected)
            {
                uncheckedPassengers.Add(p);
            }
        }
        
        return uncheckedPassengers;
    }
    
    private System.Collections.IEnumerator TraverseBarrierSmoothly(Passenger passenger)
    {
        passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;

        UnityEngine.AI.OffMeshLinkData linkData = passenger.navAgent.currentOffMeshLinkData;
        Vector3 startPos = passenger.transform.position;
        Vector3 endPos = linkData.endPos + Vector3.up * passenger.navAgent.baseOffset;

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / passenger.navAgent.speed;
        float timer = 0f;

        while (timer < duration)
        {
            if (passenger == null) yield break; // Safety check

            passenger.transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        passenger.transform.position = endPos;

        passenger.navAgent.CompleteOffMeshLink();
        
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
    }
    
    void DecideNextAction(Passenger passenger)
    {
        if (passenger.currentTarget != null)
        {
            passenger.currentTarget.RemovePerson(passenger);
            passenger.currentTarget = null;
        }
        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;

        passenger.currentSubState = Passenger.passengerSubStates.Idle;
        if (passenger.navAgent != null && passenger.navAgent.isOnNavMesh)
        {
            passenger.navAgent.ResetPath();
        }

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
        
        Passenger.NeedType nextNeed = passenger.GetMostUrgentNeed();
        
        switch (passenger.currentMasterState)
        {
            case Passenger.passengerMasterStates.InStation:
                if (!passenger.hasTicket && passenger.isTicketEvader)
                {
                    MoveToPlatformPosition(passenger);
                    return;
                }
                
                if (!passenger.hasTicket && !passenger.isTicketEvader)
                {
                    GoToFacility(FacilityType.TicketMachine, passenger);
                    return;
                }

                switch (nextNeed)
                {
                    case Passenger.NeedType.Comfort:
                        break;
                    case Passenger.NeedType.Satiation:
                        break;
                    case Passenger.NeedType.Hydration:
                        int RandomHydrationChoice = Random.Range(0, 2);
                        switch (RandomHydrationChoice)
                        {
                            case 0:
                                if (FacilityManager.Instance.HasFacility(FacilityType.DrinkMachine))
                                {
                                    GoToFacility(FacilityType.DrinkMachine, passenger);
                                    return;
                                }
                                break;
                            case 1:
                                if (FacilityManager.Instance.HasFacility(FacilityType.CoffeeMachine))
                                {
                                    GoToFacility(FacilityType.CoffeeMachine, passenger);
                                    return;
                                }
                                break;
                        }
                        break; // If no drink machine is available, just skip the need for now
                    case Passenger.NeedType.Hygiene:
                        break;
                }
                
                MoveToPlatformPosition(passenger);
                break;
            
            case Passenger.passengerMasterStates.OnPlatform:
                if (passenger.assignedTrainService.physicalTrainInstance != null && 
                    passenger.assignedTrainService.physicalTrainInstance.IsAtStation())
                {
                    SendPassengerToTrainDoor(passenger, passenger.assignedTrainService);
                    return;
                }

                if (Time.time - passenger.timeOfLastPlatformWander > 20f)
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
        int passengersToLitter = Mathf.FloorToInt(Random.Range(0, activePassengers.Count / 250f)); 
        if (passengersToLitter == 0)
        {
            if (Random.Range(0, 2500) < activePassengers.Count) 
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

            Vector3 rayStart = randomPassenger.transform.position + (Vector3.up * 0.5f);
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 2f))
            {
                if (hit.collider.CompareTag("UnlitterableSurface"))
                {
                    continue; 
                }
            }

            GameObject litterPrefab = litterPrefabs[Random.Range(0, litterPrefabs.Count)];
            
            if (NavMesh.SamplePosition(randomPassenger.transform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                Instantiate(litterPrefab, navHit.position, Quaternion.identity);
                passengersLittered.Add(randomPassenger);
            }
        }
    }
    
    private void LeaveStation(Passenger passenger)
    {
        if (passenger.navAgent == null || !passenger.navAgent.isActiveAndEnabled || !passenger.navAgent.isOnNavMesh) 
        {
            UnregisterPassenger(passenger);
            return; 
        }
        
        UnassignTarget(passenger);
        
        Vector3 randomizedSpawn = passengerSpawnPoint + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        Vector3 exitPosition = NavMesh.SamplePosition(randomizedSpawn, out NavMeshHit validPosition, 4, NavMesh.AllAreas) ? validPosition.position : passengerSpawnPoint;
        
        passenger.navAgent.SetDestination(exitPosition);
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        passenger.navAgent.stoppingDistance = 2f;

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
        
        if (passenger.navAgent != null && passenger.navAgent.isActiveAndEnabled && passenger.navAgent.isOnNavMesh)
        {
            passenger.navAgent.ResetPath();
        }
    }

    private void GoToFacility(FacilityType type, Passenger passenger)
    {
        StationFacility bestMachine = FacilityManager.Instance.GetLeastOccupiedFacility(type);
        
        if (bestMachine != null)
        {
            passenger.currentTarget = bestMachine;
            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentTarget.AssignPerson(passenger);
                            
            Vector3 frontOfMachine = passenger.currentTarget.transform.position + passenger.currentTarget.transform.forward * 2f;
                            
            if(NavMesh.SamplePosition(frontOfMachine, out NavMeshHit hit, 4, NavMesh.AllAreas))
            {
                passenger.navAgent.SetDestination(hit.position);
            }
            else
            {
                passenger.navAgent.SetDestination(frontOfMachine);
            }
        }
    }
    
    private void UpdateQueuePosition(Passenger passenger)
    {
        if (passenger.currentTarget == null) return;
        
        if (!passenger.navAgent.isActiveAndEnabled) return;
        
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
        PlatformController targetPlatform = passenger.assignedTrainService.assignedPlatform;

        if (targetPlatform == null) return; 
        
        bool foundValidPosition = false;
        NavMeshHit hit = new NavMeshHit();
        int attempts = 0;
        
        while (!foundValidPosition && attempts < 10)
        {
            passenger.trainWaitPosition = targetPlatform.GetRandomWaitPosition();
            
            if (NavMesh.SamplePosition(passenger.trainWaitPosition, out hit, 2f, NavMesh.AllAreas))
            {
                foundValidPosition = true;
            }
            attempts++;
        }
                        
        if (foundValidPosition)
        {
            passenger.navAgent.SetDestination(hit.position);
            passenger.navAgent.stoppingDistance = Random.Range(1f, 3f); 
            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.Platform;
        }
        else
        {
            LeaveStation(passenger);
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
    
    public void MeetNeedFromTarget(Passenger.NeedType needType, Passenger passenger)
    {
        if (passenger != null)
        {
            if (passenger.currentTarget != null)
            {
                passenger.currentTarget.RemovePerson(passenger);
                passenger.currentTarget = null;
            }

            switch (needType)
            {
                case Passenger.NeedType.Comfort:
                    passenger.comfort = 100f;
                    break;
                case Passenger.NeedType.Satiation:
                    passenger.satiation = 100f;
                    break;
                case Passenger.NeedType.Hydration:
                    passenger.hydration = 100f;
                    break;
                case Passenger.NeedType.Hygiene:
                    passenger.hygiene = 100f;
                    break;
            }
            
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
        }
    }

    public bool HasReachedTarget(Passenger passenger)
    {
        if (passenger.navAgent.pathPending) return false;
        
        Vector3 flatPassenger = new Vector3(passenger.transform.position.x, 0, passenger.transform.position.z);
        Vector3 flatDestination = new Vector3(passenger.navAgent.destination.x, 0, passenger.navAgent.destination.z);
        
        float actualDistance = Vector3.Distance(flatPassenger, flatDestination);
        
        if (actualDistance <= passenger.navAgent.stoppingDistance + 0.2f)
        {
            return true;
        }

        return false;
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
        TrainDoorController[] doors = arrivingService.physicalTrainInstance.GetComponentsInChildren<TrainDoorController>();
        
        foreach (TrainDoorController door in doors)
        {
            door.StartBoardingProcess(arrivingService);
        }
        
        // In case the train arrives completely empty and doors instantly flip to Entering
        NotifyDoorsReady(arrivingService);
    }

    public void NotifyDoorsReady(TrainService service)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform &&
                passenger.assignedTrainService == service &&
                passenger.currentTarget == null) 
            {
                SendPassengerToTrainDoor(passenger, service);
            }
        }
    }

    public void SendPassengerToTrainDoor(Passenger passenger, TrainService service)
    {
        if (service.physicalTrainInstance == null) return;

        TrainDoorController[] doors = service.physicalTrainInstance.GetComponentsInChildren<TrainDoorController>();
        if (doors.Length == 0) return;

        float closestDistSqr = Mathf.Infinity;
        TrainDoorController closestDoor = null;
                
        foreach(TrainDoorController door in doors)
        {
            float distSqr = (passenger.transform.position - door.transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestDoor = door;
            }
        }
                
        if (closestDoor != null && closestDoor.state == TrainDoorController.MachineState.Entering)
        {
            if (passenger.currentTarget != null)
            {
                passenger.currentTarget.RemovePerson(passenger);
            }
                    
            passenger.currentTarget = closestDoor;
            closestDoor.AssignPerson(passenger);
                    
            passenger.navAgent.stoppingDistance = 1f;
            passenger.navAgent.SetDestination(closestDoor.transform.position);

            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None; 
        }
    }
    
    public bool ArePassengersWaitingForTrain(TrainService service)
    {
        for (int i = 0; i < activePassengers.Count; i++)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.assignedTrainService == service && 
                passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform)
            {
                return true;
            }
        }
        
        return false;
    }

    public void CallWaitingPassengersToDoors(TrainService service)
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform &&
                passenger.assignedTrainService == service &&
                passenger.currentTarget == null) 
            {
                SendPassengerToTrainDoor(passenger, service);
            }
        }
    }

    public Passenger SpawnExitingPassenger(Vector3 spawnPos, Quaternion spawnRot)
    {
        Passenger newPassenger = Instantiate(passengerPrefab, spawnPos, spawnRot).GetComponent<Passenger>();
        
        if (newPassenger.navAgent != null)
        {
            newPassenger.navAgent.enabled = false;
        }

        newPassenger.transform.parent = transform;

        SpawnPassengerModels(newPassenger);
        RegisterPassenger(newPassenger);

        newPassenger.hasTicket = true; 
        
        return newPassenger;
    }

    public void FinaliseExitingPassenger(Passenger passenger)
    {
        if (passenger.navAgent != null)
        {
            passenger.navAgent.enabled = true;
            
            if (NavMesh.SamplePosition(passenger.transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                passenger.navAgent.Warp(hit.position); 
            }
        }
        
        LeaveStation(passenger);
    }

    public void BoardTrain(Passenger passenger)
    {
        if (passenger.currentTarget != null)
        {
            passenger.currentTarget.RemovePerson(passenger);
        }
        UnregisterPassenger(passenger);
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

        newPassenger.navAgent.avoidancePriority = Random.Range(50, 100);
        
        DecideNextAction(newPassenger);
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