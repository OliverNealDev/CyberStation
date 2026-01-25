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
                activeTrainServices.Add(new TrainService(train));
            }
        }
    }

    void Start()
    {
        var newPlatform = new Platform();
        newPlatform.platformVector3 = new Vector3(-8, 0, 68.5f);
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
                // Find an available platform
                foreach (Platform platform in activePlatforms)
                {
                    if (!platform.isOccupied)
                    {
                        // Spawn the train at the platform
                        GameObject trainInstance = Instantiate(service.trainData.trainPrefab, platform.platformVector3, Quaternion.identity);
                        platform.isOccupied = true;

                        // Schedule next arrival
                        service.ScheduleNextArrival();
                        break;
                    }
                }
            }
        }
    }

    private class Platform
    {
        public Vector3 platformVector3; // Specifically where the train stops
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
