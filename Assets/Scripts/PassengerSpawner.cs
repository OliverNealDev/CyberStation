using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    private const float ServiceCycleDurationSeconds = 240f;
    private const float MinDemandOccupancy = 0.2925f;
    private const float MaxDemandOccupancy = 1.0f;
    private const float DemandCurveExponent = 1.8408775f;
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
        if (!PassengerManager.Instance.HasMaterializer()) return;

        float targetOccupancy = GetTargetOccupancy();

        foreach (var service in TrainManager.Instance.activeTrainServices)
        {
            if (service.trainData == null) continue;

            float capacity = service.TrainPassengerCapacity();
            float targetPassengers = capacity * targetOccupancy;
            
            float spawnRatePerSecond = targetPassengers / ServiceCycleDurationSeconds;
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

    private float GetTargetOccupancy()
    {
        float normalizedRating = Mathf.InverseLerp(1f, 5f, RatingManager.Instance.stationRating);
        float curvedRating = Mathf.Pow(normalizedRating, DemandCurveExponent);
        return Mathf.Lerp(MinDemandOccupancy, MaxDemandOccupancy, curvedRating);
    }
}
