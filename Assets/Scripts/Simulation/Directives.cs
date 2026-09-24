using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum DirectiveMetric
{
    // State metrics: read fresh from the station every check.
    Materializers,
    TicketMachines,
    ThirstFacilities,
    HungerFacilities,
    FoodVariety,
    EnergyFacilities,
    HygieneFacilities,
    Decorations,
    ActiveLines,
    StationRating,
    Tier,
    Janitors,
    SecurityDrones,
    Expansions,
    TodayIncome,

    // Counter metrics: lifetime totals, measured from the moment the directive was issued.
    TicketsSold,
    PassengersBoarded,
    NeedsMet,
    EvadersCaught,
    DaysGradedB,
    DaysGradedA,
    RushHoursHeld,
    InspectionsPassed
}

public enum DirectiveHighlight
{
    None,
    Build,
    Trains,
    Platforms,
    Staff,
    Expansions,
    Ratings,
    Progression
}

public sealed class DirectiveDefinition
{
    public readonly string id;
    public readonly string title;
    public readonly string hint;
    public readonly DirectiveMetric metric;
    public readonly float target;
    public readonly int rewardMoney;
    public readonly int rewardXp;
    public readonly int minTier;
    public readonly DirectiveHighlight highlight;

    public DirectiveDefinition(
        string id,
        string title,
        string hint,
        DirectiveMetric metric,
        float target,
        int rewardMoney,
        int rewardXp = 0,
        int minTier = 1,
        DirectiveHighlight highlight = DirectiveHighlight.None)
    {
        this.id = id;
        this.title = title;
        this.hint = hint;
        this.metric = metric;
        this.target = target;
        this.rewardMoney = rewardMoney;
        this.rewardXp = rewardXp;
        this.minTier = minTier;
        this.highlight = highlight;
    }

    public bool IsCounter => DirectiveRules.IsCounterMetric(metric);
}

public static class DirectiveRules
{
    public static bool IsCounterMetric(DirectiveMetric metric)
    {
        return metric >= DirectiveMetric.TicketsSold;
    }

    // Counters start from the value at issue time, so "sell 100 tickets" means 100 more,
    // not 100 across the whole save. State metrics ignore the baseline entirely.
    public static float GetProgressValue(DirectiveDefinition definition, float currentValue, float baseline)
    {
        if (definition == null)
        {
            return 0f;
        }

        return definition.IsCounter ? Mathf.Max(0f, currentValue - baseline) : currentValue;
    }

    public static float GetProgress01(DirectiveDefinition definition, float currentValue, float baseline)
    {
        if (definition == null || definition.target <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(GetProgressValue(definition, currentValue, baseline) / definition.target);
    }

    public static bool IsComplete(DirectiveDefinition definition, float currentValue, float baseline)
    {
        // A small tolerance stops a rating that has settled at 3.999 from never paying out.
        return definition != null && GetProgressValue(definition, currentValue, baseline) >= definition.target - 0.0001f;
    }

    public static string FormatProgress(DirectiveDefinition definition, float currentValue, float baseline)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        float value = GetProgressValue(definition, currentValue, baseline);
        switch (definition.metric)
        {
            case DirectiveMetric.StationRating:
                return value.ToString("0.0") + " / " + definition.target.ToString("0.0") + " stars";
            case DirectiveMetric.TodayIncome:
                return "$" + Mathf.FloorToInt(value).ToString("N0", CultureInfo.InvariantCulture) + " / $" + Mathf.RoundToInt(definition.target).ToString("N0", CultureInfo.InvariantCulture);
            default:
                return Mathf.Min(Mathf.FloorToInt(value), Mathf.RoundToInt(definition.target)) + " / " + Mathf.RoundToInt(definition.target);
        }
    }
}

public static class DirectiveCatalogue
{
    public const int OnboardingDirectiveCount = 4;

