using UnityEngine;

public class PlatformController : MonoBehaviour
{
    public string platformName = "Platform 1";
    public int platformNumber;
    public Transform trainStopPoint; 
    public BoxCollider passengerWaitArea; 
    public bool isOccupied = false;

    public Train trainInSlot1;
    public Train trainInSlot2;

    private float spawnTimer = 120f;
    private int nextSlotToSpawn = 1;

    void Start()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.RegisterPlatform(this);
        }
    }

    void Update()
    {
        if (isOccupied) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = 120f;
            SpawnNextTrain();
        }
    }

    void SpawnNextTrain()
    {
        Train trainToSpawn = nextSlotToSpawn == 1 ? trainInSlot1 : trainInSlot2;
        nextSlotToSpawn = nextSlotToSpawn == 1 ? 2 : 1;

        if (trainToSpawn != null && TrainManager.Instance != null)
        {
            TrainService service = TrainManager.Instance.GetServiceByTrain(trainToSpawn);
            
            if (service != null)
            {
                Vector3 spawnPosition = trainStopPoint.position - (trainStopPoint.forward * 1000f);
                GameObject trainInstance = Instantiate(trainToSpawn.trainPrefab, spawnPosition, trainStopPoint.rotation);
                
                TrainController controller = trainInstance.GetComponent<TrainController>();
                service.physicalTrainInstance = controller;
                
                isOccupied = true;
                
                controller.trainData = trainToSpawn;
                controller.trainStopPoint = trainStopPoint;
                controller.platformNumber = platformNumber;
                controller.trainService = service;
            }
        }
    }
    
    public void OnTrainAssigned(int slotIndex)
    {
        nextSlotToSpawn = slotIndex;
        
        bool isOnlyTrain = (slotIndex == 1 && trainInSlot2 == null) || (slotIndex == 2 && trainInSlot1 == null);

        if (isOnlyTrain && !isOccupied) 
        {
            spawnTimer = 0f;
        }
    }

    public Vector3 GetRandomWaitPosition()
    {
        if (passengerWaitArea == null) return transform.position;

        Bounds bounds = passengerWaitArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, 0, randomZ);
    }
}