using System;
using System.Collections.Generic;
using UnityEngine;

public enum StationEventType
{
    CrowdSurge,
    SignalFault,
    Inspection,
    FareDodgers,
    FoodFestival,
    Heatwave,
    Brownout,
    Influencer,
    TouristCoach
}

public sealed class StationEventDefinition
{
    public StationEventType type;
    public string title;
    public string warning;
    public string activeText;
    public bool isPositive;
    public int minTier = 1;
    public float weight = 1f;
    public float warningSeconds = 20f;
    public float durationSeconds = 60f;

    public float spawnMultiplier = 1f;
    public float evasionMultiplier = 1f;
    public float litterMultiplier = 1f;
    public float serviceTimeMultiplier = 1f;
    public float fareMultiplier = 1f;
    public float hungerBonus;
    public float thirstBonus;
    public float energyBonus;
    public float hygieneBonus;
    public bool holdsPlatform;
    public bool isInspection;
}

// Everything that bends the simulation for a while (the time of day, an event) writes here,
// and the systems that care read one multiplier. That keeps each event a row of numbers
// instead of a new branch in PassengerManager for every idea.
public static class StationModifiers
{
    public static float DemandMultiplier { get; set; } = 1f;
    public static float EventSpawnMultiplier { get; set; } = 1f;
    public static float EvasionMultiplier { get; set; } = 1f;
    public static float LitterMultiplier { get; set; } = 1f;
    public static float ServiceTimeMultiplier { get; set; } = 1f;
    public static float FareMultiplier { get; set; } = 1f;
    public static float HungerBonus { get; set; }
    public static float ThirstBonus { get; set; }
    public static float EnergyBonus { get; set; }
    public static float HygieneBonus { get; set; }

    private static readonly HashSet<int> heldPlatforms = new HashSet<int>();

    public static float SpawnMultiplier => DemandMultiplier * EventSpawnMultiplier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        DemandMultiplier = 1f;
        ClearEventModifiers();
    }

    public static void ClearEventModifiers()
    {
        EventSpawnMultiplier = 1f;
        EvasionMultiplier = 1f;
        LitterMultiplier = 1f;
        ServiceTimeMultiplier = 1f;
        FareMultiplier = 1f;
        HungerBonus = 0f;
        ThirstBonus = 0f;
        EnergyBonus = 0f;
        HygieneBonus = 0f;
        heldPlatforms.Clear();
    }

    public static void ApplyEvent(StationEventDefinition definition)
    {
        ClearEventModifiers();
        if (definition == null)
        {
            return;
        }

        EventSpawnMultiplier = definition.spawnMultiplier;
        EvasionMultiplier = definition.evasionMultiplier;
        LitterMultiplier = definition.litterMultiplier;
        ServiceTimeMultiplier = definition.serviceTimeMultiplier;
        FareMultiplier = definition.fareMultiplier;
        HungerBonus = definition.hungerBonus;
        ThirstBonus = definition.thirstBonus;
        EnergyBonus = definition.energyBonus;
        HygieneBonus = definition.hygieneBonus;
    }

    public static void HoldPlatform(int platformNumber)
    {
        heldPlatforms.Add(platformNumber);
    }

    public static bool IsPlatformHeld(int platformNumber)
    {
        return heldPlatforms.Contains(platformNumber);
    }

    public static float ApplyNeedBonus(float baseChance, float bonus)
    {
        // A need the line never generates stays off: an event can make a thirsty line
        // thirstier, but it should not invent hygiene demand on a line designed without it.
        if (baseChance <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(baseChance + bonus);
    }
}

