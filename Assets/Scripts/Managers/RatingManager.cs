using UnityEngine;

public class RatingManager : MonoBehaviour
{
    public static RatingManager Instance;

    public float stationRating = 5f;
    public float cleanlinessRating = 5f;
    public float crowdednessRating = 5f;
    public float queueTimesRating = 5f;
    public float passengerNeedsRating = 5f;
    public float stationSizeRating = 5f;
    public float decorationRating = 5f;

    public const float DecorationSampleRadius = 8f;
    public const float MaxDecorationScorePerPassenger = 5f;

    private float tickTimer;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= 1f)
        {
            CalculateTargetsAndApply();
            tickTimer -= 1f;
        }
    }

    void CalculateTargetsAndApply()
    {
        float targetCleanliness = GetCleanlinessTarget();
        float targetCrowdedness = GetCrowdednessTarget();
        float targetQueueTimes = GetQueueTimesTarget();
        float targetStationSize = GetStationSizeTarget();
        float targetPassengerNeeds = GetPassengerNeedsTarget();
        float targetDecoration = GetDecorationTarget();

        cleanlinessRating = Mathf.Lerp(cleanlinessRating, targetCleanliness, 0.2f);
        crowdednessRating = Mathf.Lerp(crowdednessRating, targetCrowdedness, 0.2f);
        queueTimesRating = Mathf.Lerp(queueTimesRating, targetQueueTimes, 0.2f);
        stationSizeRating = Mathf.Lerp(stationSizeRating, targetStationSize, 0.2f);
        passengerNeedsRating = Mathf.Lerp(passengerNeedsRating, targetPassengerNeeds, 0.2f);
        decorationRating = Mathf.Lerp(decorationRating, targetDecoration, 0.2f);

        stationRating = (cleanlinessRating + crowdednessRating + queueTimesRating + passengerNeedsRating + stationSizeRating + decorationRating) / 6f;
    }
    
    float GetCleanlinessTarget()
    {
        int totalLitter = JanitorCoordinator.Instance.allLitter.Count;
        int passengerCount = PassengerManager.Instance.activePassengers.Count;

        if (passengerCount == 0 || totalLitter == 0) return 5f;

        float maxLitterLimit = passengerCount / 4f;

        if (totalLitter >= maxLitterLimit) return 0f;
        
        return (1f - (totalLitter / maxLitterLimit)) * 5f;
    }

    float GetCrowdednessTarget()
    {
        var passengers = PassengerManager.Instance.activePassengers;
        int count = passengers.Count;

        if (count == 0) return 5f;

        int sampleSize = Mathf.Min(count, 20);
        int totalNearby = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            Passenger sampleTarget = passengers[Random.Range(0, count)];
            if (sampleTarget == null) continue;

            for (int j = 0; j < count; j++)
            {
                Passenger other = passengers[j];
                if (other != null && other != sampleTarget && (sampleTarget.transform.position - other.transform.position).sqrMagnitude <= 9f)
                {
                    totalNearby++;
                }
            }
        }

        float density = (float)totalNearby / sampleSize;

        if (density <= 2.0f) return 5f;
        if (density <= 4.5f) return 4f;
        if (density <= 7.0f) return 3f;
        if (density <= 10.0f) return 2f;
        if (density <= 14.0f) return 1f;
        return 0f;
    }

    float GetQueueTimesTarget()
    {
        int totalFacilities = FacilityManager.Instance.GetTotalFacilityCount();
        
        if (totalFacilities == 0) return 5f;

        int totalQueued = FacilityManager.Instance.GetTotalQueuedPassengers();
        float avgQueue = (float)totalQueued / totalFacilities;

        if (avgQueue <= 1.5f) return 5f;
        if (avgQueue <= 3.0f) return 4f;
        if (avgQueue <= 5.0f) return 3f;
        if (avgQueue <= 7.0f) return 2f;
        if (avgQueue <= 9.0f) return 1f;
        return 0f;
    }

    float GetStationSizeTarget()
    {
        if (ExpansionManager.Instance == null || ExpansionManager.Instance.allExpansions == null)
        {
            return 1f;
        }

        int totalExpansions = ExpansionManager.Instance.allExpansions.Length;
        if (totalExpansions == 0)
        {
            return 1f;
        }

        int builtCount = ExpansionManager.Instance.builtExpansions.Count;
        float ratio = (float)builtCount / totalExpansions;
        return 1f + (ratio * 4f);
    }

    float GetPassengerNeedsTarget()
    {
        var passengers = PassengerManager.Instance.activePassengers;
        int count = passengers.Count;

        if (count == 0) return 5f;

        int failedCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (passengers[i] != null && passengers[i].hasFailedNeed)
            {
                failedCount++;
            }
        }

        float failRatio = (float)failedCount / count;

        // If 50% or more of the station has a failed need, rating drops to 0.
        return Mathf.Clamp((1f - (failRatio / 0.5f)) * 5f, 0f, 5f);
    }

    public static float GetDecorationScoreAtPosition(Vector3 position, PlacedBuildable[] placedBuildables)
    {
        if (placedBuildables == null || placedBuildables.Length == 0)
        {
            return 0f;
        }

        float localDecorationScore = 0f;
        float sqrSampleRadius = DecorationSampleRadius * DecorationSampleRadius;

        for (int i = 0; i < placedBuildables.Length; i++)
        {
            PlacedBuildable buildable = placedBuildables[i];
            if (buildable == null || !buildable.HasDecoration)
            {
                continue;
            }

            Vector3 offset = buildable.transform.position - position;
            offset.y = 0f;

            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > sqrSampleRadius)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float falloff = 1f - (distance / DecorationSampleRadius);
            localDecorationScore += buildable.decorationStrength * falloff;
        }

        return Mathf.Clamp(localDecorationScore, 0f, MaxDecorationScorePerPassenger);
    }

    float GetDecorationTarget()
    {
        var passengers = PassengerManager.Instance.activePassengers;
        int count = passengers.Count;

        if (count == 0) return 5f;

        PlacedBuildable[] placedBuildables = FindObjectsByType<PlacedBuildable>(FindObjectsSortMode.None);
        if (placedBuildables.Length == 0) return 0f;

        bool foundDecor = false;
        for (int i = 0; i < placedBuildables.Length; i++)
        {
            if (placedBuildables[i] != null && placedBuildables[i].HasDecoration)
            {
                foundDecor = true;
                break;
            }
        }

        if (!foundDecor)
        {
            return 0f;
        }

        int sampleSize = Mathf.Min(count, 20);
        int validSampleCount = 0;
        float totalDecorationScore = 0f;

        for (int i = 0; i < sampleSize; i++)
        {
            Passenger passenger = passengers[Random.Range(0, count)];
            if (passenger == null) continue;

            validSampleCount++;
            totalDecorationScore += GetDecorationScoreAtPosition(passenger.transform.position, placedBuildables);
        }

        if (validSampleCount == 0) return 5f;

        return totalDecorationScore / validSampleCount;
    }
}