    // Authored in play order. The first four run one at a time as the tutorial;
    // after that up to three are live at once so the player always has a choice.
    public static readonly IReadOnlyList<DirectiveDefinition> Chain = new List<DirectiveDefinition>
    {
        new DirectiveDefinition("materializer", "Open the doors",
            "Passengers arrive through a Materializer. Build one from the Build menu.",
            DirectiveMetric.Materializers, 1, 100, 20, 1, DirectiveHighlight.Build),
        new DirectiveDefinition("ticket_machine", "Start selling tickets",
            "Build a Ticket Machine. Nobody boards without a ticket.",
            DirectiveMetric.TicketMachines, 1, 100, 20, 1, DirectiveHighlight.Build),
        new DirectiveDefinition("first_line", "Run your first line",
            "Buy a train line from the Trains menu. It is assigned to a free platform for you.",
            DirectiveMetric.ActiveLines, 1, 150, 30, 1, DirectiveHighlight.Trains),
        new DirectiveDefinition("first_boardings", "Welcome aboard",
            "Get 10 passengers onto a train. Keep the ticket queue short.",
            DirectiveMetric.PassengersBoarded, 10, 100, 40),
        new DirectiveDefinition("thirst", "Quench the queue",
            "Thirsty passengers want a drink before boarding. Build a Hydrating Obelisk.",
            DirectiveMetric.ThirstFacilities, 1, 80, 20, 1, DirectiveHighlight.Build),
        new DirectiveDefinition("hunger", "Feed the commute",
            "Build a Nutrient Dispenser for hungry passengers.",
            DirectiveMetric.HungerFacilities, 1, 80, 20, 1, DirectiveHighlight.Build),
        new DirectiveDefinition("decorate", "Light it up",
            "Place 3 decorations (lights, walls or globes) where passengers wait.",
            DirectiveMetric.Decorations, 3, 100, 30, 1, DirectiveHighlight.Build),
        new DirectiveDefinition("three_stars", "Three-star station",
            "Reach 3 stars with at least 8 passengers inside. Open Ratings to see what drags it down.",
            DirectiveMetric.StationRating, 3f, 150, 40, 1, DirectiveHighlight.Ratings),
        new DirectiveDefinition("tickets_100", "Busy concourse",
            "Sell 100 tickets.",
            DirectiveMetric.TicketsSold, 100, 250, 60),
        new DirectiveDefinition("tier_2", "Climb the network",
            "Reach Tier 2. XP comes from every ticket, boarding and need you meet.",
            DirectiveMetric.Tier, 2, 200, 0, 1, DirectiveHighlight.Progression),
        new DirectiveDefinition("janitor", "Keep it clean",
            "Hire a Janitor android to sweep up litter.",
            DirectiveMetric.Janitors, 1, 150, 30, 2, DirectiveHighlight.Staff),
        new DirectiveDefinition("food_choice", "A little variety",
            "Offer two kinds of food: a Nutrient Dispenser and a Snack Printer.",
            DirectiveMetric.FoodVariety, 2, 250, 40, 2, DirectiveHighlight.Build),
        new DirectiveDefinition("two_lines", "Double service",
            "Run 2 train lines at the same time.",
            DirectiveMetric.ActiveLines, 2, 300, 50, 2, DirectiveHighlight.Trains),
        new DirectiveDefinition("grade_b", "Solid shift",
            "Finish a day with a grade of B or better.",
            DirectiveMetric.DaysGradedB, 1, 400, 60),
        new DirectiveDefinition("security", "Mind the barriers",
            "Hire a Security android to catch fare evaders.",
            DirectiveMetric.SecurityDrones, 1, 200, 30, 2, DirectiveHighlight.Staff),
        new DirectiveDefinition("four_stars", "Four-star station",
            "Reach 4 stars with at least 8 passengers inside.",
            DirectiveMetric.StationRating, 4f, 400, 80, 2, DirectiveHighlight.Ratings),
        new DirectiveDefinition("evaders", "Zero tolerance",
            "Catch 5 fare evaders.",
            DirectiveMetric.EvadersCaught, 5, 300, 50, 2),
        new DirectiveDefinition("rush_hour", "Rush hour hero",
            "Get through a rush hour averaging 3.5 stars or better.",
            DirectiveMetric.RushHoursHeld, 1, 450, 80, 2),
        new DirectiveDefinition("expansion", "More room to breathe",
            "Buy a station expansion.",
            DirectiveMetric.Expansions, 1, 500, 80, 3, DirectiveHighlight.Expansions),
        new DirectiveDefinition("inspection", "Passed with honours",
            "Pass a transit inspection.",
            DirectiveMetric.InspectionsPassed, 1, 400, 80, 2),
        new DirectiveDefinition("income_day", "Big day",
            "Take $2,500 in fares and sales in a single day.",
            DirectiveMetric.TodayIncome, 2500, 500, 80, 3),
        new DirectiveDefinition("three_lines", "Network hub",
            "Run 3 train lines at the same time.",
            DirectiveMetric.ActiveLines, 3, 600, 100, 3, DirectiveHighlight.Trains),
        new DirectiveDefinition("grade_a", "Model station",
            "Finish a day with a grade of A or better.",
            DirectiveMetric.DaysGradedA, 1, 800, 120, 3),
        new DirectiveDefinition("tier_5", "Top of the network",
            "Reach Tier 5.",
            DirectiveMetric.Tier, 5, 1500, 0, 4, DirectiveHighlight.Progression),
        new DirectiveDefinition("five_stars", "Five-star hub",
            "Reach 4.8 stars with at least 8 passengers inside.",
            DirectiveMetric.StationRating, 4.8f, 1200, 150, 4, DirectiveHighlight.Ratings),
    };

