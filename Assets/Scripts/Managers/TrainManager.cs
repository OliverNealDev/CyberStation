using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    public List<TrainService> activeTrainServices = new List<TrainService>();
    public bool unlockAllTrains = false;
    
    [SerializeField]
    public List<Platform> activePlatforms = new List<Platform>();

    void Awake()
    {
        Instance = this;
        
        allTrains = Resources.LoadAll<Train>("Trains");
        if (unlockAllTrains)
        {
            foreach (var train in allTrains)
            {
                TrainService newService = new TrainService(train);
                activeTrainServices.Add(newService);
            }
        }
    }

    void Start()
    {
        var newPlatform = new Platform();
        newPlatform.trainStopPosition = new Vector3(-8, 0, 68.5f);
        newPlatform.isOccupied = false;
        newPlatform.platformNumber = 1;
        newPlatform.maxTrainLength = 5;
        activePlatforms.Add(newPlatform);
    }

    void FixedUpdate()
    {
        CheckServicesDue();
    }
    
    private void CheckServicesDue()
    {
        foreach (var service in activeTrainServices)
        {
            if (Time.time >= service.nextArrivalTime)
            {
                bool isAnyPlatformAvailable = false;
                foreach (Platform platform in activePlatforms)
                {
                    if (!platform.isOccupied)
                    {
                        GameObject trainInstance = Instantiate(service.trainData.trainPrefab,
                            platform.trainStopPosition + new Vector3(1000, 0, 0), Quaternion.identity);
                        
                        TrainController controller = trainInstance.GetComponent<TrainController>();
                        service.physicalTrainInstance = controller;
                        
                        platform.isOccupied = true;
                        
                        controller.trainData = service.trainData;
                        controller.trainStopPosition = platform.trainStopPosition;
                        controller.platformNumber = platform.platformNumber;
                        controller.trainService = service;

                        service.OnTrainSpawned();
                        isAnyPlatformAvailable = true;
                        break;
                    }
                }

                if (!isAnyPlatformAvailable)
                {
                    service.OnTrainDelayed();
                }
            }
        }
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
        foreach (Platform platform in activePlatforms)
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
        foreach (var service in activeTrainServices) // Don't add a new service if one already exists for this train type
        {
            if (service.trainData == train)
            {
                return;
            }
        }
        
        TrainService newService = new TrainService(train);
        activeTrainServices.Add(newService);
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

    [System.Serializable]
    public class Platform
    {
        public Vector3 trainStopPosition; 
        public bool isOccupied;
        public int platformNumber;
        public int maxTrainLength;
    }
}