using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public Train trainData;
    public TrainService trainService;
    public Transform trainStopPoint; 
    public int platformNumber;

    private float timeStationary;
    private float currentSpeed;
    private float acceleration;
    private float deceleration;

    [SerializeField] private List<Material> changeableMaterials;
    
    // We will store all the doors here to check if they are busy
    private TrainDoorController[] trainDoors; 

    private trainStates currentState = trainStates.Approaching;
    private enum trainStates
    {
        Approaching,
        Stationary,
        Departing
    }

    void Start()
    {
        currentSpeed = trainData.speed;
        acceleration = trainData.speed / 16f;
        deceleration = trainData.speed / 16f;

        if (trainStopPoint != null)
        {
            transform.rotation = trainStopPoint.rotation;
        }

        // Spawn carriages
        if (trainData.carriageCount > 1)
        {
            for (int i = 2; i < trainData.carriageCount + 1; i++)
            {
                Vector3 carriagePosition = transform.position - (transform.forward * (i * trainData.carriageLength)) + new Vector3(0, 3, 0);
                Instantiate(trainData.carriagePrefab, carriagePosition, transform.rotation, transform);
            }
        }
        
        // Grab all doors AFTER carriages are spawned
        trainDoors = GetComponentsInChildren<TrainDoorController>();
    }
    
    void Update()
    {
        if (trainStopPoint == null) return;

        switch (currentState)
        {
            case trainStates.Approaching:
                float distToStop = Vector3.Distance(transform.position, trainStopPoint.position);

                if (distToStop > 0.01f)
                {
                    float maxAllowedSpeed = Mathf.Sqrt(2 * deceleration * distToStop);
                    float targetSpeed = Mathf.Min(trainData.speed, maxAllowedSpeed);
                    
                    float speedChangeRate = currentSpeed > targetSpeed ? deceleration : acceleration;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
                    
                    transform.position = Vector3.MoveTowards(transform.position, trainStopPoint.position, currentSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = trainStopPoint.position;
                    currentSpeed = 0f;
                    currentState = trainStates.Stationary;
                    PassengerManager.Instance.TrainArrived(trainService);
                }
                break;
            
            case trainStates.Stationary:
                timeStationary += Time.deltaTime;
                
                if (timeStationary >= trainData.secondsStationary)
                {
                    if (IsReadyToDepart())
                    {
                        foreach (TrainDoorController door in trainDoors)
                        {
                            door.CloseDoors();
                        }

                        if (PassengerManager.Instance != null)
                        {
                            PassengerManager.Instance.ResetWaitingPassengersForNextTrain(trainService);
                        }
                        
                        currentState = trainStates.Departing;
                    }
                }
                break;
            
            case trainStates.Departing:
                Vector3 departTarget = trainStopPoint.position + (trainStopPoint.forward * 1000f);

                if (Vector3.Distance(transform.position, departTarget) > 0.1f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, trainData.speed, acceleration * Time.deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, departTarget, currentSpeed * Time.deltaTime);
                }
                else
                {
                    TrainManager.Instance.FreePlatform(platformNumber);
                    Destroy(gameObject);
                }
                break;
        }
    }
    
    public bool IsAtStation()
    {
        return currentState == trainStates.Stationary;
    }

    private bool IsReadyToDepart()
    {
        foreach (TrainDoorController door in trainDoors)
        {
            if (!door.IsAvailable) return false; 
        }
        
        if (timeStationary >= trainData.secondsStationary + 15f)
        {
            return true;
        }

        if (PassengerManager.Instance != null && PassengerManager.Instance.ArePassengersWaitingForTrain(trainService))
        {
            return false;
        }

        return true;
    }

    public List<Vector3> GetDoorPositions()
    {
        List<Vector3> doorPositions = new List<Vector3>();
        
        foreach (Transform child in transform)
        {
            if (child.CompareTag("TrainCarriage"))
            {
                foreach (Transform door in child)
                {
                    if (door.CompareTag("TrainDoor"))
                    {
                        doorPositions.Add(door.position);
                    }
                }
            }
        }
        
        return doorPositions;
    }
}
