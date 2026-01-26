using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    private List<TrainService> activeTrainServices = new List<TrainService>();
    public bool unlockAllTrains = false;
    
    [SerializeField]
    private List<Platform> activePlatforms = new List<Platform>();

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
                newService.AddTrainToService();
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
                            platform.trainStopPosition + new Vector3(100, 0, 0), Quaternion.identity);
                        platform.isOccupied = true;
                        trainInstance.GetComponent<TrainController>().trainData = service.trainData; // needs optimised
                        trainInstance.GetComponent<TrainController>().trainStopPosition = platform.trainStopPosition; // needs optimised
                        trainInstance.GetComponent<TrainController>().platformNumber = platform.platformNumber; // needs optimised
                        trainInstance.GetComponent<TrainController>().trainService = service;

                        // Schedule next arrival
                        service.ConfirmArrival();
                        isAnyPlatformAvailable = true;
                        break;
                    }
                }

                if (!isAnyPlatformAvailable)
                {
                    service.RescheduleCurrentArrival(); // Try again in 1 second
                    Debug.Log("Train rescheduled due to no available platforms.");
                }
            }
        }
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

    private class Platform
    {
        public Vector3 trainStopPosition; // Specifically where the train stops
        public bool isOccupied;
        public int platformNumber;
        public int maxTrainLength;
    }
    
    /* TO DO
     Train has a controller and pulls into the platform, waits, then departs
     Train triggers platform to free up after departure, and schedules next arrival
     Platform creates area of waiting for passengers?
     
     Passenger spawning and boarding system, probably do ticket machine first
     */
}
