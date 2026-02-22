using UnityEngine;

[System.Serializable]
public class TrainService
{
    public Train trainData;
    public float nextArrivalTime;
    public TrainController physicalTrainInstance;
    public PlatformController assignedPlatform; 
    
    public TrainService(Train data, PlatformController platform)
    {
        trainData = data;
        assignedPlatform = platform;
        nextArrivalTime = Time.time;
    }
    
    public void OnTrainSpawned() 
    {
        nextArrivalTime = Time.time + trainData.secondsBetweenArrivals; 
    }
    
    public void OnTrainDelayed() 
    {
        nextArrivalTime = Time.time + 1.0f; 
    }
    
    public int TrainPassengerCapacity()
    {
        return trainData.carriageCount * trainData.capacityPerCarriage;
    }
    
    public void EndTotalService()
    {
        PassengerManager.Instance.TrainServiceEnded(this);
        
        if (physicalTrainInstance != null)
        {
            TrainManager.Instance.FreePlatform(physicalTrainInstance.platformNumber);
            GameObject.Destroy(physicalTrainInstance.gameObject);
            physicalTrainInstance = null;
        }
    }
}