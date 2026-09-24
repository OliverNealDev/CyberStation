using NUnit.Framework;

public class StationEventTests
{
    [TearDown]
    public void TearDown()
    {
        StationModifiers.ResetAll();
    }

    [Test]
    public void NoEventsWithoutATrainLine()
    {
        Assert.IsEmpty(StationEventScheduler.GetEligible(5, 0, null));
    }

    [Test]
    public void EligibilityRespectsTier()
    {
        foreach (StationEventDefinition definition in StationEventScheduler.GetEligible(1, 1, null))
        {
            Assert.LessOrEqual(definition.minTier, 1, definition.type.ToString());
        }

        Assert.Greater(
            StationEventScheduler.GetEligible(5, 1, null).Count,
            StationEventScheduler.GetEligible(1, 1, null).Count);
    }

    [Test]
    public void TheSameEventNeverRunsTwiceInARow()
    {
        foreach (StationEventDefinition definition in StationEventScheduler.GetEligible(5, 2, StationEventType.Heatwave))
        {
            Assert.AreNotEqual(StationEventType.Heatwave, definition.type);
        }
    }

    [Test]
    public void WeightedPickCoversTheWholeRange()
    {
        var candidates = StationEventScheduler.GetEligible(5, 2, null);

        Assert.AreSame(candidates[0], StationEventScheduler.PickWeighted(candidates, 0f));
        Assert.AreSame(candidates[candidates.Count - 1], StationEventScheduler.PickWeighted(candidates, 1f));
        Assert.IsNull(StationEventScheduler.PickWeighted(null, 0.5f));
    }

    [Test]
    public void GapBetweenEventsStaysInsideItsBounds()
    {
        Assert.AreEqual(StationEventScheduler.MinimumGapSeconds, StationEventScheduler.GetNextEventDelay(-1f), 0.0001f);
        Assert.AreEqual(StationEventScheduler.MaximumGapSeconds, StationEventScheduler.GetNextEventDelay(2f), 0.0001f);
    }

    [Test]
    public void ApplyingAnEventReplacesThePreviousOne()
    {
        StationModifiers.ApplyEvent(StationEventCatalogue.Find(StationEventType.CrowdSurge));
        StationModifiers.ApplyEvent(StationEventCatalogue.Find(StationEventType.Heatwave));

        Assert.AreEqual(1f, StationModifiers.EventSpawnMultiplier, 0.0001f);
        Assert.Greater(StationModifiers.ThirstBonus, 0f);
    }

    [Test]
    public void DemandAndEventMultipliersCompound()
    {
        StationModifiers.DemandMultiplier = 1.5f;
        StationModifiers.ApplyEvent(StationEventCatalogue.Find(StationEventType.CrowdSurge));

        Assert.AreEqual(1.5f * 1.8f, StationModifiers.SpawnMultiplier, 0.0001f);

        StationModifiers.ClearEventModifiers();
        Assert.AreEqual(1.5f, StationModifiers.SpawnMultiplier, 0.0001f);
    }

    [Test]
    public void EventsCannotInventANeedALineDoesNotHave()
    {
        Assert.AreEqual(0f, StationModifiers.ApplyNeedBonus(0f, 0.4f), 0.0001f);
        Assert.AreEqual(0.6f, StationModifiers.ApplyNeedBonus(0.2f, 0.4f), 0.0001f);
        Assert.AreEqual(1f, StationModifiers.ApplyNeedBonus(0.9f, 0.4f), 0.0001f);
    }

    [Test]
    public void HeldPlatformsClearWithTheEvent()
    {
        StationModifiers.HoldPlatform(2);
        Assert.IsTrue(StationModifiers.IsPlatformHeld(2));

        StationModifiers.ClearEventModifiers();
        Assert.IsFalse(StationModifiers.IsPlatformHeld(2));
    }

    [Test]
    public void InspectionsGetStricterWithTier()
    {
        Assert.Less(StationEventCatalogue.GetInspectionThreshold(2), StationEventCatalogue.GetInspectionThreshold(4));
    }
}
