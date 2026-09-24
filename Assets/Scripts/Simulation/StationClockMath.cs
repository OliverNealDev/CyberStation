using UnityEngine;

public enum DayPhase
{
    Night,
    EarlyMorning,
    MorningRush,
    Daytime,
    EveningRush,
    Evening,
    LateNight
}

// The station clock is pure arithmetic over one number: game seconds since the save began.
// Keeping it free of MonoBehaviour state is what lets the phase and demand curves be tested
// without a scene, and it means a save only has to store that one float.
public static class StationClockMath
{
    public const float SecondsPerGameHour = 20f;
    public const float HoursPerDay = 24f;
    public const float SecondsPerDay = SecondsPerGameHour * HoursPerDay;

    // A new station opens after the morning rush, so the first thing a player
    // meets is a calm concourse rather than a crowd they have no machines for.
    public const float FirstDayStartHour = 10f;

    private const float EarlyMorningStartHour = 5f;
    private const float MorningRushStartHour = 7f;
    private const float DaytimeStartHour = 9.5f;
    private const float EveningRushStartHour = 16f;
    private const float EveningStartHour = 19f;
    private const float LateNightStartHour = 22f;

    public static float GetAbsoluteSeconds(float elapsedGameSeconds)
    {
        return Mathf.Max(0f, elapsedGameSeconds) + (FirstDayStartHour * SecondsPerGameHour);
    }

    public static int GetDay(float elapsedGameSeconds)
    {
        return 1 + Mathf.FloorToInt(GetAbsoluteSeconds(elapsedGameSeconds) / SecondsPerDay);
    }

    public static float GetHourOfDay(float elapsedGameSeconds)
    {
        float secondsIntoDay = GetAbsoluteSeconds(elapsedGameSeconds) % SecondsPerDay;
        return secondsIntoDay / SecondsPerGameHour;
    }

    public static DayPhase GetPhase(float hourOfDay)
    {
        float hour = Mathf.Repeat(hourOfDay, HoursPerDay);

        if (hour < EarlyMorningStartHour) return DayPhase.Night;
        if (hour < MorningRushStartHour) return DayPhase.EarlyMorning;
        if (hour < DaytimeStartHour) return DayPhase.MorningRush;
        if (hour < EveningRushStartHour) return DayPhase.Daytime;
        if (hour < EveningStartHour) return DayPhase.EveningRush;
        if (hour < LateNightStartHour) return DayPhase.Evening;
        return DayPhase.LateNight;
    }

    public static bool IsRushHour(DayPhase phase)
    {
        return phase == DayPhase.MorningRush || phase == DayPhase.EveningRush;
    }

    // Averages a little under 1 across a whole day, so the day cycle reshapes demand
    // into peaks and lulls without quietly rebalancing the economy it sits on top of.
    public static float GetDemandMultiplier(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.Night: return 0.45f;
            case DayPhase.EarlyMorning: return 0.85f;
            case DayPhase.MorningRush: return 1.75f;
            case DayPhase.Daytime: return 1f;
            case DayPhase.EveningRush: return 1.6f;
            case DayPhase.Evening: return 0.9f;
            case DayPhase.LateNight: return 0.55f;
            default: return 1f;
        }
    }

    public static string GetPhaseLabel(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.Night: return "Night";
            case DayPhase.EarlyMorning: return "Early Morning";
            case DayPhase.MorningRush: return "Morning Rush";
            case DayPhase.Daytime: return "Daytime";
            case DayPhase.EveningRush: return "Evening Rush";
            case DayPhase.Evening: return "Evening";
            case DayPhase.LateNight: return "Late Night";
            default: return string.Empty;
        }
    }

    public static string FormatTime(float hourOfDay)
    {
        float hour = Mathf.Repeat(hourOfDay, HoursPerDay);
        int wholeHour = Mathf.FloorToInt(hour);
        int minute = Mathf.FloorToInt((hour - wholeHour) * 60f);

        // Snapping to ten minutes stops the clock flickering every frame at 3x speed.
        minute -= minute % 10;
        return wholeHour.ToString("00") + ":" + minute.ToString("00");
    }

    public static float GetSecondsUntilHour(float elapsedGameSeconds, float targetHour)
    {
        float currentHour = GetHourOfDay(elapsedGameSeconds);
        float hoursAhead = Mathf.Repeat(targetHour - currentHour, HoursPerDay);
        return hoursAhead * SecondsPerGameHour;
    }
}
