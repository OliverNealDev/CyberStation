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

    [System.NonSerialized] private Sprite runtimeIcon;

    private float spawnTimer = 120f;
    private int nextSlotToSpawn = 1;

    public Sprite GetIcon()
    {
        if (runtimeIcon == null)
        {
            runtimeIcon = PrefabIconRenderer.GetIcon(
                gameObject,
                null,
                PrefabIconView.BuildablesAndStaff,
                GetInstanceID().ToString(),
                null);
        }

        return runtimeIcon;
    }

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
        int slotToSpawn = nextSlotToSpawn;
        Train trainToSpawn = GetTrainInSlot(slotToSpawn);

        if (trainToSpawn == null)
        {
            slotToSpawn = GetFallbackSlot(slotToSpawn);
            trainToSpawn = GetTrainInSlot(slotToSpawn);
        }

        if (trainToSpawn == null)
        {
            return;
        }

        nextSlotToSpawn = GetNextSlotAfter(slotToSpawn);

        if (trainToSpawn != null && TrainManager.Instance != null)
        {
            TrainService service = TrainManager.Instance.GetServiceByTrain(trainToSpawn);

            if (service != null)
            {
                TrainManager.Instance.SpawnTrainService(service);
            }
        }
    }

    private Train GetTrainInSlot(int slotIndex)
    {
        return slotIndex == 1 ? trainInSlot1 : trainInSlot2;
    }

    private int GetFallbackSlot(int preferredSlot)
    {
        int alternateSlot = preferredSlot == 1 ? 2 : 1;

        if (GetTrainInSlot(alternateSlot) != null)
        {
            return alternateSlot;
        }

        return preferredSlot;
    }

    private int GetNextSlotAfter(int spawnedSlot)
    {
        int alternateSlot = spawnedSlot == 1 ? 2 : 1;
        return GetTrainInSlot(alternateSlot) != null ? alternateSlot : spawnedSlot;
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

    public void SetNextSlotToSpawn(int slotIndex)
    {
        if (slotIndex == 1 || slotIndex == 2)
        {
            nextSlotToSpawn = slotIndex;
        }
    }

    public Vector3 GetRandomWaitPosition()
    {
        if (passengerWaitArea == null) return transform.position;

        Bounds bounds = passengerWaitArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        float waitY = bounds.center.y;

        return new Vector3(randomX, waitY, randomZ);
    }

    public Vector3 GetPassengerWaitingLookTarget(Vector3 passengerPosition)
    {
        Vector3 facingDirection = transform.forward;

        if (trainStopPoint != null)
        {
            facingDirection = trainStopPoint.right;
        }

        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = Vector3.forward;
        }

        return passengerPosition + facingDirection.normalized;
    }
}