public static class StationEventCatalogue
{
    public static readonly IReadOnlyList<StationEventDefinition> All = new List<StationEventDefinition>
    {
        new StationEventDefinition
        {
            type = StationEventType.CrowdSurge, title = "Concert Crowd",
            warning = "The NeoArena show is ending. A crowd is heading your way.",
            activeText = "Passenger arrivals up 80%.",
            minTier = 1, weight = 1.2f, durationSeconds = 60f, spawnMultiplier = 1.8f
        },
        new StationEventDefinition
        {
            type = StationEventType.Heatwave, title = "Heatwave",
            warning = "The city grid reports record heat. Expect thirsty passengers.",
            activeText = "Far more passengers arrive thirsty.",
            minTier = 1, weight = 1f, durationSeconds = 90f, thirstBonus = 0.4f
        },
        new StationEventDefinition
        {
            type = StationEventType.SignalFault, title = "Signal Fault",
            warning = "A signal fault is spreading along the line.",
            activeText = "Trains are held outside {0}.",
            minTier = 2, weight = 1f, durationSeconds = 45f, holdsPlatform = true
        },
        new StationEventDefinition
        {
            type = StationEventType.Inspection, title = "Transit Inspection",
            warning = "A Transit Authority inspector is on the way.",
            activeText = "Hold {0} stars until the inspector leaves.",
            isPositive = true, minTier = 2, weight = 1.1f, durationSeconds = 60f, isInspection = true
        },
        new StationEventDefinition
        {
            type = StationEventType.FareDodgers, title = "Fare-Dodger Crew",
            warning = "A crew of fare dodgers has been spotted nearby.",
            activeText = "Three times as many passengers skip the barriers.",
            minTier = 2, weight = 0.9f, durationSeconds = 60f, evasionMultiplier = 3f
        },
        new StationEventDefinition
        {
            type = StationEventType.FoodFestival, title = "Street Food Festival",
            warning = "A street food festival is setting up next to the station.",
            activeText = "Hungry crowds, and plenty of litter.",
            minTier = 2, weight = 0.9f, durationSeconds = 75f, hungerBonus = 0.3f, litterMultiplier = 3f
        },
        new StationEventDefinition
        {
            type = StationEventType.Brownout, title = "Brownout",
            warning = "The power grid is struggling. Machines may slow down.",
            activeText = "Every facility serves 50% slower.",
            minTier = 3, weight = 0.8f, durationSeconds = 45f, serviceTimeMultiplier = 1.5f
        },
        new StationEventDefinition
        {
            type = StationEventType.Influencer, title = "Influencer Stream",
            warning = "A famous streamer is on the way to film your concourse.",
            activeText = "Fares up 50% and extra visitors.",
            isPositive = true, minTier = 3, weight = 0.8f, durationSeconds = 60f, fareMultiplier = 1.5f, spawnMultiplier = 1.3f
        },
        new StationEventDefinition
        {
            type = StationEventType.TouristCoach, title = "Tourist Coach",
            warning = "A coach of tired tourists is pulling up outside.",
            activeText = "More visitors, needing a wash and a meal.",
            minTier = 3, weight = 0.8f, durationSeconds = 50f, spawnMultiplier = 1.5f, hygieneBonus = 0.35f, hungerBonus = 0.2f
        },
    };

    public static StationEventDefinition Find(StationEventType type)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].type == type)
            {
                return All[i];
            }
        }

        return null;
    }

    public static float GetInspectionThreshold(int tier)
    {
        return tier >= 4 ? 4.2f : tier >= 3 ? 3.8f : 3.4f;
    }
}

public static class StationEventScheduler
{
    // Roughly two or three events a day: enough that every day has a story, not so many
    // that the station spends most of its time in a crisis.
    public const float MinimumGapSeconds = 140f;
    public const float MaximumGapSeconds = 220f;

    public static float GetNextEventDelay(float roll01)
    {
        return Mathf.Lerp(MinimumGapSeconds, MaximumGapSeconds, Mathf.Clamp01(roll01));
    }

    public static List<StationEventDefinition> GetEligible(int tier, int activeLineCount, StationEventType? lastEvent)
    {
        List<StationEventDefinition> eligible = new List<StationEventDefinition>();
        if (activeLineCount <= 0)
        {
            return eligible;
        }

        for (int i = 0; i < StationEventCatalogue.All.Count; i++)
        {
            StationEventDefinition definition = StationEventCatalogue.All[i];
            if (definition.minTier > tier)
            {
                continue;
            }

            // The same event twice in a row reads as a bug, not as bad luck.
            if (lastEvent.HasValue && definition.type == lastEvent.Value)
            {
                continue;
            }

            eligible.Add(definition);
        }

        return eligible;
    }

    public static StationEventDefinition PickWeighted(IList<StationEventDefinition> candidates, float roll01)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += Mathf.Max(0f, candidates[i].weight);
        }

        if (totalWeight <= 0f)
        {
            return candidates[0];
        }

        float target = Mathf.Clamp01(roll01) * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            target -= Mathf.Max(0f, candidates[i].weight);
            if (target < 0f)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }
}
