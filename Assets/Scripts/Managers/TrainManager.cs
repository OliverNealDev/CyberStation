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
        if (!unlockedTrains.Contains(train)) unlockedTrains.Add(train);
    }

    public void RegisterPlatform(PlatformController platform)
    {
        if (!activePlatforms.Contains(platform)) activePlatforms.Add(platform);
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

    public void AssignTrainToPlatformSlot(Train train, PlatformController platform, int slotIndex)
    {
        RemoveTrainFromService(train);

        if (slotIndex == 1) platform.trainInSlot1 = train;
        else if (slotIndex == 2) platform.trainInSlot2 = train;

        TrainService newService = new TrainService(train, platform);
        activeTrainServices.Add(newService);

        platform.OnTrainAssigned(slotIndex);

        OnTrainAssignmentsChanged?.Invoke();
    }
    
    public void RemoveTrainFromService(Train train)
    {
        foreach (var platform in activePlatforms)
        {
            if (platform.trainInSlot1 == train) platform.trainInSlot1 = null;
            if (platform.trainInSlot2 == train) platform.trainInSlot2 = null;
        }

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
}