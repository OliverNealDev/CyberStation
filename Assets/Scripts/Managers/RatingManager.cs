using UnityEngine;

public class RatingManager : MonoBehaviour
{
    public static RatingManager Instance;

    [Range(0, 5)] public int stationRating = 0;
    [Range(0, 5)] public int cleanlinessRating = 0;
    [Range(0, 5)] public int crowdednessRating = 0;
    [Range(0, 5)] public int queueTimesRating = 0;
    [Range(0, 5)] public int passengerNeedsRating = 0;
    [Range(0, 5)] public int trainSelectionRating = 0;
    [Range(0, 5)] public int stationSizeRating = 0;

    private float tickTimer;
    private float tickInterval;
    private int tickRate = 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        tickInterval = 1f / tickRate;
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval)
        {
            AdjustRatings();
            tickTimer -= tickInterval;
        }
    }

    void AdjustRatings()
    {
        AdjustCleanlinessRating();
        //AdjustCrowdednessRating();
        
        stationRating = Mathf.RoundToInt((cleanlinessRating + crowdednessRating + queueTimesRating + passengerNeedsRating + trainSelectionRating + stationSizeRating) / 6f);
    }
    
    void AdjustCleanlinessRating()
    {
        int totalLitter = JanitorCoordinator.Instance.allLitter.Count;
        int passengerCount = PassengerManager.Instance.activePassengers.Count;

        if (passengerCount == 0) return;

        if (totalLitter == 0)
        {
            cleanlinessRating = 5;
            return;
        }

        float maxLitterLimit = passengerCount / 5f;

        if (totalLitter >= maxLitterLimit)
        {
            cleanlinessRating = 0;
        }
        else
        {
            float litterRatio = totalLitter / maxLitterLimit;
            cleanlinessRating = Mathf.RoundToInt((1f - litterRatio) * 5f);
        }
    }
}