using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    public List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    
    [Range(0f, 1f)] public float spontaneousDematerializationRate = 0.0004f;
    
    private List<Vector3> passengerSpawnPoints = new List<Vector3>();
    
    public List<MaterializerController> materializers = new List<MaterializerController>();
    
    public List<GameObject> litterPrefabs = new List<GameObject>();

    private float tickLength = 0.1f; 
    private float tickTimer;
    
    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        ExpansionManager.OnExpansionBuilt += UpdatePassengerEntrances;
    }

    void OnDisable()
    {
        ExpansionManager.OnExpansionBuilt -= UpdatePassengerEntrances;
    }
    
    void Start()
    {
        UpdatePassengerEntrances();
        InvokeRepeating("DropLitter", 1, 1);
    }
    
    public void RegisterMaterializer(MaterializerController mat)
    {
        if (!materializers.Contains(mat))
        {
            materializers.Add(mat);
        }
    }

    public void DeregisterMaterializer(MaterializerController mat)
    {
        if (materializers.Contains(mat))
        {
            materializers.Remove(mat);
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
            if (TrainManager.Instance.activeTrainServices.Count > 0)
            {
                SpawnPassengerForService(TrainManager.Instance.AssignTrainServiceToPassenger());
            }
        }
    }
    
    void LogicUpdate()
    {
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            
            if (passenger.navAgent == null || !passenger.navAgent.isActiveAndEnabled) continue;

            switch (passenger.currentSubState)
            {
                case Passenger.passengerSubStates.Idle:
                    DecideNextAction(passenger);
                    
                    if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform)
                    {
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
                                    passenger.navAgent.enabled = false;
                                    passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
                                    
                                    MaterializeAnimator animator = passenger.GetComponent<MaterializeAnimator>();
                                    if (animator != null)
                                    {
                                        animator.Dematerialize(() => 
                                        {
                                            UnregisterPassenger(passenger);
                                        });
                                    }
                                    else
                                    {
                                        UnregisterPassenger(passenger);
                                    }
                                    break;
                            }
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
            if (passenger == null) yield break;

            passenger.transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        passenger.transform.position = endPos;
        passenger.navAgent.CompleteOffMeshLink();
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
    }

    public void UpdatePassengerEntrances()
    {
        passengerSpawnPoints.Clear();
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("PassengerSpawnPoint");
        
        foreach (GameObject obj in spawnObjects)
        {
            passengerSpawnPoints.Add(obj.transform.position);
        }
        
        if (passengerSpawnPoints.Count == 0)
        {
            passengerSpawnPoints.Add(Vector3.zero);
        }
    }
    
    public Vector3 GetRandomSpawnPoint()
    {
        if (materializers.Count > 0)
        {
            MaterializerController mat = materializers[Random.Range(0, materializers.Count)];
            return mat.GetRandomPointOnPad();
        }

        if (passengerSpawnPoints.Count == 0) return Vector3.zero;
        return passengerSpawnPoints[Random.Range(0, passengerSpawnPoints.Count)];
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

        if (passenger.assignedTrainService == null || !TrainManager.Instance.activeTrainServices.Contains(passenger.assignedTrainService))
        {
            passenger.assignedTrainService = null;
            LeaveStation(passenger);
            return;
        }

        if (passenger.currentMasterState == Passenger.passengerMasterStates.InStation)
        {
            if (!passenger.hasTicket && !passenger.isTicketEvader)
            {
                GoToFacility(FacilityType.TicketMachine, passenger);
                return;
            }
            
            if (!passenger.hasTicket && passenger.isTicketEvader)
            {
                MoveToPlatformPosition(passenger);
                return;
            }

            while (true)
            {
                Passenger.NeedType nextNeed = passenger.GetNextNeed();
                
                if (nextNeed == Passenger.NeedType.None)
                {
                    break;
                }

                List<FacilityType> validFacilities = FacilityManager.Instance.GetFacilitiesForNeed(nextNeed);
                
                if (validFacilities.Count > 0 && TrySatisfyNeedWithRandomSwitching(validFacilities, passenger))
                {
                    return; 
                }
                
                passenger.hasFailedNeed = true;
                passenger.ClearNeed(nextNeed);
            }

            MoveToPlatformPosition(passenger);
        }
        else if (passenger.currentMasterState == Passenger.passengerMasterStates.OnPlatform)
        {
            if (passenger.assignedTrainService.physicalTrainInstance != null && 
                passenger.assignedTrainService.physicalTrainInstance.IsAtStation())
            {
                SendPassengerToTrainDoor(passenger, passenger.assignedTrainService);
            }
        }
    }
    
    private bool TrySatisfyNeedWithRandomSwitching(List<FacilityType> validTypes, Passenger passenger)
    {
        List<FacilityType> typesToTry = new List<FacilityType>(validTypes);
        
        while (typesToTry.Count > 0)
        {
            int randomIndex = Random.Range(0, typesToTry.Count);
            FacilityType chosenType = typesToTry[randomIndex];
            
            typesToTry.RemoveAt(randomIndex);
            
            if (FacilityManager.Instance.HasFacility(chosenType))
            {
                return GoToFacility(chosenType, passenger);
            }
            
            if (typesToTry.Count > 0 && Random.value > 0.5f) 
            {
                return false;
            }
        }
        
        return false;
    }

    public void DropLitter()
    {
        if (litterPrefabs.Count == 0 || activePassengers.Count == 0) return;
        
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            if (Random.value <= spontaneousDematerializationRate)
            {
                Passenger randomPassenger = activePassengers[i];
                
                Vector3 rayStart = randomPassenger.transform.position + (Vector3.up * 0.5f);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 2f))
                {
                    if (hit.collider.CompareTag("UnlitterableSurface"))
                    {
                        continue; 
                    }
                }

                activePassengers.RemoveAt(i);
                UnassignTarget(randomPassenger);
                if (randomPassenger.navAgent != null) randomPassenger.navAgent.enabled = false;
                
                StartCoroutine(ReplacePassengerWithLitter(randomPassenger));
            }
        }
    }

    private System.Collections.IEnumerator ReplacePassengerWithLitter(Passenger passenger)
    {
        float duration = 1.0f;
        float elapsed = 0f;

        Vector3 puddlePos = passenger.transform.position;
        Color puddleColor = Color.white;
        
        if (passenger.transform.childCount > 0)
        {
            MeshRenderer renderer = passenger.transform.GetChild(0).GetComponent<MeshRenderer>();
            if (renderer != null) puddleColor = renderer.material.color;
        }

        GameObject litterPrefab = litterPrefabs[Random.Range(0, litterPrefabs.Count)];
        Vector3 targetLitterScale = litterPrefab.transform.localScale;

        GameObject litterObj = Instantiate(litterPrefab, puddlePos, Quaternion.Euler(0, Random.Range(0, 360), 0));
        litterObj.transform.localScale = Vector3.zero;

        foreach (Transform child in litterObj.transform)
        {
            MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = puddleColor;
            }
        }

        MaterializeAnimator animator = passenger.GetComponent<MaterializeAnimator>();
        if (animator != null)
        {
            animator.Dematerialize(null);
        }

        while (elapsed < duration)
        {
            if (litterObj == null) yield break;

            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);

            litterObj.transform.localScale = Vector3.Lerp(Vector3.zero, targetLitterScale, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (litterObj != null)
        {
            litterObj.transform.localScale = targetLitterScale;
            Litter litterComp = litterObj.GetComponent<Litter>();
            if (litterComp != null && JanitorCoordinator.Instance != null)
            {
                JanitorCoordinator.Instance.ReportLitter(litterComp);
            }
        }

        if (passenger != null)
        {
            Destroy(passenger.gameObject);
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
        
        Vector3 exitPosition = GetRandomSpawnPoint();
        
        passenger.navAgent.SetDestination(exitPosition);
        passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
        passenger.navAgent.stoppingDistance = 0.5f;

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
    
    private bool GoToFacility(FacilityType type, Passenger passenger)
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

            return true;
        }

        return false;
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
            
            ApplyPassengerVisuals(passenger);
            
            MaterializeAnimator animator = passenger.GetComponent<MaterializeAnimator>();
            if (animator != null) animator.Pop();
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

            passenger.ClearNeed(needType);
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
            
            MaterializeAnimator animator = passenger.GetComponent<MaterializeAnimator>();
            if (animator != null) animator.Pop();
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

    public Passenger SpawnExitingPassenger(Vector3 spawnPos, Quaternion spawnRot, TrainService service)
    {
        Vector3 finalSpawnPos = spawnPos;
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }

        Passenger newPassenger = Instantiate(passengerPrefab, finalSpawnPos, spawnRot).GetComponent<Passenger>();
        
        if (newPassenger.navAgent != null)
        {
            newPassenger.navAgent.enabled = false;
        }

        newPassenger.transform.parent = transform;

        newPassenger.assignedTrainService = service;
        ApplyPassengerVisuals(newPassenger);
        
        newPassenger.assignedTrainService = null; 

        RegisterPassenger(newPassenger);

        newPassenger.hasTicket = true; 
        
        MaterializeAnimator animator = newPassenger.GetComponent<MaterializeAnimator>();
        if (animator != null) animator.Pop();
        
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
                passenger.assignedTrainService = null;
            }
        }
    }

    public void OnCaughtByDrone(Passenger passenger)
    {
        UnassignTarget(passenger);
        
        passenger.hasBypassedBarrier = false; 
        passenger.currentSubState = Passenger.passengerSubStates.InteractingWithSomething;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(50);
        }

        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$50", 
                passenger.transform.position + Vector3.up * 3f, 
                Color.darkGreen);
        }

        passenger.navAgent.enabled = false;
        
        MaterializeAnimator animator = passenger.GetComponent<MaterializeAnimator>();
        if (animator != null)
        {
            animator.Dematerialize(() => 
            {
                UnregisterPassenger(passenger);
            });
        }
        else
        {
            UnregisterPassenger(passenger);
        }
    }

    public void SpawnPassengerForService(TrainService service)
    {
        Vector3 finalSpawnPoint = GetRandomSpawnPoint();

        Passenger newPassenger = Instantiate(passengerPrefab, finalSpawnPoint, Quaternion.identity).GetComponent<Passenger>();
        newPassenger.transform.parent = transform;

        newPassenger.assignedTrainService = service;
        newPassenger.isTicketEvader = Random.Range(1, 100) <= 5;
        
        RegisterPassenger(newPassenger);
        
        newPassenger.TimeToGoToPlatform = Time.time + Random.Range(10f, 60f);
        newPassenger.navAgent.avoidancePriority = Random.Range(50, 100);
        
        if (newPassenger.navAgent != null)
        {
            newPassenger.navAgent.enabled = false;
        }

        MaterializeAnimator animator = newPassenger.GetComponent<MaterializeAnimator>();
        if (animator != null)
        {
            animator.Materialize(() => 
            {
                if (newPassenger != null && newPassenger.gameObject != null)
                {
                    if (newPassenger.navAgent != null)
                    {
                        newPassenger.navAgent.enabled = true;
                    }
                    DecideNextAction(newPassenger);
                }
            });
        }
        else
        {
            if (newPassenger.navAgent != null) newPassenger.navAgent.enabled = true;
            DecideNextAction(newPassenger);
        }
    }

    public void ApplyPassengerVisuals(Passenger passenger)
    {
        Color passengerColor = Color.gray;
        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            passengerColor = passenger.assignedTrainService.trainData.trainColor;
        }

        int childCount = Mathf.Min(2, passenger.transform.childCount);
        for (int i = 0; i < childCount; i++)
        {
            MeshRenderer renderer = passenger.transform.GetChild(i).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = passengerColor;
            }
        }
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