    public static DirectiveDefinition FindChainDirective(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < Chain.Count; i++)
        {
            if (Chain[i].id == id)
            {
                return Chain[i];
            }
        }

        return null;
    }

    // Once the chain is finished, contracts keep a short-term goal on screen for good.
    // They are deterministic from their serial number so a saved contract rebuilds exactly.
    public static DirectiveDefinition CreateContract(int serial, int tier)
    {
        int safeTier = Mathf.Max(1, tier);
        System.Random random = new System.Random(unchecked(serial * 7919 + 17));
        float scale = 1f + (0.35f * (safeTier - 1)) + (0.05f * Mathf.Min(serial, 20));
        string id = "contract_" + serial;

        switch (random.Next(0, 6))
        {
            case 0:
            {
                int target = RoundTo(60 * scale, 10);
                return new DirectiveDefinition(id, "Contract: ticket drive",
                    "Sell " + target + " tickets.", DirectiveMetric.TicketsSold, target, RoundTo(target * 4f, 25), RoundTo(target * 0.6f, 5));
            }
            case 1:
            {
                int target = RoundTo(50 * scale, 10);
                return new DirectiveDefinition(id, "Contract: full trains",
                    "Board " + target + " passengers.", DirectiveMetric.PassengersBoarded, target, RoundTo(target * 5f, 25), RoundTo(target * 0.6f, 5));
            }
            case 2:
            {
                int target = RoundTo(40 * scale, 5);
                return new DirectiveDefinition(id, "Contract: comfort run",
                    "Meet " + target + " passenger needs.", DirectiveMetric.NeedsMet, target, RoundTo(target * 6f, 25), RoundTo(target * 0.8f, 5));
            }
            case 3:
                return new DirectiveDefinition(id, "Contract: steady shift",
                    "Finish a day with a grade of B or better.", DirectiveMetric.DaysGradedB, 1, RoundTo(350 * scale, 25), 60);
            case 4:
                return new DirectiveDefinition(id, "Contract: peak performance",
                    "Hold 3.5 stars or better through a rush hour.", DirectiveMetric.RushHoursHeld, 1, RoundTo(400 * scale, 25), 70);
            default:
            {
                int target = RoundTo(1500 * scale, 250);
                return new DirectiveDefinition(id, "Contract: revenue target",
                    "Take $" + target.ToString("N0", CultureInfo.InvariantCulture) + " in fares and sales in one day.", DirectiveMetric.TodayIncome, target, RoundTo(target * 0.2f, 25), 60);
            }
        }
    }

    private static int RoundTo(float value, int step)
    {
        return Mathf.Max(step, Mathf.RoundToInt(value / step) * step);
    }
}
