using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    public List<TrainService> activeTrainServices = new List<TrainService>();
    public bool unlockAllTrains = false;
    //public List<TrainController> trainInstances = new List<TrainController>();
    
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
                        // Spawn the train at the platform
                        GameObject trainInstance = Instantiate(service.trainData.trainPrefab,
                            platform.trainStopPosition + new Vector3(1000, 0, 0), Quaternion.identity);
                        service.physicalTrainInstance = trainInstance.GetComponent<TrainController>();
                        platform.isOccupied = true;
                        trainInstance.GetComponent<TrainController>().trainData = service.trainData; // needs optimised
                        trainInstance.GetComponent<TrainController>().trainStopPosition = platform.trainStopPosition; // needs optimised
                        trainInstance.GetComponent<TrainController>().platformNumber = platform.platformNumber; // needs optimised
                        trainInstance.GetComponent<TrainController>().trainService = service;
                        //trainInstances.Add(trainInstance.GetComponent<TrainController>());

                        // Schedule next arrival
                        service.ConfirmArrival();
                        isAnyPlatformAvailable = true;
                        break;
                    }
                }

                if (!isAnyPlatformAvailable)
                {
                    service.RescheduleCurrentArrival(); // Try again in 1 second
                    //Debug.Log("Train rescheduled due to no available platforms.");
                }
            }
        }
    }

    public TrainService AssignTrainServiceToPassenger()
    {
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
        return null; // Should never reach here
    }
    
    public void FreePlatform(int platformNumber)
    {
        foreach (Platform platform in activePlatforms)
        {
            if (platform.platformNumber == platformNumber)
            {
                platform.isOccupied = false;
                Debug.Log("Platform " + platformNumber + " is now free.");
                return;
            }
        }
        Debug.LogWarning("Platform " + platformNumber + " not found!");
    }
    
    public void AddTrainToService(Train train)
    {
        TrainService newService = new TrainService(train);
        activeTrainServices.Add(newService);
    }

    public class Platform
    {
        public Vector3 trainStopPosition; // Specifically where the train stops
        public bool isOccupied;
        public int platformNumber;
        public int maxTrainLength;
    }
}
