using UnityEngine;

public class PassengerSpawner : MonoBehaviour
{
    private const float ServiceCycleDurationSeconds = 240f;
    private const float MinDemandOccupancy = 0.35f;
    private const float MaxDemandOccupancy = 1f;
    private const float DemandCurveExponent = 1.6f;
    private float tickRate = 20f; 
    private const int MaxTicksPerFrame = 8;
    private float tickInterval;
    private float tickTimer = 0f;

    void Start()
    {
        tickInterval = 1f / tickRate;
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        // At 2x and 3x a frame can span several ticks. Running them all keeps arrivals at
        // the right rate; the cap and the reset stop a long stall turning into a surge.
        int ticksThisFrame = 0;
        while (tickTimer >= tickInterval && ticksThisFrame < MaxTicksPerFrame)
        {
            ProcessSpawns();
            tickTimer -= tickInterval;
            ticksThisFrame++;
        }

        if (ticksThisFrame == MaxTicksPerFrame)
        {
            tickTimer = Mathf.Min(tickTimer, tickInterval);
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
            
            float spawnRatePerSecond = targetPassengers * StationModifiers.SpawnMultiplier / ServiceCycleDurationSeconds;
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
        return CalculateTargetOccupancy(RatingManager.Instance.stationRating);
    }

    public static float CalculateTargetOccupancy(float stationRating)
    {
        float normalizedRating = Mathf.InverseLerp(1f, 5f, stationRating);
        float curvedRating = Mathf.Pow(normalizedRating, DemandCurveExponent);
        return Mathf.Lerp(MinDemandOccupancy, MaxDemandOccupancy, curvedRating);
    }
}
