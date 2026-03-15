using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    [Range(1, 5)]
    public int stationRating = 3;

    private float tickRate = 20f; 
    private float tickInterval;
    private float tickTimer = 0f;

    void Start()
    {
        tickInterval = 1f / tickRate;
    }

    void Update()
    {
        tickTimer += Time.deltaTime;
        
        if (tickTimer >= tickInterval)
        {
            ProcessSpawns();
            tickTimer -= tickInterval;
        }
    }

    private void ProcessSpawns()
    {
        if (TrainManager.Instance == null || PassengerManager.Instance == null) return;

        float efficiency = Mathf.Lerp(0.2f, 1.0f, (stationRating - 1) / 4f);

        foreach (var service in TrainManager.Instance.activeTrainServices)
        {
            if (service.trainData == null) continue;

            float capacity = service.TrainPassengerCapacity();
            float targetPassengers = capacity * efficiency;
            
            float spawnRatePerSecond = targetPassengers / 240f; 
            float spawnProbability = spawnRatePerSecond * tickInterval;

            int guaranteedSpawns = Mathf.FloorToInt(spawnProbability);
            float remainingProbability = spawnProbability - guaranteedSpawns;

            for (int i = 0; i < guaranteedSpawns; i++)
            {
                PassengerManager.Instance.SpawnPassengerForService(service);
            }

            if (Random.value < remainingProbability)
            {
                PassengerManager.Instance.SpawnPassengerForService(service);
            }
        }
    }
}