using System.Collections.Generic;
using UnityEngine;

public class RatingManager : MonoBehaviour
{
    private const string BuildableFloorTag = "BuildableFlooring";
    private const float PassengerFloorRaycastHeight = 32f;
    private const float PassengerFloorRaycastDistance = 64f;
    private const float PassengerFloorTolerance = 0.5f;
    private const float DecorationPresenceThreshold = 0.15f;
    private const float DecorationCoverageTarget = 0.6f;
    private const float DecorationIntensityWeight = 0.7f;
    private const float DecorationCoverageWeight = 0.3f;
    private const float DecorationFiveStarStrengthMultiplier = 0.4f;
    private const float DecorationMinFiveStarScore = 1.25f;
    private const float DecorationMaxFiveStarScore = 2.5f;

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
    private int groundLayerMask;
    private readonly List<Passenger> floorRatingPassengers = new();
    private readonly RaycastHit[] passengerFloorHits = new RaycastHit[32];

    void Awake()
    {
        Instance = this;

        int groundLayer = LayerMask.NameToLayer("groundLayer");
        groundLayerMask = groundLayer >= 0 ? 1 << groundLayer : 0;
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
        List<Passenger> floorEligiblePassengers = GetFloorEligiblePassengers();
        float targetCleanliness = GetCleanlinessTarget();
        float targetCrowdedness = GetCrowdednessTarget(floorEligiblePassengers);
        float targetQueueTimes = GetQueueTimesTarget();
        float targetStationSize = GetStationSizeTarget();
        float targetPassengerNeeds = GetPassengerNeedsTarget();
        float targetDecoration = GetDecorationTarget(floorEligiblePassengers);

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

    float GetCrowdednessTarget(List<Passenger> passengers)
    {
        int count = passengers.Count;

        if (count == 0) return 5f;

        int sampleSize = Mathf.Min(count, 20);
        int totalNearby = 0;
        int validSampleCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            Passenger sampleTarget = passengers[Random.Range(0, count)];
            if (sampleTarget == null) continue;
            validSampleCount++;

            for (int j = 0; j < count; j++)
            {
                Passenger other = passengers[j];
                if (other != null && other != sampleTarget && (sampleTarget.transform.position - other.transform.position).sqrMagnitude <= 9f)
                {
                    totalNearby++;
                }
            }
        }

        if (validSampleCount == 0) return 5f;

        float density = (float)totalNearby / validSampleCount;

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

    List<Passenger> GetFloorEligiblePassengers()
    {
        floorRatingPassengers.Clear();

        var passengers = PassengerManager.Instance.activePassengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            Passenger passenger = passengers[i];
            if (IsPassengerOnOrAboveBuildableFloor(passenger))
            {
                floorRatingPassengers.Add(passenger);
            }
        }

        return floorRatingPassengers;
    }

    bool IsPassengerOnOrAboveBuildableFloor(Passenger passenger)
    {
        if (passenger == null)
        {
            return false;
        }

        return TryGetSupportingBuildableFloorHeight(passenger.transform.position, out _);
    }

    bool TryGetSupportingBuildableFloorHeight(Vector3 position, out float floorHeight)
    {
        floorHeight = 0f;

        if (groundLayerMask == 0)
        {
            return false;
        }

        Vector3 rayStart = position + (Vector3.up * PassengerFloorRaycastHeight);
        int hitCount = Physics.RaycastNonAlloc(
            rayStart,
            Vector3.down,
            passengerFloorHits,
            PassengerFloorRaycastDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return false;
        }

        float highestSupportedFloor = float.MinValue;
        float maxSupportedHeight = position.y + PassengerFloorTolerance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = passengerFloorHits[i];
            if (hit.collider == null || hit.collider.isTrigger || !hit.collider.CompareTag(BuildableFloorTag))
            {
                continue;
            }

            float hitHeight = hit.point.y;
            if (hitHeight > maxSupportedHeight || hitHeight <= highestSupportedFloor)
            {
                continue;
            }

            highestSupportedFloor = hitHeight;
            floorHeight = hitHeight;
        }

        return highestSupportedFloor > float.MinValue;
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

    float GetDecorationTarget(List<Passenger> passengers)
    {
        int count = passengers.Count;

        if (count == 0) return 5f;

        PlacedBuildable[] placedBuildables = FindObjectsByType<PlacedBuildable>(FindObjectsSortMode.None);
        if (placedBuildables.Length == 0) return 0f;

        int decorativeBuildableCount = 0;
        float totalPlacedDecorationStrength = 0f;
        for (int i = 0; i < placedBuildables.Length; i++)
        {
            PlacedBuildable buildable = placedBuildables[i];
            if (buildable != null && buildable.HasDecoration)
            {
                decorativeBuildableCount++;
                totalPlacedDecorationStrength += buildable.decorationStrength;
            }
        }

        if (decorativeBuildableCount == 0)
        {
            return 0f;
        }

        float totalDecorationScore = 0f;
        int decoratedPassengerCount = 0;

        for (int i = 0; i < count; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger == null) continue;

            float localDecorationScore = GetDecorationScoreAtPosition(passenger.transform.position, placedBuildables);
            totalDecorationScore += localDecorationScore;

            if (localDecorationScore >= DecorationPresenceThreshold)
            {
                decoratedPassengerCount++;
            }
        }

        float averageDecorationScore = totalDecorationScore / count;
        float averageDecorationStrength = totalPlacedDecorationStrength / decorativeBuildableCount;
        float fiveStarScore = Mathf.Clamp(
            averageDecorationStrength * DecorationFiveStarStrengthMultiplier,
            DecorationMinFiveStarScore,
            DecorationMaxFiveStarScore);

        float normalizedIntensity = Mathf.Clamp01(averageDecorationScore / fiveStarScore);
        float normalizedCoverage = Mathf.Clamp01(((float)decoratedPassengerCount / count) / DecorationCoverageTarget);
        float normalizedDecorationRating =
            (normalizedIntensity * DecorationIntensityWeight) +
            (normalizedCoverage * DecorationCoverageWeight);

        return normalizedDecorationRating * 5f;
    }
}
