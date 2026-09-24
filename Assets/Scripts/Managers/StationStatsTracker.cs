using System;
using System.Collections.Generic;
using UnityEngine;

// Listens to the simulation and keeps score: today's numbers for the end-of-day report,
// and lifetime counters that directives measure against. It never changes the simulation.
public class StationStatsTracker : MonoBehaviour, ISaveParticipant
{
    public static StationStatsTracker Instance { get; private set; }

    public static event Action<DayStats> OnDayReportReady;
    public static event Action<float> OnRushHourHeld;

    private const float RatingSampleInterval = 1f;
    private const float RushHourHoldThreshold = 3.5f;
    private const int MinimumRushArrivals = 8;

    [Serializable]
    public class LifetimeStats
    {
        public int ticketsSold;
        public int passengersBoarded;
        public int needsMet;
        public int evadersCaught;
        public int daysGradedB;
        public int daysGradedA;
        public int rushHoursHeld;
        public int inspectionsPassed;
        public int bestDayIncome;
        public StationGrade bestGrade;
    }

    [Serializable]
    private class StatsSaveData
    {
        public DayStats today;
        public DayStats lastDay;
        public LifetimeStats lifetime;
        public float rushSampleTotal;
        public int rushSampleCount;
        public int rushArrivals;
    }

    private DayStats today = new DayStats { day = 1 };
    private DayStats lastDay;
    private LifetimeStats lifetime = new LifetimeStats();
    private float ratingSampleTimer;

    private float rushSampleTotal;
    private int rushSampleCount;
    private int rushArrivals;

    public DayStats Today => today;
    public DayStats LastDay => lastDay;
    public LifetimeStats Lifetime => lifetime;

    public string SaveKey => "stats";

    private void Awake()
    {
        Instance = this;
        SaveManager.RegisterParticipant(this);
    }

    private void OnEnable()
    {
        PassengerManager.OnPassengerArrived += HandlePassengerArrived;
        PassengerManager.OnTicketSold += HandleTicketSold;
        PassengerManager.OnPassengerBoarded += HandlePassengerBoarded;
        PassengerManager.OnNeedMet += HandleNeedMet;
        PassengerManager.OnNeedAbandoned += HandleNeedAbandoned;
        PassengerManager.OnEvaderCaught += HandleEvaderCaught;
        PassengerManager.OnServiceEndedForPassenger += HandleServiceEnded;
        TrainController.OnTrainDeparted += HandleTrainDeparted;
        EconomyManager.OnMoneyDelta += HandleMoneyDelta;
        StationClockManager.OnDayEnded += HandleDayEnded;
        StationClockManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        PassengerManager.OnPassengerArrived -= HandlePassengerArrived;
        PassengerManager.OnTicketSold -= HandleTicketSold;
        PassengerManager.OnPassengerBoarded -= HandlePassengerBoarded;
        PassengerManager.OnNeedMet -= HandleNeedMet;
        PassengerManager.OnNeedAbandoned -= HandleNeedAbandoned;
        PassengerManager.OnEvaderCaught -= HandleEvaderCaught;
        PassengerManager.OnServiceEndedForPassenger -= HandleServiceEnded;
        TrainController.OnTrainDeparted -= HandleTrainDeparted;
        EconomyManager.OnMoneyDelta -= HandleMoneyDelta;
        StationClockManager.OnDayEnded -= HandleDayEnded;
        StationClockManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        SaveManager.UnregisterParticipant(this);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        ratingSampleTimer += Time.deltaTime;
        if (ratingSampleTimer < RatingSampleInterval)
        {
            return;
        }

        ratingSampleTimer -= RatingSampleInterval;
        SampleStation();
    }

    public void RecordInspectionPassed()
    {
        lifetime.inspectionsPassed++;
    }

    private void SampleStation()
    {
        if (PassengerManager.Instance == null || RatingManager.Instance == null)
        {
            return;
        }

        int passengerCount = PassengerManager.Instance.activePassengers.Count;
        today.peakPassengers = Mathf.Max(today.peakPassengers, passengerCount);

        // An empty concourse is trivially five stars, so it would flatter the average.
        if (passengerCount == 0)
        {
            return;
        }

        bool isRushHour = StationClockManager.Instance != null && StationClockManager.Instance.IsRushHour;
        float rating = RatingManager.Instance.stationRating;
        today.RecordRatingSample(rating, isRushHour);

        if (isRushHour)
        {
            rushSampleTotal += rating;
            rushSampleCount++;
        }
    }

    private void HandlePassengerArrived(Passenger passenger)
    {
        today.passengersArrived++;
        if (StationClockManager.Instance != null && StationClockManager.Instance.IsRushHour)
        {
            rushArrivals++;
        }
    }

    private void HandleTicketSold(Passenger passenger, int fare)
    {
        today.ticketsSold++;
        lifetime.ticketsSold++;
    }

    private void HandlePassengerBoarded(Passenger passenger)
    {
        today.passengersBoarded++;
        lifetime.passengersBoarded++;
    }

    private void HandleNeedMet(Passenger passenger, Passenger.NeedType needType)
    {
        today.needsMet++;
        lifetime.needsMet++;
    }

    private void HandleNeedAbandoned(Passenger passenger, Passenger.NeedType needType)
    {
        today.needsFailed++;
        today.RecordComplaint(GetComplaint(needType));
    }

    private void HandleEvaderCaught(Passenger passenger)
    {
        today.evadersCaught++;
        lifetime.evadersCaught++;
    }

    private void HandleServiceEnded(Passenger passenger)
    {
        today.RecordComplaint(ComplaintType.ServiceEnded);
    }

    private void HandleTrainDeparted(TrainController train)
    {
        today.trainsDeparted++;
    }

