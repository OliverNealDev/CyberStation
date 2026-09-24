using System;
using System.Collections.Generic;
using UnityEngine;

public enum StationGrade
{
    None,
    D,
    C,
    B,
    A,
    S
}

public enum ComplaintType
{
    NoTicketMachine,
    TicketQueue,
    NoFood,
    FoodQueue,
    NoDrink,
    DrinkQueue,
    NoEnergy,
    EnergyQueue,
    NoHygiene,
    HygieneQueue,
    ServiceEnded
}

// One day of station history. Serializable so the running day survives a save and the
// previous day's report can be reopened after a reload.
[Serializable]
public class DayStats
{
    public int day;
    public int passengersArrived;
    public int passengersBoarded;
    public int ticketsSold;
    public int needsMet;
    public int needsFailed;
    public int evadersCaught;
    public int trainsDeparted;
    // Operating money only: fares, facility sales, fines, wages and running costs.
    // Spending on the station is tracked apart, and refunds, rewards and subsidies are
    // left out, so a day's grade and its income goals reflect running the station.
    public int income;
    public int expenses;
    public int investment;
    public int peakPassengers;
    public int ratingSampleCount;
    public float ratingSampleTotal;
    public float lowestRating = 5f;
    public int rushRatingSampleCount;
    public float rushRatingSampleTotal;
    public int[] complaints = new int[Enum.GetValues(typeof(ComplaintType)).Length];

    // Worst-performing rating category at day end, filled in by the report.
    public string weakestCategory;
    public float weakestCategoryValue;
    public StationGrade grade;
    public int subsidy;

    public int Net => income - expenses;

    public float AverageRating => ratingSampleCount > 0 ? ratingSampleTotal / ratingSampleCount : 0f;

    public float RushAverageRating => rushRatingSampleCount > 0 ? rushRatingSampleTotal / rushRatingSampleCount : AverageRating;

    public float ServiceRatio
    {
        get
        {
            // A ticket is a need too: failed ticket attempts count against service, so
            // successful ones have to count for it.
            int served = needsMet + ticketsSold;
            int attempts = served + needsFailed;
            return attempts > 0 ? (float)served / attempts : 1f;
        }
    }

    public bool HadService => passengersArrived >= DayGrader.MinimumPassengersForGrade;

    public void RecordRatingSample(float rating, bool isRushHour)
    {
        ratingSampleTotal += rating;
        ratingSampleCount++;
        lowestRating = Mathf.Min(lowestRating, rating);

        if (isRushHour)
        {
            rushRatingSampleTotal += rating;
            rushRatingSampleCount++;
        }
    }

    public void RecordMoney(int delta, bool isOperating)
    {
        if (isOperating)
        {
            if (delta > 0) income += delta;
            else expenses -= delta;
        }
        else if (delta < 0)
        {
            investment -= delta;
        }
    }

    public void RecordComplaint(ComplaintType complaint)
    {
        EnsureComplaintArray();
        complaints[(int)complaint]++;
    }

    public List<KeyValuePair<ComplaintType, int>> GetTopComplaints(int count)
    {
        EnsureComplaintArray();

        List<KeyValuePair<ComplaintType, int>> ranked = new List<KeyValuePair<ComplaintType, int>>();
        for (int i = 0; i < complaints.Length; i++)
        {
            if (complaints[i] > 0)
            {
                ranked.Add(new KeyValuePair<ComplaintType, int>((ComplaintType)i, complaints[i]));
            }
        }

        ranked.Sort((left, right) => right.Value.CompareTo(left.Value));
        if (ranked.Count > count)
        {
            ranked.RemoveRange(count, ranked.Count - count);
        }

        return ranked;
    }

    // JsonUtility drops arrays it did not write, and an enum added later would otherwise
    // index past the end of an array loaded from an older save.
    private void EnsureComplaintArray()
    {
        int complaintTypeCount = Enum.GetValues(typeof(ComplaintType)).Length;
        if (complaints == null)
        {
            complaints = new int[complaintTypeCount];
        }
        else if (complaints.Length < complaintTypeCount)
        {
            Array.Resize(ref complaints, complaintTypeCount);
        }
    }
}

