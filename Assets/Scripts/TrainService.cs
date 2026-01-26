using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrainService
{
    public Train trainData;
    public List<float> arrivalTimes = new List<float>();

    public int trainsInService => arrivalTimes.Count; 
    
    public TrainService(Train data)
    {
        trainData = data;
        AddTrainToService();
    }

    public void ConfirmArrival() 
    {
        if (arrivalTimes.Count > 0)
        {
            arrivalTimes.RemoveAt(0); 
        }
    }
    
    public void ScheduleNextArrival() 
    {
        arrivalTimes.Add(Time.time + trainData.secondsBetweenArrivals); 
    }
    
    public void RescheduleCurrentArrival() 
    {
        if (arrivalTimes.Count > 0)
        {
            arrivalTimes.RemoveAt(0); 
        }
        arrivalTimes.Add(Time.time + 1.0f); 
    }
    
    public float nextArrivalTime
    {
        get
        {
            if (arrivalTimes.Count > 0) return arrivalTimes[0];
            return float.MaxValue;
        }
    }
    
    public void AddTrainToService()
    {
        arrivalTimes.Add(Time.time); 
        Debug.Log("Train added. Total in service: " + trainsInService);
    }
}