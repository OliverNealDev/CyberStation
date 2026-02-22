using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    public List<TrainService> activeTrainServices = new List<TrainService>();
    public bool unlockAllTrains = false;
    
    public List<PlatformController> activePlatforms = new List<PlatformController>();

    void Awake()
    {
        Instance = this;
        allTrains = Resources.LoadAll<Train>("Trains");
    }

    void Start()
    {
        if (unlockAllTrains && activePlatforms.Count > 0)
        {
            AddTrainToService(allTrains[0]);
        }
    }

    public void RegisterPlatform(PlatformController platform)
    {
        if (!activePlatforms.Contains(platform))
        {
            activePlatforms.Add(platform);
        }
    }

    void FixedUpdate()
    {
        foreach (var service in activeTrainServices)
        {
            if (Time.time >= service.nextArrivalTime)
            {
                if (!service.assignedPlatform.isOccupied)
                {
                    SpawnTrainForService(service);
                }
                else
                {
                    service.OnTrainDelayed();
                }
            }
        }
    }

    private void SpawnTrainForService(TrainService service)
    {
        PlatformController platform = service.assignedPlatform;
        
        Vector3 spawnPosition = platform.trainStopPoint.position - (platform.trainStopPoint.forward * 1000f);
        
        GameObject trainInstance = Instantiate(service.trainData.trainPrefab, spawnPosition, platform.trainStopPoint.rotation);
                        
        TrainController controller = trainInstance.GetComponent<TrainController>();
        service.physicalTrainInstance = controller;
                        
        platform.isOccupied = true;
                        
        controller.trainData = service.trainData;
        controller.trainStopPoint = platform.trainStopPoint;
        controller.platformNumber = platform.platformNumber;
        controller.trainService = service;

        service.OnTrainSpawned();
    }

    public TrainService AssignTrainServiceToPassenger()
    {
        if (activeTrainServices.Count == 0) return null;

        int totalSeatsInService = 0;
        foreach (var service in activeTrainServices)
        {
            totalSeatsInService += service.TrainPassengerCapacity();
        }
        
        int randomlyAssignedSeat = Random.Range(0, totalSeatsInService);
        foreach (var service in activeTrainServices)
        {
            int serviceCapacity = service.TrainPassengerCapacity();
            if (randomlyAssignedSeat < serviceCapacity)
            {
                return service;
            }
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
    
    public void AddTrainToService(Train train)
    {
        foreach (var service in activeTrainServices) 
        {
            if (service.trainData == train)
            {
                return;
            }
        }
        
        PlatformController bestPlatform = GetBestPlatformForService();
        if (bestPlatform != null)
        {
            TrainService newService = new TrainService(train, bestPlatform);
            activeTrainServices.Add(newService);
        }
    }

    private PlatformController GetBestPlatformForService()
    {
        if (activePlatforms.Count == 0) return null;

        PlatformController bestPlatform = null;
        int lowestServiceCount = int.MaxValue;

        foreach (PlatformController platform in activePlatforms)
        {
            int currentServiceCount = 0;
            
            foreach (TrainService service in activeTrainServices)
            {
                if (service.assignedPlatform == platform)
                {
                    currentServiceCount++;
                }
            }

            if (currentServiceCount < lowestServiceCount)
            {
                lowestServiceCount = currentServiceCount;
                bestPlatform = platform;
                
                if (lowestServiceCount == 0)
                {
                    return bestPlatform;
                }
            }
        }

        return bestPlatform;
    }
    
    public void RemoveTrainFromService(Train train)
    {
        for (int i = activeTrainServices.Count - 1; i >= 0; i--)
        {
            if (activeTrainServices[i].trainData == train)
            {
                activeTrainServices[i].EndTotalService();
                activeTrainServices.RemoveAt(i);
            }
        }
    }
}