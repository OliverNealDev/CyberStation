using NUnit.Framework;

public class StationClockMathTests
{
    [Test]
    public void FirstDayStartsAfterTheMorningRush()
    {
        Assert.AreEqual(1, StationClockMath.GetDay(0f));
        Assert.AreEqual("10:00", StationClockMath.FormatTime(StationClockMath.GetHourOfDay(0f)));
        Assert.AreEqual(DayPhase.Daytime, StationClockMath.GetPhase(StationClockMath.GetHourOfDay(0f)));
    }

    [Test]
    public void DayRollsOverAtMidnight()
    {
        float secondsToMidnight = StationClockMath.GetSecondsUntilHour(0f, 0f);

        Assert.AreEqual(14f * StationClockMath.SecondsPerGameHour, secondsToMidnight, 0.0001f);
        Assert.AreEqual(1, StationClockMath.GetDay(secondsToMidnight - 0.01f));
        Assert.AreEqual(2, StationClockMath.GetDay(secondsToMidnight));
    }

    [TestCase(3f, DayPhase.Night)]
    [TestCase(6f, DayPhase.EarlyMorning)]
    [TestCase(8f, DayPhase.MorningRush)]
    [TestCase(12f, DayPhase.Daytime)]
    [TestCase(17.5f, DayPhase.EveningRush)]
    [TestCase(20f, DayPhase.Evening)]
    [TestCase(23f, DayPhase.LateNight)]
    [TestCase(24f, DayPhase.Night)]
    public void PhaseBoundaries(float hour, DayPhase expected)
    {
        Assert.AreEqual(expected, StationClockMath.GetPhase(hour));
    }

    [Test]
    public void DemandAveragesCloseToOneAcrossTheDay()
    {
        // The cycle should move demand around the day, not quietly change how much
        // money a station makes in total.
        const int samplesPerHour = 60;
        float total = 0f;
        int samples = 0;

        for (int i = 0; i < 24 * samplesPerHour; i++)
        {
            float hour = i / (float)samplesPerHour;
            total += StationClockMath.GetDemandMultiplier(StationClockMath.GetPhase(hour));
            samples++;
        }

        float average = total / samples;
        Assert.That(average, Is.InRange(0.9f, 1.05f));
    }

    [Test]
    public void RushHoursAreTheOnlyPhasesAboveBaseline()
    {
        foreach (DayPhase phase in System.Enum.GetValues(typeof(DayPhase)))
        {
            bool aboveBaseline = StationClockMath.GetDemandMultiplier(phase) > 1f;
            Assert.AreEqual(StationClockMath.IsRushHour(phase), aboveBaseline, phase.ToString());
        }
    }

    [Test]
    public void TimeIsShownInTenMinuteSteps()
    {
        Assert.AreEqual("08:10", StationClockMath.FormatTime(8f + (19f / 60f)));
        Assert.AreEqual("23:50", StationClockMath.FormatTime(23.99f));
    }
}
