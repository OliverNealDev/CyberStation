using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    public static event Action OnTrainAssignmentsChanged;

    private Train[] allTrains;
    public List<TrainService> activeTrainServices = new List<TrainService>();
    public List<PlatformController> activePlatforms = new List<PlatformController>();
    
    public List<Train> unlockedTrains = new List<Train>();
    public PlatformController pendingPlatform;
    public int pendingSlot = -1;

    public bool unlockAllTrains = false;

    void Awake()
    {
        Instance = this;
        allTrains = Resources.LoadAll<Train>("Trains");
    }

    void Start()
    {
        if (unlockAllTrains)
        {
            foreach (var train in allTrains) UnlockTrain(train);
        }
    }

    public int GetTotalTrainCount()
    {
        return allTrains != null ? allTrains.Length : 0;
    }

    public void UnlockTrain(Train train)
    {
        if (!unlockedTrains.Contains(train))
        {
            unlockedTrains.Add(train);

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.RecordTrainUnlocked();
            }

            NotifyTrainAssignmentsChanged();
        }
    }

    public void RegisterPlatform(PlatformController platform)
    {
        if (!activePlatforms.Contains(platform))
        {
            activePlatforms.Add(platform);
            NotifyTrainAssignmentsChanged();
        }
    }

    public void NotifyTrainAssignmentsChanged()
    {
        OnTrainAssignmentsChanged?.Invoke();
    }

    public TrainService GetServiceByTrain(Train train)
    {
        return activeTrainServices.Find(s => s.trainData == train);
    }

    public TrainService AssignTrainServiceToPassenger()
    {
        if (activeTrainServices.Count == 0) return null;

        int totalSeatsInService = 0;
        foreach (var service in activeTrainServices) totalSeatsInService += service.TrainPassengerCapacity();
        
        if (totalSeatsInService <= 0) return activeTrainServices[0];

        int randomlyAssignedSeat = Random.Range(0, totalSeatsInService);
        foreach (var service in activeTrainServices)
        {
            int serviceCapacity = service.TrainPassengerCapacity();
            if (randomlyAssignedSeat < serviceCapacity) return service;
            randomlyAssignedSeat -= serviceCapacity;
        }
        return activeTrainServices[0];
    }
    
    public void FreePlatform(int platformNumber)
    {
        foreach (var platform in activePlatforms)
        {
            if (platform.platformNumber == platformNumber)
            {
                platform.isOccupied = false;
                return;
            }
        }
    }

    public bool SpawnTrainService(TrainService service)
    {
        if (service == null || service.trainData == null || service.assignedPlatform == null || service.assignedPlatform.trainStopPoint == null)
        {
            return false;
        }

        GameObject trainPrefab = service.trainData.trainPrefab;
        if (trainPrefab == null)
        {
            Debug.LogWarning($"TrainManager cannot spawn {service.trainData.name} because its train prefab is not assigned.");
            return false;
        }

        PlatformController platform = service.assignedPlatform;
        Vector3 spawnPosition = platform.trainStopPoint.position - (platform.trainStopPoint.forward * 1000f);
        GameObject trainInstance = Instantiate(trainPrefab, spawnPosition, platform.trainStopPoint.rotation);
        TrainController controller = trainInstance.GetComponent<TrainController>();

        if (controller == null)
        {
            Debug.LogWarning("The assigned generic train prefab is missing a TrainController component.");
            Destroy(trainInstance);
            return false;
        }

        controller.trainData = service.trainData;
        controller.trainService = service;
        controller.trainStopPoint = platform.trainStopPoint;
        controller.platformNumber = platform.platformNumber;
        service.physicalTrainInstance = controller;
        platform.isOccupied = true;

        SoundEffectController.Play(SoundEffectId.TrainApproaching);
        return true;
    }

    public void AssignTrainToPlatformSlot(Train train, PlatformController platform, int slotIndex)
    {
        if (platform == null || !IsValidSlotIndex(slotIndex))
        {
            return;
        }

        Train occupyingTrain = GetTrainInSlot(platform, slotIndex);
        if (occupyingTrain == train)
        {
            return;
        }

        if (occupyingTrain != null)
        {
            TrainService movingService = GetServiceByTrain(train);
            TrainService occupyingService = GetServiceByTrain(occupyingTrain);

            if (movingService != null &&
                occupyingService != null &&
                TryFindTrainAssignment(train, out PlatformController sourcePlatform, out int sourceSlotIndex))
            {
                SwapTrainAssignments(
                    train,
                    movingService,
                    sourcePlatform,
                    sourceSlotIndex,
                    occupyingTrain,
                    occupyingService,
                    platform,
                    slotIndex);

                OnTrainAssignmentsChanged?.Invoke();
                return;
            }

            RemoveTrainFromService(occupyingTrain);
        }

        if (train == null)
        {
            return;
        }

        TrainService service = GetServiceByTrain(train);
        PlatformController previousPlatform = service != null ? service.assignedPlatform : null;
        bool platformChanged = service != null && previousPlatform != platform;

        ClearTrainAssignments(train);

        if (service == null)
        {
            service = new TrainService(train, platform);
            activeTrainServices.Add(service);
        }
        else
        {
            service.assignedPlatform = platform;
        }

        SetTrainInSlot(platform, slotIndex, train);

        if (platformChanged)
        {
            if (PassengerManager.Instance != null)
            {
                PassengerManager.Instance.OnTrainServiceReassigned(service, previousPlatform, platform);
            }

            DespawnPhysicalTrain(service);
        }

        platform.OnTrainAssigned(slotIndex);
        OnTrainAssignmentsChanged?.Invoke();
    }
    
    public void RemoveTrainFromService(Train train)
    {
        if (train == null)
        {
            return;
        }

        ClearTrainAssignments(train);

        for (int i = activeTrainServices.Count - 1; i >= 0; i--)
        {
            if (activeTrainServices[i].trainData == train)
            {
                activeTrainServices[i].EndTotalService();
                activeTrainServices.RemoveAt(i);
            }
        }

        OnTrainAssignmentsChanged?.Invoke();
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex == 1 || slotIndex == 2;
    }

    private Train GetTrainInSlot(PlatformController platform, int slotIndex)
    {
        if (platform == null)
        {
            return null;
        }

        if (slotIndex == 1)
        {
            return platform.trainInSlot1;
        }

        if (slotIndex == 2)
        {
            return platform.trainInSlot2;
        }

        return null;
    }

    private void SetTrainInSlot(PlatformController platform, int slotIndex, Train train)
    {
        if (platform == null)
        {
            return;
        }

        if (slotIndex == 1)
        {
            platform.trainInSlot1 = train;
        }
        else if (slotIndex == 2)
        {
            platform.trainInSlot2 = train;
        }
    }

    private void ClearTrainAssignments(Train train)
    {
        foreach (var platform in activePlatforms)
        {
            if (platform.trainInSlot1 == train) platform.trainInSlot1 = null;
            if (platform.trainInSlot2 == train) platform.trainInSlot2 = null;
        }
    }

    private void DespawnPhysicalTrain(TrainService service)
    {
        if (service == null || service.physicalTrainInstance == null)
        {
            return;
        }

        FreePlatform(service.physicalTrainInstance.platformNumber);
        GameObject.Destroy(service.physicalTrainInstance.gameObject);
        service.physicalTrainInstance = null;
    }

    private bool TryFindTrainAssignment(Train train, out PlatformController platform, out int slotIndex)
    {
        platform = null;
        slotIndex = -1;

        if (train == null)
        {
            return false;
        }

        for (int i = 0; i < activePlatforms.Count; i++)
        {
            PlatformController candidatePlatform = activePlatforms[i];
            if (candidatePlatform == null)
            {
                continue;
            }

            if (candidatePlatform.trainInSlot1 == train)
            {
                platform = candidatePlatform;
                slotIndex = 1;
                return true;
            }

            if (candidatePlatform.trainInSlot2 == train)
            {
                platform = candidatePlatform;
                slotIndex = 2;
                return true;
            }
        }

        return false;
    }

    private void SwapTrainAssignments(
        Train movingTrain,
        TrainService movingService,
        PlatformController sourcePlatform,
        int sourceSlotIndex,
        Train occupyingTrain,
        TrainService occupyingService,
        PlatformController targetPlatform,
        int targetSlotIndex)
    {
        if (movingTrain == null ||
            movingService == null ||
            sourcePlatform == null ||
            !IsValidSlotIndex(sourceSlotIndex) ||
            occupyingTrain == null ||
            occupyingService == null ||
            targetPlatform == null ||
            !IsValidSlotIndex(targetSlotIndex))
        {
            return;
        }

        PlatformController movingPreviousPlatform = movingService.assignedPlatform;
        PlatformController occupyingPreviousPlatform = occupyingService.assignedPlatform;
        bool movingPlatformChanged = movingPreviousPlatform != targetPlatform;
        bool occupyingPlatformChanged = occupyingPreviousPlatform != sourcePlatform;

        ClearTrainAssignments(movingTrain);
        ClearTrainAssignments(occupyingTrain);

        SetTrainInSlot(targetPlatform, targetSlotIndex, movingTrain);
        SetTrainInSlot(sourcePlatform, sourceSlotIndex, occupyingTrain);

        movingService.assignedPlatform = targetPlatform;
        occupyingService.assignedPlatform = sourcePlatform;

        if (movingPlatformChanged)
        {
            NotifyPassengersOfPlatformChange(movingService, movingPreviousPlatform, targetPlatform);
            DespawnPhysicalTrain(movingService);
        }

        if (occupyingPlatformChanged)
        {
            NotifyPassengersOfPlatformChange(occupyingService, occupyingPreviousPlatform, sourcePlatform);
            DespawnPhysicalTrain(occupyingService);
        }

        if (sourcePlatform == targetPlatform)
        {
            targetPlatform.SetNextSlotToSpawn(targetSlotIndex);
        }
        else
        {
            sourcePlatform.SetNextSlotToSpawn(sourceSlotIndex);
            targetPlatform.SetNextSlotToSpawn(targetSlotIndex);
        }
    }

    private void NotifyPassengersOfPlatformChange(TrainService service, PlatformController previousPlatform, PlatformController newPlatform)
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.OnTrainServiceReassigned(service, previousPlatform, newPlatform);
        }
    }
}
