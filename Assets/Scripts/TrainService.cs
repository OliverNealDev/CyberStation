using UnityEngine;

[System.Serializable]
public class TrainService
{
    public Train trainData;
    public TrainController physicalTrainInstance;
    public PlatformController assignedPlatform; 
    
    public TrainService(Train data, PlatformController platform)
    {
        trainData = data;
        assignedPlatform = platform;
    }
    
    public int TrainPassengerCapacity()
    {
        return trainData.carriageCount * trainData.capacityPerCarriage;
    }
    
    public void EndTotalService()
    {
        if (PassengerManager.Instance != null)
        {
            PassengerManager.Instance.TrainServiceEnded(this);
        }
        
        if (physicalTrainInstance != null)
        {
            TrainManager.Instance.FreePlatform(physicalTrainInstance.platformNumber);
            GameObject.Destroy(physicalTrainInstance.gameObject);
            physicalTrainInstance = null;
        }
    }
}