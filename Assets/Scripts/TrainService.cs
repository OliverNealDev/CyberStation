using UnityEngine;

[System.Serializable]
public class TrainService
{
    public Train trainData;
    public float nextArrivalTime;
    
    public TrainService(Train data)
    {
        trainData = data;
        nextArrivalTime = Time.time + data.secondsBetweenArrivals;
    }

    public void ScheduleNextArrival()
    {
        nextArrivalTime = Time.time + trainData.secondsBetweenArrivals;
    }
}