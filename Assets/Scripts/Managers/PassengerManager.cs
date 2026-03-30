using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;
    
    public List<Passenger> activePassengers = new List<Passenger>();
    [SerializeField] private GameObject passengerPrefab;
    
    [Range(0f, 1f)] public float spontaneousDematerializationRate = 0.0004f;
    
    private List<Vector3> passengerSpawnPoints = new List<Vector3>();
    
    public List<MaterializerController> materializers = new List<MaterializerController>();
    
    public List<GameObject> litterPrefabs = new List<GameObject>();

    [Header("Need Unlock Tiers")]
    [Min(1)] public int hungerNeedStartTier = 1;
    [Min(1)] public int thirstNeedStartTier = 1;
    [Min(1)] public int energyNeedStartTier = 999;
    [Min(1)] public int hygieneNeedStartTier = 1;
    [Range(0f, 1f)] public float disembarkingFacilityUsageChance = 0.5f;

    [Header("Need Warning UI")]
    [SerializeField] private GameObject needIconPrefab;
    [SerializeField] private Sprite failedNeedOverlaySprite;
    [SerializeField] private Sprite ticketNeedSprite;
    [SerializeField] private Sprite thirstNeedSprite;
    [SerializeField] private Sprite hungerNeedSprite;
    [SerializeField] private Sprite hygieneNeedSprite;
    [SerializeField] private Color ticketNeedColor = new Color(1f, 0.88f, 0.26f);
    [SerializeField] private Color thirstNeedColor = new Color(0.22f, 0.67f, 1f);
    [SerializeField] private Color hungerNeedColor = new Color(0.88f, 0.53f, 0.2f);
    [SerializeField] private Color hygieneNeedColor = new Color(0.54f, 0.87f, 0.64f);
    [Min(0f)] public float blockedNeedPassiveDuration = 10f;
    [Min(0f)] public float blockedNeedUrgentDuration = 10f;
    public Vector3 needIconWorldOffset = new Vector3(0f, 5.5f, 0f);

    [Header("Blocked Need Movement")]
    [Min(0f)] public float blockedNeedWanderRadius = 4f;
    [Min(0f)] public float blockedNeedWanderIntervalMin = 1.5f;
    [Min(0f)] public float blockedNeedWanderIntervalMax = 3.5f;

    [Header("Service Queue Tolerance")]
    [Min(0f)] public float minServiceWaitTime = 10f;
    [Min(0f)] public float maxServiceWaitTime = 24f;
    [Min(1)] public int maxFacilityQueueLength = 8;

    private const float BlockedNeedCheckInterval = 1f;
    private const float FallbackPassengerWalkSpeed = 3.5f;
    private Sprite runtimeTicketNeedSprite;
    private float tickLength = 0.1f; 
    private float tickTimer;
    private NavMeshPath facilityRoutePath;
    
    void Awake()
    {
        Instance = this;
        facilityRoutePath = new NavMeshPath();
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

    public bool HasMaterializer()
    {
        return materializers.Count > 0;
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

                    if (!IsActivePassenger(passenger))
                    {
                        continue;
                    }
                    
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
                                case Passenger.passengerSpecialTargets.BlockedNeedWander:
                                    passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
                                    passenger.currentSubState = Passenger.passengerSubStates.Idle;
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

                        if (!IsActivePassenger(passenger))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        UpdateQueuePosition(passenger);
                        
                        if (passenger.currentTarget != null)
                        {
                            Vector3 myQueueSpot = passenger.currentTarget.GetQueuePositionFor(passenger);
                            if (Vector3.Distance(passenger.navAgent.destination, myQueueSpot) > 0.5f)
                            {
                                passenger.navAgent.SetDestination(myQueueSpot);
                            }
                        }

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
                    if (passenger.currentTarget == null)
                    {
                        passenger.currentSubState = Passenger.passengerSubStates.Idle;
                        passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
                        break;
                    }

                    UpdateQueuePosition(passenger);
                    
                    if (passenger.currentTarget != null)
                    {
                        passenger.FaceTarget(passenger.currentTarget.transform.position);
                    }
                    break;
            }

            if (!IsActivePassenger(passenger))
            {
                continue;
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
            if (passenger.shouldUseFacilitiesBeforeExit)
            {
                HandleDisembarkingPassengerNeeds(passenger);
                return;
            }

            ClearBlockedNeed(passenger);
            passenger.assignedTrainService = null;
            LeaveStation(passenger);
            return;
        }

        if (passenger.currentMasterState == Passenger.passengerMasterStates.InStation)
        {
            if (!passenger.hasTicket && !passenger.isTicketEvader)
            {
                Passenger.NeedType ticketNeed = Passenger.NeedType.Ticket;

                if (passenger.blockedNeed == ticketNeed &&
                    passenger.blockedNeedFailureStage > 0 &&
                    Time.time < passenger.nextBlockedNeedCheckTime)
                {
                    TryWanderWhileBlocked(passenger);
                    return;
                }

                if (TrySendPassengerToNeedFacility(passenger, ticketNeed))
                {
                    ClearBlockedNeed(passenger, true);
                    return;
                }

                HandleBlockedNeed(passenger, ticketNeed);
                return;
            }
            
            if (!passenger.hasTicket && passenger.isTicketEvader)
            {
                MoveToPlatformPosition(passenger);
                return;
            }

            if (passenger.hasGivenUpNeed)
            {
                MoveToPlatformPosition(passenger, true);
                return;
            }

            while (true)
            {
                Passenger.NeedType nextNeed = passenger.GetNextNeed();
                
                if (nextNeed == Passenger.NeedType.None)
                {
                    ClearBlockedNeed(passenger);
                    break;
                }

                if (passenger.blockedNeed == nextNeed &&
                    passenger.blockedNeedFailureStage > 0 &&
                    Time.time < passenger.nextBlockedNeedCheckTime)
                {
                    TryWanderWhileBlocked(passenger);
                    return;
                }

                if (TrySendPassengerToNeedFacility(passenger, nextNeed))
                {
                    ClearBlockedNeed(passenger, true);
                    return; 
                }

                HandleBlockedNeed(passenger, nextNeed);
                return;
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

    private void HandleDisembarkingPassengerNeeds(Passenger passenger)
    {
        if (passenger == null)
        {
            return;
        }

        if (passenger.hasFailedNeed || passenger.hasGivenUpNeed)
        {
            passenger.shouldUseFacilitiesBeforeExit = false;
            LeaveStation(passenger, passenger.hasFailedNeed);
            return;
        }

        while (true)
        {
            Passenger.NeedType nextNeed = passenger.GetNextNeed();

            if (nextNeed == Passenger.NeedType.None)
            {
                passenger.shouldUseFacilitiesBeforeExit = false;
                ClearBlockedNeed(passenger);
                LeaveStation(passenger);
                return;
            }

            if (passenger.blockedNeed == nextNeed &&
                passenger.blockedNeedFailureStage > 0 &&
                Time.time < passenger.nextBlockedNeedCheckTime)
            {
                TryWanderWhileBlocked(passenger);
                return;
            }

            if (TrySendPassengerToNeedFacility(passenger, nextNeed))
            {
                ClearBlockedNeed(passenger, true);
                return;
            }

            HandleBlockedNeed(passenger, nextNeed);
            return;
        }
    }

    public void DropLitter()
    {
        if (litterPrefabs.Count == 0 || activePassengers.Count == 0) return;
        
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            if (Random.value <= spontaneousDematerializationRate)
            {
                Passenger randomPassenger = activePassengers[i];
                if (randomPassenger.currentSubState == Passenger.passengerSubStates.InteractingWithSomething)
                {
                    continue;
                }
                
                Vector3 rayStart = randomPassenger.transform.position + (Vector3.up * 0.5f);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 6f))
                {
                    if (hit.collider.CompareTag("UnlitterableSurface"))
                    {
                        continue; 
                    }
                }

                RemovePassengerAsLitter(randomPassenger);
            }
        }
    }

    private System.Collections.IEnumerator ReplacePassengerWithLitter(Passenger passenger)
    {
        float duration = 1.0f;
        float elapsed = 0f;

        Vector3 puddlePos = passenger.transform.position;
        Color puddleColor = Color.white;
        
        if (passenger.visorRenderer != null)
        {
            puddleColor = passenger.visorRenderer.material.color;
        }
        else if (passenger.transform.childCount > 1)
        {
            MeshRenderer renderer = passenger.transform.GetChild(1).GetComponent<MeshRenderer>();
            if (renderer != null) puddleColor = renderer.material.color;
        }

        GameObject litterPrefab = litterPrefabs[Random.Range(0, litterPrefabs.Count)];
        Vector3 targetLitterScale = litterPrefab.transform.localScale;

        GameObject litterObj = Instantiate(litterPrefab, puddlePos, Quaternion.Euler(0, Random.Range(0, 360), 0));
        litterObj.transform.localScale = Vector3.zero;

        foreach (Transform child in litterObj.transform)
        {
            MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
            if (childRenderer != null && childRenderer.tag != "BaseLitterColour")
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
    
    private void LeaveStation(Passenger passenger, bool preserveNeedFailureState = false)
    {
        passenger.shouldUseFacilitiesBeforeExit = false;

        if (!preserveNeedFailureState)
        {
            ClearBlockedNeed(passenger);
        }

        if (!CanUseNavAgent(passenger))
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
        
        if (CanUseNavAgent(passenger))
        {
            passenger.navAgent.ResetPath();
        }
    }

    public void HandleDestroyedQueueTarget(QueuableObject destroyedTarget)
    {
        if (destroyedTarget == null)
        {
            return;
        }

        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            Passenger passenger = activePassengers[i];
            if (passenger == null || passenger.currentTarget != destroyedTarget)
            {
                continue;
            }

            UnassignTarget(passenger);
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
        }
    }

    private bool IsActivePassenger(Passenger passenger)
    {
        return passenger != null && activePassengers.Contains(passenger);
    }

    private int GetCurrentNeedTier()
    {
        return ProgressionManager.Instance != null ? Mathf.Max(1, ProgressionManager.Instance.CurrentLevel) : 1;
    }

    private int GetNeedStartTier(Passenger.NeedType needType)
    {
        switch (needType)
        {
            case Passenger.NeedType.Ticket:
                return 1;
            case Passenger.NeedType.Hunger:
                return Mathf.Max(1, hungerNeedStartTier);
            case Passenger.NeedType.Thirst:
                return Mathf.Max(1, thirstNeedStartTier);
            case Passenger.NeedType.Energy:
                return Mathf.Max(1, energyNeedStartTier);
            case Passenger.NeedType.Hygiene:
                return Mathf.Max(1, hygieneNeedStartTier);
            default:
                return int.MaxValue;
        }
    }

    private bool IsNeedUnlocked(Passenger.NeedType needType)
    {
        return GetCurrentNeedTier() >= GetNeedStartTier(needType);
    }

    private bool TrySendPassengerToNeedFacility(Passenger passenger, Passenger.NeedType needType)
    {
        if (FacilityManager.Instance == null || passenger == null)
        {
            return false;
        }

        List<FacilityType> validFacilities = FacilityManager.Instance.GetFacilitiesForNeed(needType);
        StationFacility bestFacility = null;
        float bestWait = Mathf.Infinity;

        for (int i = 0; i < validFacilities.Count; i++)
        {
            FacilityType type = validFacilities[i];
            List<StationFacility> facilities = FacilityManager.Instance.GetFacilities(type);
            if (facilities == null || facilities.Count == 0)
            {
                continue;
            }

            StationFacility candidateFacility = GetMostAccessibleFacility(facilities, passenger, passenger.maxServiceWaitTime, out float candidateDelay);
            if (candidateFacility != null && candidateDelay < bestWait)
            {
                bestWait = candidateDelay;
                bestFacility = candidateFacility;
            }
        }

        return bestFacility != null && GoToFacility(bestFacility, passenger);
    }

    private void HandleBlockedNeed(Passenger passenger, Passenger.NeedType needType)
    {
        if (passenger == null || needType == Passenger.NeedType.None)
        {
            return;
        }

        if (passenger.blockedNeed != needType || passenger.blockedNeedFailureStage == 0)
        {
            passenger.blockedNeed = needType;
            passenger.blockedNeedStartTime = Time.time;
            passenger.blockedNeedFailureStage = 1;
            passenger.nextBlockedNeedCheckTime = Time.time + BlockedNeedCheckInterval;
            passenger.blockedNeedWanderCenter = passenger.transform.position;
            passenger.nextBlockedNeedWanderTime = Time.time;
            ShowBlockedNeedIcon(passenger, needType, false);
            return;
        }

        if (Time.time < passenger.nextBlockedNeedCheckTime)
        {
            return;
        }

        passenger.nextBlockedNeedCheckTime = Time.time + BlockedNeedCheckInterval;

        float blockedDuration = Time.time - passenger.blockedNeedStartTime;
        float passiveDuration = Mathf.Max(0f, blockedNeedPassiveDuration);
        float urgentDuration = Mathf.Max(0f, blockedNeedUrgentDuration);

        if (passenger.blockedNeedFailureStage == 1 && blockedDuration >= passiveDuration)
        {
            passenger.blockedNeedFailureStage = 2;
            ShowBlockedNeedIcon(passenger, needType, true);
        }

        if (blockedDuration >= passiveDuration + urgentDuration)
        {
            GiveUpOnNeed(passenger, needType);
            return;
        }
    }

    private void ShowBlockedNeedIcon(Passenger passenger, Passenger.NeedType needType, bool shouldBlink)
    {
        Sprite needSprite = GetNeedSprite(needType);
        if (needSprite == null)
        {
            RemoveNeedIcon(passenger);
            return;
        }

        PassengerNeedIconController iconController = passenger.blockedNeedIcon;
        if (iconController == null)
        {
            Transform iconParent = GetNeedIconParent(passenger);
            if (iconParent == null)
            {
                return;
            }

            GameObject iconObject;
            if (needIconPrefab != null)
            {
                iconObject = Instantiate(needIconPrefab, iconParent);
            }
            else
            {
                iconObject = new GameObject("NeedIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(iconParent, false);
            }

            iconController = iconObject.GetComponent<PassengerNeedIconController>();
            if (iconController == null)
            {
                iconController = iconObject.AddComponent<PassengerNeedIconController>();
            }

            passenger.blockedNeedIcon = iconController;
            iconController.Initialize(passenger, needSprite, GetNeedColor(needType), needIconWorldOffset);
        }
        else
        {
            iconController.SetSprite(needSprite);
        }

        iconController.SetNormalColor(GetNeedColor(needType));
        iconController.SetFailedOverlaySprite(failedNeedOverlaySprite);
        iconController.SetOverlayState(PassengerNeedIconController.OverlayState.None);
        iconController.SetIconOpacity(1f);
        iconController.SetAlertState(shouldBlink, shouldBlink);
    }

    private void RemoveNeedIcon(Passenger passenger, bool fadeIcon = false)
    {
        if (passenger == null || passenger.blockedNeedIcon == null)
        {
            return;
        }

        if (fadeIcon)
        {
            passenger.blockedNeedIcon.FadeOutAndDestroy();
        }
        else
        {
            Destroy(passenger.blockedNeedIcon.gameObject);
        }

        passenger.blockedNeedIcon = null;
    }

    private void GiveUpOnNeed(Passenger passenger, Passenger.NeedType needType)
    {
        if (passenger == null)
        {
            return;
        }

        passenger.ClearNeed(needType);
        passenger.hasFailedNeed = true;
        passenger.hasGivenUpNeed = needType != Passenger.NeedType.Ticket;
        passenger.blockedNeed = Passenger.NeedType.None;
        passenger.blockedNeedStartTime = 0f;
        passenger.nextBlockedNeedCheckTime = 0f;
        passenger.blockedNeedFailureStage = 0;
        passenger.blockedNeedWanderCenter = Vector3.zero;
        passenger.nextBlockedNeedWanderTime = 0f;

        if (passenger.blockedNeedIcon != null)
        {
            UpdateGivenUpNeedIcon(passenger, needType);
        }

        if (needType == Passenger.NeedType.Ticket || passenger.shouldUseFacilitiesBeforeExit || passenger.assignedTrainService == null)
        {
            passenger.shouldUseFacilitiesBeforeExit = false;
            LeaveStation(passenger, true);
            return;
        }

        MoveToPlatformPosition(passenger, true);
    }

    private void UpdateGivenUpNeedIcon(Passenger passenger, Passenger.NeedType needType)
    {
        ShowBlockedNeedIcon(passenger, needType, false);
        if (passenger.blockedNeedIcon == null)
        {
            return;
        }

        passenger.blockedNeedIcon.SetSprite(GetNeedSprite(needType));
        passenger.blockedNeedIcon.SetNormalColor(GetNeedColor(needType));
        passenger.blockedNeedIcon.SetAlertState(false, false);
        passenger.blockedNeedIcon.SetOverlayState(PassengerNeedIconController.OverlayState.Failed);
        passenger.blockedNeedIcon.SetIconOpacity(0.4f);
    }

    private void TryWanderWhileBlocked(Passenger passenger)
    {
        if (!CanUseNavAgent(passenger))
        {
            return;
        }

        if (passenger.currentSpecialTarget == Passenger.passengerSpecialTargets.BlockedNeedWander)
        {
            return;
        }

        if (Time.time < passenger.nextBlockedNeedWanderTime)
        {
            return;
        }

        float wanderRadius = Mathf.Max(0.5f, blockedNeedWanderRadius);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * wanderRadius;
            Vector3 targetPosition = passenger.blockedNeedWanderCenter + new Vector3(offset2D.x, 0f, offset2D.y);

            if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                continue;
            }

            passenger.navAgent.SetDestination(hit.position);
            passenger.navAgent.stoppingDistance = 0.2f;
            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.BlockedNeedWander;
            passenger.nextBlockedNeedWanderTime = Time.time + Random.Range(
                blockedNeedWanderIntervalMin,
                Mathf.Max(blockedNeedWanderIntervalMin, blockedNeedWanderIntervalMax));
            return;
        }

        passenger.nextBlockedNeedWanderTime = Time.time + 1f;
    }

    private Transform GetNeedIconParent(Passenger passenger)
    {
        if (WorldSpacePromptCoordinator.Instance != null && WorldSpacePromptCoordinator.Instance.worldSpaceCanvas != null)
        {
            return WorldSpacePromptCoordinator.Instance.worldSpaceCanvas.transform;
        }

        if (passenger != null)
        {
            passenger.CreateNewPersonalCanvas(passenger);
            if (passenger.personalCanvas != null)
            {
                return passenger.personalCanvas.transform;
            }
        }

        return null;
    }

    private Sprite GetNeedSprite(Passenger.NeedType needType)
    {
        switch (needType)
        {
            case Passenger.NeedType.Ticket:
                return GetTicketNeedSprite();
            case Passenger.NeedType.Thirst:
                return thirstNeedSprite;
            case Passenger.NeedType.Hunger:
                return hungerNeedSprite;
            case Passenger.NeedType.Hygiene:
                return hygieneNeedSprite;
            default:
                return null;
        }
    }

    private Color GetNeedColor(Passenger.NeedType needType)
    {
        switch (needType)
        {
            case Passenger.NeedType.Ticket:
                return ticketNeedColor;
            case Passenger.NeedType.Thirst:
                return thirstNeedColor;
            case Passenger.NeedType.Hunger:
                return hungerNeedColor;
            case Passenger.NeedType.Hygiene:
                return hygieneNeedColor;
            default:
                return Color.white;
        }
    }

    private void ClearBlockedNeed(Passenger passenger, bool fadeIcon = false)
    {
        if (passenger == null)
        {
            return;
        }

        passenger.blockedNeed = Passenger.NeedType.None;
        passenger.blockedNeedStartTime = 0f;
        passenger.nextBlockedNeedCheckTime = 0f;
        passenger.blockedNeedFailureStage = 0;
        passenger.blockedNeedWanderCenter = Vector3.zero;
        passenger.nextBlockedNeedWanderTime = 0f;
        passenger.hasFailedNeed = false;
        passenger.hasGivenUpNeed = false;
        RemoveNeedIcon(passenger, fadeIcon);
    }

    private void RemovePassengerAsLitter(Passenger passenger)
    {
        if (!IsActivePassenger(passenger))
        {
            return;
        }

        ClearBlockedNeed(passenger);
        activePassengers.Remove(passenger);
        UnassignTarget(passenger);

        if (passenger.navAgent != null && passenger.navAgent.isActiveAndEnabled)
        {
            passenger.navAgent.enabled = false;
        }

        if (litterPrefabs.Count == 0)
        {
            Destroy(passenger.gameObject);
            return;
        }

        StartCoroutine(ReplacePassengerWithLitter(passenger));
    }

    private bool CanUseNavAgent(Passenger passenger)
    {
        return passenger != null &&
               passenger.navAgent != null &&
               passenger.navAgent.isActiveAndEnabled &&
               passenger.navAgent.isOnNavMesh;
    }
    
    private bool GoToFacility(StationFacility bestMachine, Passenger passenger)
    {
        if (bestMachine != null)
        {
            passenger.currentTarget = bestMachine;
            passenger.currentSubState = Passenger.passengerSubStates.MovingToTarget;
            passenger.currentTarget.AssignPerson(passenger);
                            
            Vector3 targetPosition = bestMachine.GetQueuePositionFor(passenger);
                            
            if(NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 4, NavMesh.AllAreas))
            {
                passenger.navAgent.SetDestination(hit.position);
            }
            else
            {
                passenger.navAgent.SetDestination(targetPosition);
            }

            return true;
        }

        return false;
    }
    
    private void UpdateQueuePosition(Passenger passenger)
    {
        if (passenger.currentTarget == null) return;
        if (!CanUseNavAgent(passenger)) return;

        passenger.navAgent.stoppingDistance = GetTargetStoppingDistance(passenger.currentTarget, passenger);
    }
    
    private void MoveToPlatformPosition(Passenger passenger, bool preserveNeedFailureState = false)
    {
        PlatformController targetPlatform = passenger.assignedTrainService.assignedPlatform;

        if (targetPlatform == null)
        {
            LeaveStation(passenger, preserveNeedFailureState);
            return;
        }
        
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
            LeaveStation(passenger, preserveNeedFailureState);
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
            ClearBlockedNeed(passenger, true);
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
            if (passenger.assignedTrainService != null)
            {
                EconomyManager.Instance.AddMoney(passenger.assignedTrainService.trainData.costPerRide);
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.RecordTicketSold();
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
            passenger.hasGivenUpNeed = false;
            ClearBlockedNeed(passenger, true);
            passenger.currentSubState = Passenger.passengerSubStates.Idle;

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.RecordNeedFulfilled();
            }
            
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
        if (!CanUseNavAgent(passenger)) return;

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

            passenger.navAgent.stoppingDistance = GetTargetStoppingDistance(closestDoor, passenger);
            if (NavMesh.SamplePosition(closestDoor.transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Vector3 targetPosition = closestDoor.GetQueuePositionFor(passenger);
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit queueHit, 2.5f, NavMesh.AllAreas))
                {
                    passenger.navAgent.SetDestination(queueHit.position);
                }
                else
                {
                    passenger.navAgent.SetDestination(hit.position);
                }
            }
            else
            {
                passenger.navAgent.SetDestination(closestDoor.GetQueuePositionFor(passenger));
            }

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
        AssignServiceWaitTime(newPassenger);

        newPassenger.hasTicket = true; 
        newPassenger.shouldUseFacilitiesBeforeExit = Random.value < disembarkingFacilityUsageChance;
        if (newPassenger.shouldUseFacilitiesBeforeExit)
        {
            newPassenger.RollNeeds(
                IsNeedUnlocked(Passenger.NeedType.Hunger),
                IsNeedUnlocked(Passenger.NeedType.Thirst),
                IsNeedUnlocked(Passenger.NeedType.Energy),
                IsNeedUnlocked(Passenger.NeedType.Hygiene),
                true);

            if (!newPassenger.HasAnyNeed())
            {
                newPassenger.shouldUseFacilitiesBeforeExit = false;
            }
        }
        
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

        if (passenger.shouldUseFacilitiesBeforeExit && passenger.HasAnyNeed())
        {
            passenger.currentSubState = Passenger.passengerSubStates.Idle;
            passenger.currentSpecialTarget = Passenger.passengerSpecialTargets.None;
            DecideNextAction(passenger);
            return;
        }

        passenger.shouldUseFacilitiesBeforeExit = false;
        LeaveStation(passenger);
    }

    public void BoardTrain(Passenger passenger)
    {
        if (passenger.currentTarget != null)
        {
            passenger.currentTarget.RemovePerson(passenger);
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.RecordPassengerBoarded();
        }

        UnregisterPassenger(passenger);
    }

    private Sprite GetTicketNeedSprite()
    {
        if (ticketNeedSprite != null)
        {
            return ticketNeedSprite;
        }

        if (runtimeTicketNeedSprite == null)
        {
            ObjectBuildable ticketMachineBuildable = Resources.Load<ObjectBuildable>("BuildItems/TicketMachine");
            if (ticketMachineBuildable != null)
            {
                runtimeTicketNeedSprite = ticketMachineBuildable.icon;
            }
        }

        return runtimeTicketNeedSprite;
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
        if (service == null || !HasMaterializer())
        {
            return;
        }

        Vector3 spawnPoint = GetRandomSpawnPoint();
        Vector3 finalSpawnPoint = spawnPoint;
        
        if (NavMesh.SamplePosition(spawnPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            finalSpawnPoint = hit.position;
        }

        Passenger newPassenger = Instantiate(passengerPrefab, finalSpawnPoint, Quaternion.identity).GetComponent<Passenger>();
        newPassenger.transform.parent = transform;

        newPassenger.assignedTrainService = service;
        newPassenger.RollNeeds(
            IsNeedUnlocked(Passenger.NeedType.Hunger),
            IsNeedUnlocked(Passenger.NeedType.Thirst),
            IsNeedUnlocked(Passenger.NeedType.Energy),
            IsNeedUnlocked(Passenger.NeedType.Hygiene));
        newPassenger.isTicketEvader = Random.Range(1, 100) <= 5;
        
        RegisterPassenger(newPassenger);
        AssignServiceWaitTime(newPassenger);
        
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

        if (passenger.visorRenderer != null)
        {
            passenger.visorRenderer.material.color = passengerColor;
        }
        else if (passenger.transform.childCount > 1)
        {
            MeshRenderer renderer = passenger.transform.GetChild(1).GetComponent<MeshRenderer>();
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

    private StationFacility GetMostAccessibleFacility(List<StationFacility> facilities, Passenger passenger, float maxWaitTime, out float bestWait)
    {
        bestWait = Mathf.Infinity;
        if (facilities == null || facilities.Count == 0 || passenger == null)
        {
            return null;
        }

        float clampedMaxWait = Mathf.Max(0f, maxWaitTime);
        StationFacility bestFacility = null;

        for (int i = 0; i < facilities.Count; i++)
        {
            StationFacility facility = facilities[i];
            if (facility == null)
            {
                continue;
            }

            if (facility.PeopleOnWay.Count >= Mathf.Max(1, maxFacilityQueueLength))
            {
                continue;
            }

            float estimatedWait = facility.GetEstimatedQueueWaitTime();
            float estimatedWalkTime = EstimateWalkTimeToService(passenger, facility);
            float totalEstimatedDelay = estimatedWait + estimatedWalkTime;

            if (totalEstimatedDelay > clampedMaxWait)
            {
                continue;
            }

            if (totalEstimatedDelay < bestWait)
            {
                bestWait = totalEstimatedDelay;
                bestFacility = facility;
            }
        }

        return bestFacility;
    }

    private float GetTargetStoppingDistance(QueuableObject target, Passenger passenger)
    {
        if (target != null)
        {
            return target.GetStoppingDistanceFor(passenger);
        }

        return 0.1f;
    }

    private void AssignServiceWaitTime(Passenger passenger)
    {
        if (passenger == null)
        {
            return;
        }

        float minWait = Mathf.Max(0f, minServiceWaitTime);
        float maxWait = Mathf.Max(minWait, maxServiceWaitTime);
        passenger.maxServiceWaitTime = Random.Range(minWait, maxWait);
    }

    private float EstimateWalkTimeToService(Passenger passenger, QueuableObject target)
    {
        if (passenger == null || target == null)
        {
            return Mathf.Infinity;
        }

        Vector3 targetPosition = GetEstimatedQueueTargetPosition(target, passenger);
        Vector3 routeDestination = targetPosition;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, 4f, NavMesh.AllAreas))
        {
            routeDestination = targetHit.position;
        }

        float routeDistance = EstimateRouteDistance(passenger.transform.position, routeDestination);
        float walkSpeed = GetEstimatedWalkSpeed(passenger);
        return routeDistance / walkSpeed;
    }

    private float EstimateRouteDistance(Vector3 startPosition, Vector3 destination)
    {
        if (facilityRoutePath != null &&
            NavMesh.CalculatePath(startPosition, destination, NavMesh.AllAreas, facilityRoutePath) &&
            facilityRoutePath.status == NavMeshPathStatus.PathComplete)
        {
            return GetPathLength(facilityRoutePath);
        }

        Vector3 flatOffset = destination - startPosition;
        flatOffset.y = 0f;
        return flatOffset.magnitude;
    }

    private float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
        {
            return 0f;
        }

        float distance = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return distance;
    }

    private float GetEstimatedWalkSpeed(Passenger passenger)
    {
        if (passenger != null && passenger.navAgent != null)
        {
            return Mathf.Max(0.1f, passenger.navAgent.speed);
        }

        return FallbackPassengerWalkSpeed;
    }

    private Vector3 GetEstimatedQueueTargetPosition(QueuableObject target, Passenger passenger)
    {
        if (target == null)
        {
            return passenger != null ? passenger.transform.position : Vector3.zero;
        }

        if (target.queueLineMode == QueueLineMode.StoppingDistance)
        {
            return target.transform.position + (target.transform.forward * target.stoppingDistanceTargetDistance);
        }

        int queueIndex = target.PeopleOnWay != null ? target.PeopleOnWay.Count : 0;
        if (target.queueStyle == QueueStyle.FrontOnly)
        {
            float distance = target.baseDistance + (queueIndex * target.queueSpacing);
            return target.transform.position + (target.transform.forward * distance);
        }

        int side = passenger != null ? Mathf.Abs(passenger.GetInstanceID()) % 4 : queueIndex % 4;
        int depth = 0;

        if (target.PeopleOnWay != null && passenger != null)
        {
            for (int i = 0; i < target.PeopleOnWay.Count; i++)
            {
                Person queuedPerson = target.PeopleOnWay[i];
                if (queuedPerson != null && Mathf.Abs(queuedPerson.GetInstanceID()) % 4 == side)
                {
                    depth++;
                }
            }
        }
        else
        {
            depth = queueIndex / 4;
        }

        Vector3 direction = target.transform.forward;
        switch (side)
        {
            case 0:
                direction = target.transform.forward;
                break;
            case 1:
                direction = target.transform.right;
                break;
            case 2:
                direction = -target.transform.forward;
                break;
            case 3:
                direction = -target.transform.right;
                break;
        }

        float offsetDistance = target.baseDistance + (depth * target.queueSpacing);
        return target.transform.position + (direction * offsetDistance);
    }
}
