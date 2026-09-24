using NUnit.Framework;

public class DayGraderTests
{
    private static DayStats MakeDay(float averageRating, int needsMet, int needsFailed, int income, int expenses, int arrivals = 50)
    {
        DayStats stats = new DayStats
        {
            passengersArrived = arrivals,
            needsMet = needsMet,
            needsFailed = needsFailed,
            income = income,
            expenses = expenses
        };

        stats.RecordRatingSample(averageRating, false);
        return stats;
    }

    [Test]
    public void EmptyDayHasNoGrade()
    {
        DayStats stats = MakeDay(5f, 0, 0, 0, 0, arrivals: 0);
        Assert.AreEqual(StationGrade.None, DayGrader.GetGrade(stats));
    }

    [Test]
    public void GreatProfitableDayIsAnS()
    {
        Assert.AreEqual(StationGrade.S, DayGrader.GetGrade(MakeDay(4.8f, 100, 2, 3000, 1000)));
    }

    [Test]
    public void FailedNeedsCostGrades()
    {
        // Same rating, but a third of needs went unmet: that is a very different day.
        StationGrade served = DayGrader.GetGrade(MakeDay(4.2f, 90, 10, 2000, 1000));
        StationGrade neglected = DayGrader.GetGrade(MakeDay(4.2f, 60, 40, 2000, 1000));

        Assert.AreEqual(StationGrade.A, served);
        Assert.Less(neglected, served);
    }

    [Test]
    public void LossMakingDayDropsBelowAnOtherwiseIdenticalProfitableDay()
    {
        float profitable = DayGrader.GetScore(4.2f, 1f, 100);
        float lossMaking = DayGrader.GetScore(4.2f, 1f, -100);

        Assert.Greater(profitable, lossMaking);
    }

    [Test]
    public void SubsidyScalesWithGradeAndTier()
    {
        Assert.AreEqual(0, DayGrader.GetSubsidy(StationGrade.D, 1));
        Assert.Less(DayGrader.GetSubsidy(StationGrade.B, 1), DayGrader.GetSubsidy(StationGrade.A, 1));
        Assert.Less(DayGrader.GetSubsidy(StationGrade.A, 1), DayGrader.GetSubsidy(StationGrade.A, 5));
        Assert.AreEqual(0, DayGrader.GetSubsidy(StationGrade.S, 3) % 5);
    }

    [Test]
    public void GradeComparisonIgnoresUngradedDays()
    {
        Assert.IsTrue(DayGrader.IsAtLeast(StationGrade.A, StationGrade.B));
        Assert.IsFalse(DayGrader.IsAtLeast(StationGrade.C, StationGrade.B));
        Assert.IsFalse(DayGrader.IsAtLeast(StationGrade.None, StationGrade.D));
    }

    [Test]
    public void RushAverageOnlyCountsRushSamples()
    {
        DayStats stats = new DayStats();
        stats.RecordRatingSample(5f, false);
        stats.RecordRatingSample(2f, true);
        stats.RecordRatingSample(3f, true);

        Assert.AreEqual(2.5f, stats.RushAverageRating, 0.0001f);
        Assert.AreEqual(10f / 3f, stats.AverageRating, 0.0001f);
        Assert.AreEqual(2f, stats.lowestRating, 0.0001f);
    }

    [Test]
    public void TopComplaintsAreRankedAndTrimmed()
    {
        DayStats stats = new DayStats();
        stats.RecordComplaint(ComplaintType.NoDrink);
        stats.RecordComplaint(ComplaintType.TicketQueue);
        stats.RecordComplaint(ComplaintType.TicketQueue);
        stats.RecordComplaint(ComplaintType.TicketQueue);
        stats.RecordComplaint(ComplaintType.NoFood);
        stats.RecordComplaint(ComplaintType.NoFood);

        var top = stats.GetTopComplaints(2);

        Assert.AreEqual(2, top.Count);
        Assert.AreEqual(ComplaintType.TicketQueue, top[0].Key);
        Assert.AreEqual(3, top[0].Value);
        Assert.AreEqual(ComplaintType.NoFood, top[1].Key);
    }

    [Test]
    public void ComplaintsSurviveAnArrayFromAnOlderSave()
    {
        DayStats stats = new DayStats { complaints = new int[2] };

        Assert.DoesNotThrow(() => stats.RecordComplaint(ComplaintType.ServiceEnded));
        Assert.AreEqual(1, stats.complaints[(int)ComplaintType.ServiceEnded]);
    }

    [Test]
    public void OnlyOperatingMoneyCountsTowardsIncomeAndProfit()
    {
        DayStats stats = new DayStats();
        stats.RecordMoney(300, true);
        stats.RecordMoney(-120, true);
        stats.RecordMoney(-30, true);

        // Building and refunding the same object must not look like a good day.
        stats.RecordMoney(-500, false);
        stats.RecordMoney(500, false);

        Assert.AreEqual(300, stats.income);
        Assert.AreEqual(150, stats.expenses);
        Assert.AreEqual(150, stats.Net);
        Assert.AreEqual(500, stats.investment);
    }

    [Test]
    public void TicketSalesCountTowardsService()
    {
        // Failed ticket attempts count against service, so successful ones must count for it.
        DayStats stats = new DayStats { ticketsSold = 150, needsMet = 30, needsFailed = 10 };

        Assert.AreEqual(180f / 190f, stats.ServiceRatio, 0.0001f);
    }
}
