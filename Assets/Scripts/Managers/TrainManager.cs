using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    private Train[] allTrains;
    public List<TrainService> activeTrainServices = new List<TrainService>();
    public bool unlockAllTrains = false;
    
    public List<ScheduledArrival> masterSchedule = new List<ScheduledArrival>();
    private int maxScheduledItems = 10; 
    
    public float timeToAnnounceBeforeArrival = 10f;

    [SerializeField]
    public List<Platform> activePlatforms = new List<Platform>();

    public System.Action<ScheduledArrival> OnTrainDepartedUI; 
    public System.Action<ScheduledArrival, int> OnPlatformAnnounced;
    
    public List<Platform> platforms = new List<Platform>();

    void Awake()
    {
        Instance = this;
        allTrains = Resources.LoadAll<Train>("Trains");
        if (unlockAllTrains)
        {
            foreach (var train in allTrains) AddTrainToService(train);
        }
    }

    void Start()
    {
        /*var newPlatform = new Platform();
        newPlatform.trainStopPosition = new Vector3(-8, 0, 68.5f);
        newPlatform.platformNumber = 1;
        newPlatform.maxTrainLength = 5;*/

        foreach (Platform platform in platforms)
        {
            activePlatforms.Add(platform);
        }
        
        masterSchedule = masterSchedule.OrderBy(x => x.arrivalTime).ToList();
    }

    void FixedUpdate()
    {
        CheckAnnouncements();
        CheckServicesDue();
        CheckServicesArrived(); 
        EnsureScheduleBuffer();
    }
    
    private void CheckAnnouncements()
    {
        if (masterSchedule.Count == 0) return;

        foreach (var nextUp in masterSchedule)
        {
            if (nextUp.hasBeenAnnounced || nextUp.hasSpawned) continue;

            if (Time.time >= nextUp.arrivalTime - timeToAnnounceBeforeArrival)
            {
                Platform freePlatform = activePlatforms.FirstOrDefault(p => !p.isOccupied && !p.isReserved);

                if (freePlatform != null)
                {
                    freePlatform.isReserved = true;
                    nextUp.assignedPlatformID = freePlatform.platformNumber;
                    nextUp.hasBeenAnnounced = true;

                    OnPlatformAnnounced?.Invoke(nextUp, freePlatform.platformNumber);
                }
                else
                {
                    nextUp.status = ArrivalStatus.Delayed;
                    nextUp.delayDuration += Time.fixedDeltaTime;
                    nextUp.arrivalTime += Time.fixedDeltaTime; 
                }
            }
        }
    }

    private void CheckServicesDue()
    {
        foreach (var nextUp in masterSchedule)
        {
            if (nextUp.hasSpawned) continue;

            if (Time.time >= nextUp.arrivalTime && nextUp.hasBeenAnnounced)
            {
                Platform platform = activePlatforms.Find(p => p.platformNumber == nextUp.assignedPlatformID);
                
                if (platform != null)
                {
                    SpawnTrain(nextUp, platform);
                }
            }
        }
    }

    private void SpawnTrain(ScheduledArrival arrivalEntry, Platform platform)
    {
        GameObject trainInstance = Instantiate(arrivalEntry.service.trainData.trainPrefab,
            platform.trainStopPosition.position + new Vector3(1000, 0, 0), Quaternion.identity);
        
        TrainController controller = trainInstance.GetComponent<TrainController>();
        arrivalEntry.service.physicalTrainInstance = controller;
        
        platform.isOccupied = true;
        platform.isReserved = false; 
        
        controller.trainData = arrivalEntry.service.trainData;
        controller.trainStopPosition = platform.trainStopPosition.position;
        controller.platformNumber = platform.platformNumber;
        controller.trainService = arrivalEntry.service;

        arrivalEntry.hasSpawned = true;
    }
    
    private void CheckServicesArrived()
    {
        for (int i = masterSchedule.Count - 1; i >= 0; i--)
        {
            ScheduledArrival item = masterSchedule[i];

            if (item.hasSpawned && item.service.physicalTrainInstance != null)
            {
                Platform platform = activePlatforms.Find(p => p.platformNumber == item.service.physicalTrainInstance.platformNumber);
                
                if (platform != null && Vector3.Distance(item.service.physicalTrainInstance.transform.position, platform.trainStopPosition.position) < 0.5f)
                {
                    OnTrainDepartedUI?.Invoke(item);
                    
                    if (PassengerManager.Instance != null)
                    {
                        PassengerManager.Instance.TrainArrived(item.service);
                    }

                    masterSchedule.RemoveAt(i);
                }
            }
        }
    }
    
    public void FreePlatform(int platformNumber)
    {
        Platform platform = activePlatforms.Find(p => p.platformNumber == platformNumber);
        if (platform != null)
        {
            platform.isOccupied = false;
            platform.isReserved = false;
            
             ScheduledArrival departingItem = masterSchedule.FirstOrDefault(x => 
                x.hasSpawned && 
                x.service.physicalTrainInstance != null && 
                x.service.physicalTrainInstance.platformNumber == platformNumber
            );

            if (departingItem != null)
            {
                OnTrainDepartedUI?.Invoke(departingItem);
                masterSchedule.Remove(departingItem);
            }
        }
    }

    private void EnsureScheduleBuffer()
    {
        if (activeTrainServices.Count == 0) return;
        int unspawnedCount = masterSchedule.Count(x => !x.hasSpawned);
        if (unspawnedCount < maxScheduledItems)
        {
            foreach(var service in activeTrainServices)
            {
                ScheduledArrival lastEntry = masterSchedule.LastOrDefault(x => x.service == service);
                float referenceTime = (lastEntry != null) ? lastEntry.originalScheduledTime : Time.time;
                float nextTime = referenceTime + service.trainData.secondsBetweenArrivals;
                
                int countForThisService = masterSchedule.Count(x => x.service == service);
                if(countForThisService < 3) 
                {
                    AddScheduledArrival(service, nextTime);
                    masterSchedule = masterSchedule.OrderBy(x => x.arrivalTime).ToList();
                }
            }
        }
    }

    private void AddScheduledArrival(TrainService service, float time)
    {
        ScheduledArrival arr = new ScheduledArrival();
        arr.service = service;
        arr.arrivalTime = time;
        arr.originalScheduledTime = time;
        arr.status = ArrivalStatus.OnTime;
        arr.hasSpawned = false;
        arr.hasBeenAnnounced = false; 
        arr.uiID = System.Guid.NewGuid().ToString();
        masterSchedule.Add(arr);
    }
    
    public void AddTrainToService(Train train)
    {
        if (activeTrainServices.Exists(x => x.trainData == train)) return;
        TrainService newService = new TrainService(train);
        activeTrainServices.Add(newService);
        AddScheduledArrival(newService, Time.time + train.secondsBetweenArrivals);
        masterSchedule = masterSchedule.OrderBy(x => x.arrivalTime).ToList();
    }
    
    public void RemoveTrainFromService(Train train)
    {
        TrainService serviceToRemove = activeTrainServices.Find(x => x.trainData == train);
        if (serviceToRemove != null)
        {
            serviceToRemove.EndTotalService();
            activeTrainServices.Remove(serviceToRemove);
            masterSchedule.RemoveAll(x => x.service == serviceToRemove);
        }
    }

    public TrainService AssignTrainServiceToPassenger()
    {
        if (activeTrainServices.Count == 0) return null;
        int totalSeatsInService = 0;
        foreach (var service in activeTrainServices) totalSeatsInService += service.TrainPassengerCapacity();
        int randomlyAssignedSeat = Random.Range(0, totalSeatsInService);
        foreach (var service in activeTrainServices)
        {
            int serviceCapacity = service.TrainPassengerCapacity();
            if (randomlyAssignedSeat < serviceCapacity) return service;
            randomlyAssignedSeat -= serviceCapacity;
        }
        return activeTrainServices[0];
    }

    [System.Serializable]
    public class Platform
    {
        public Transform trainStopPosition;
        public Transform passengerWaitingArea;
        public bool isOccupied;
        public bool isReserved;
        public int platformNumber;
        public int maxTrainLength;
    }
}

public enum ArrivalStatus { OnTime, Delayed }

[System.Serializable]
public class ScheduledArrival
{
    public string uiID;
    public TrainService service;
    public float arrivalTime;
    public float originalScheduledTime;
    public ArrivalStatus status;
    public float delayDuration;
    public bool hasSpawned;
    public bool hasBeenAnnounced; 
    public int assignedPlatformID; 
}