using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public Train trainData;
    public TrainService trainService;
    public Vector3 trainStopPosition;
    public int platformNumber;

    private float timeStationary;
    private float currentSpeed;
    private float acceleration;
    private float deceleration;

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

        if (trainData.carriageCount > 1)
        {
            for (int i = 2; i < trainData.carriageCount + 1; i++)
            {
                Vector3 carriagePosition = transform.position + new Vector3(i * trainData.carriageLength, 3, 0);
                Instantiate(trainData.carriagePrefab, carriagePosition, Quaternion.identity, transform);
            }
        }
    }
    
    void Update()
    {
        switch (currentState)
        {
            case trainStates.Approaching:
                float distToStop = Vector3.Distance(transform.position, trainStopPosition);

                if (distToStop > 0.01f)
                {
                    float maxAllowedSpeed = Mathf.Sqrt(2 * deceleration * distToStop);
                    float targetSpeed = Mathf.Min(trainData.speed, maxAllowedSpeed);
                    
                    float speedChangeRate = currentSpeed > targetSpeed ? deceleration : acceleration;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
                    
                    transform.position = Vector3.MoveTowards(transform.position, trainStopPosition, currentSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = trainStopPosition;
                    currentSpeed = 0f;
                    currentState = trainStates.Stationary;
                    PassengerManager.Instance.TrainArrived(trainService);
                }
                break;
            
            case trainStates.Stationary:
                timeStationary += Time.deltaTime;
                if (timeStationary >= trainData.secondsStationary)
                {
                    currentState = trainStates.Departing;
                }
                break;
            
            case trainStates.Departing:
                Vector3 departTarget = trainStopPosition - new Vector3(1000, 0, 0);

                if (transform.position != departTarget)
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