public static class DayGrader
{
    public const int MinimumPassengersForGrade = 5;
    private const float ServiceRatioTarget = 0.9f;
    private const float ServicePenaltyPerMissingRatio = 5f;
    private const float LossPenalty = 0.5f;

    public static float GetScore(float averageRating, float serviceRatio, int net)
    {
        float score = Mathf.Clamp(averageRating, 0f, 5f);

        if (serviceRatio < ServiceRatioTarget)
        {
            score -= (ServiceRatioTarget - Mathf.Clamp01(serviceRatio)) * ServicePenaltyPerMissingRatio;
        }

        if (net < 0)
        {
            score -= LossPenalty;
        }

        return Mathf.Max(0f, score);
    }

    public static StationGrade GetGrade(DayStats stats)
    {
        if (stats == null || !stats.HadService)
        {
            return StationGrade.None;
        }

        return GetGrade(GetScore(stats.AverageRating, stats.ServiceRatio, stats.Net));
    }

    public static StationGrade GetGrade(float score)
    {
        if (score >= 4.6f) return StationGrade.S;
        if (score >= 4f) return StationGrade.A;
        if (score >= 3.3f) return StationGrade.B;
        if (score >= 2.5f) return StationGrade.C;
        return StationGrade.D;
    }

    // The subsidy grows with tier so a good day stays worth something against late-game costs.
    public static int GetSubsidy(StationGrade grade, int tier)
    {
        int baseAmount;
        switch (grade)
        {
            case StationGrade.S: baseAmount = 600; break;
            case StationGrade.A: baseAmount = 400; break;
            case StationGrade.B: baseAmount = 250; break;
            case StationGrade.C: baseAmount = 100; break;
            default: return 0;
        }

        float tierScale = 1f + (0.25f * (Mathf.Max(1, tier) - 1));
        return Mathf.RoundToInt(baseAmount * tierScale / 5f) * 5;
    }

    public static bool IsAtLeast(StationGrade grade, StationGrade minimum)
    {
        return grade != StationGrade.None && grade >= minimum;
    }

    public static string GetLabel(StationGrade grade)
    {
        return grade == StationGrade.None ? "-" : grade.ToString();
    }

    public static string GetComplaintText(ComplaintType complaint)
    {
        switch (complaint)
        {
            case ComplaintType.NoTicketMachine: return "Couldn't find a ticket machine";
            case ComplaintType.TicketQueue: return "Ticket queues were too long";
            case ComplaintType.NoFood: return "Nothing to eat";
            case ComplaintType.FoodQueue: return "Food queues were too long";
            case ComplaintType.NoDrink: return "Nothing to drink";
            case ComplaintType.DrinkQueue: return "Drink queues were too long";
            case ComplaintType.NoEnergy: return "Nowhere to get energy";
            case ComplaintType.EnergyQueue: return "Energy queues were too long";
            case ComplaintType.NoHygiene: return "Nowhere to freshen up";
            case ComplaintType.HygieneQueue: return "Hygiene queues were too long";
            case ComplaintType.ServiceEnded: return "Their train line was cancelled";
            default: return complaint.ToString();
        }
    }

    public static string GetComplaintHint(ComplaintType complaint)
    {
        switch (complaint)
        {
            case ComplaintType.NoTicketMachine: return "Build a Ticket Machine near the Materializer.";
            case ComplaintType.TicketQueue: return "Add another Ticket Machine, away from the first.";
            case ComplaintType.NoFood: return "Build a Nutrient Dispenser or Snack Printer.";
            case ComplaintType.FoodQueue: return "Add a second food machine to split the queue.";
            case ComplaintType.NoDrink: return "Build a Hydrating Obelisk or Bottle Dispenser.";
            case ComplaintType.DrinkQueue: return "Add another drinks machine.";
            case ComplaintType.NoEnergy: return "Build an Energy Bottle Dispenser.";
            case ComplaintType.EnergyQueue: return "Add another energy dispenser.";
            case ComplaintType.NoHygiene: return "Build a Private Lavatory or Cleansing Shower.";
            case ComplaintType.HygieneQueue: return "Add more hygiene facilities.";
            case ComplaintType.ServiceEnded: return "Ending a line strands everyone waiting for it.";
            default: return string.Empty;
        }
    }
}
