using System.Collections.Generic;
using NUnit.Framework;

public class DirectiveTests
{
    [Test]
    public void ChainIdsAreUnique()
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (DirectiveDefinition definition in DirectiveCatalogue.Chain)
        {
            Assert.IsTrue(ids.Add(definition.id), "Duplicate directive id " + definition.id);
        }
    }

    [Test]
    public void ChainNeverAsksForMoreThanTheTiersThatExist()
    {
        foreach (DirectiveDefinition definition in DirectiveCatalogue.Chain)
        {
            Assert.That(definition.minTier, Is.InRange(1, 5), definition.id);
            if (definition.metric == DirectiveMetric.Tier)
            {
                Assert.That(definition.target, Is.InRange(2f, 5f), definition.id);
            }
        }
    }

    [Test]
    public void OnboardingTeachesTheThreeThingsAStationNeedsToRun()
    {
        // Without a Materializer, a Ticket Machine and a line, nothing happens at all,
        // which is exactly what a new player could not work out before this chain existed.
        Assert.AreEqual(DirectiveMetric.Materializers, DirectiveCatalogue.Chain[0].metric);
        Assert.AreEqual(DirectiveMetric.TicketMachines, DirectiveCatalogue.Chain[1].metric);
        Assert.AreEqual(DirectiveMetric.ActiveLines, DirectiveCatalogue.Chain[2].metric);
    }

    [Test]
    public void CountersMeasureFromTheBaseline()
    {
        DirectiveDefinition sellTickets = new DirectiveDefinition("t", "t", "t", DirectiveMetric.TicketsSold, 10, 0);

        Assert.IsFalse(DirectiveRules.IsComplete(sellTickets, 500f, 495f));
        Assert.IsTrue(DirectiveRules.IsComplete(sellTickets, 505f, 495f));
        Assert.AreEqual(0.5f, DirectiveRules.GetProgress01(sellTickets, 500f, 495f), 0.0001f);
    }

    [Test]
    public void StateMetricsIgnoreTheBaseline()
    {
        DirectiveDefinition rating = new DirectiveDefinition("r", "r", "r", DirectiveMetric.StationRating, 3f, 0);

        Assert.IsTrue(DirectiveRules.IsComplete(rating, 3.2f, 99f));
        Assert.IsFalse(DirectiveRules.IsComplete(rating, 2.9f, 0f));
    }

    [Test]
    public void RatingThatSettlesJustUnderTheTargetStillCounts()
    {
        DirectiveDefinition rating = new DirectiveDefinition("r", "r", "r", DirectiveMetric.StationRating, 4f, 0);
        Assert.IsTrue(DirectiveRules.IsComplete(rating, 3.99995f, 0f));
    }

    [Test]
    public void ProgressTextDoesNotOvershootTheTarget()
    {
        DirectiveDefinition boardings = new DirectiveDefinition("b", "b", "b", DirectiveMetric.PassengersBoarded, 10, 0);
        Assert.AreEqual("10 / 10", DirectiveRules.FormatProgress(boardings, 14f, 0f));
    }

    [Test]
    public void ContractsAreDeterministicFromTheirSerial()
    {
        // A saved contract is rebuilt from its serial on load, so it must come back identical.
        for (int serial = 0; serial < 40; serial++)
        {
            DirectiveDefinition first = DirectiveCatalogue.CreateContract(serial, 3);
            DirectiveDefinition second = DirectiveCatalogue.CreateContract(serial, 3);

            Assert.AreEqual(first.metric, second.metric);
            Assert.AreEqual(first.target, second.target);
            Assert.AreEqual(first.rewardMoney, second.rewardMoney);
            Assert.Greater(first.target, 0f);
            Assert.Greater(first.rewardMoney, 0);
        }
    }

    [Test]
    public void ContractsGetHarderAtHigherTiers()
    {
        for (int serial = 0; serial < 20; serial++)
        {
            DirectiveDefinition low = DirectiveCatalogue.CreateContract(serial, 1);
            DirectiveDefinition high = DirectiveCatalogue.CreateContract(serial, 5);

            Assert.AreEqual(low.metric, high.metric);
            Assert.GreaterOrEqual(high.target, low.target);
            Assert.GreaterOrEqual(high.rewardMoney, low.rewardMoney);
        }
    }
}