    private void HandleMoneyDelta(int delta, bool isOperating)
    {
        today.RecordMoney(delta, isOperating);
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        if (StationClockMath.IsRushHour(phase))
        {
            rushSampleTotal = 0f;
            rushSampleCount = 0;
            rushArrivals = 0;
            return;
        }

        if (rushSampleCount == 0 || rushArrivals < MinimumRushArrivals)
        {
            return;
        }

        float rushAverage = rushSampleTotal / rushSampleCount;
        rushSampleTotal = 0f;
        rushSampleCount = 0;
        rushArrivals = 0;

        if (rushAverage >= RushHourHoldThreshold)
        {
            lifetime.rushHoursHeld++;
            OnRushHourHeld?.Invoke(rushAverage);
        }
    }

    private void HandleDayEnded(int finishedDay)
    {
        DayStats finished = today;
        finished.day = finishedDay;
        finished.grade = DayGrader.GetGrade(finished);
        FillWeakestCategory(finished);

        int tier = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 1;
        finished.subsidy = DayGrader.GetSubsidy(finished.grade, tier);

        if (DayGrader.IsAtLeast(finished.grade, StationGrade.B)) lifetime.daysGradedB++;
        if (DayGrader.IsAtLeast(finished.grade, StationGrade.A)) lifetime.daysGradedA++;
        if (finished.grade > lifetime.bestGrade) lifetime.bestGrade = finished.grade;
        lifetime.bestDayIncome = Mathf.Max(lifetime.bestDayIncome, finished.income);

        lastDay = finished;
        today = new DayStats { day = finishedDay + 1 };

        // Paid after the swap, so the subsidy counts towards the new day rather than
        // inflating the day it was earned for.
        if (finished.subsidy > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(finished.subsidy, false);
        }

        OnDayReportReady?.Invoke(finished);
    }

    private static void FillWeakestCategory(DayStats stats)
    {
        RatingManager ratings = RatingManager.Instance;
        if (ratings == null)
        {
            return;
        }

        List<KeyValuePair<string, float>> categories = new List<KeyValuePair<string, float>>
        {
            new KeyValuePair<string, float>("Cleanliness", ratings.cleanlinessRating),
            new KeyValuePair<string, float>("Crowdedness", ratings.crowdednessRating),
            new KeyValuePair<string, float>("Queue Lengths", ratings.queueTimesRating),
            new KeyValuePair<string, float>("Service", ratings.passengerNeedsRating),
            new KeyValuePair<string, float>("Decoration", ratings.decorationRating),
            new KeyValuePair<string, float>("Choice", ratings.choiceRating)
        };

        categories.Sort((left, right) => left.Value.CompareTo(right.Value));
        stats.weakestCategory = categories[0].Key;
        stats.weakestCategoryValue = categories[0].Value;
    }

    public static string GetCategoryHint(string category)
    {
        switch (category)
        {
            case "Cleanliness": return "Hire Janitors, and keep bins of litter-heavy machines close together.";
            case "Crowdedness": return "Spread facilities out so crowds don't pile into one corridor.";
            case "Queue Lengths": return "Add another machine of whichever type has the longest queue.";
            case "Service": return "Passengers are giving up on needs. Build what they are asking for.";
            case "Decoration": return "Put lights, globes and billboards where passengers wait.";
            case "Choice": return "Build every unlocked facility type for each need.";
            default: return string.Empty;
        }
    }

    private static ComplaintType GetComplaint(Passenger.NeedType needType)
    {
        bool hasFacility = HasFacilityForNeed(needType);
        switch (needType)
        {
            case Passenger.NeedType.Ticket: return hasFacility ? ComplaintType.TicketQueue : ComplaintType.NoTicketMachine;
            case Passenger.NeedType.Hunger: return hasFacility ? ComplaintType.FoodQueue : ComplaintType.NoFood;
            case Passenger.NeedType.Thirst: return hasFacility ? ComplaintType.DrinkQueue : ComplaintType.NoDrink;
            case Passenger.NeedType.Energy: return hasFacility ? ComplaintType.EnergyQueue : ComplaintType.NoEnergy;
            default: return hasFacility ? ComplaintType.HygieneQueue : ComplaintType.NoHygiene;
        }
    }

    private static bool HasFacilityForNeed(Passenger.NeedType needType)
    {
        if (FacilityManager.Instance == null)
        {
            return false;
        }

        List<FacilityType> facilityTypes = FacilityManager.Instance.GetFacilitiesForNeed(needType);
        for (int i = 0; i < facilityTypes.Count; i++)
        {
            if (FacilityManager.Instance.HasFacility(facilityTypes[i]))
            {
                return true;
            }
        }

        return false;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new StatsSaveData
        {
            today = today,
            lastDay = lastDay,
            lifetime = lifetime,
            rushSampleTotal = rushSampleTotal,
            rushSampleCount = rushSampleCount,
            rushArrivals = rushArrivals
        });
    }

    public void RestoreState(string json)
    {
        StatsSaveData data = JsonUtility.FromJson<StatsSaveData>(json);
        if (data == null)
        {
            return;
        }

        today = data.today ?? new DayStats();
        lastDay = data.lastDay != null && data.lastDay.day > 0 ? data.lastDay : null;
        lifetime = data.lifetime ?? new LifetimeStats();
        rushSampleTotal = data.rushSampleTotal;
        rushSampleCount = data.rushSampleCount;
        rushArrivals = data.rushArrivals;

        if (StationClockManager.Instance != null)
        {
            today.day = StationClockManager.Instance.CurrentDay;
        }
    }

    public void OnNoSavedState(int loadedSaveVersion)
    {
        today = new DayStats { day = StationClockManager.Instance != null ? StationClockManager.Instance.CurrentDay : 1 };
    }
}
