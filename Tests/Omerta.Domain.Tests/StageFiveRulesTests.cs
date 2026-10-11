using Omerta.Domain.Businesses;
using Omerta.Domain.Economy;
using Omerta.Domain.Law;
using Omerta.Domain.Missions;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class StageFiveRulesTests
{
    private const string Player = "faction.player-family";
    private const string Rival = "faction.circle-ferro";

    [Test]
    public void TakeoverMissionWaitsForPoliceAndGrantsRewardExactlyOnce()
    {
        EconomyService economy = new(100);
        PropertyTakeoverMission mission = new(economy, 150);

        Assert.That(mission.Start(), Is.True);
        Assert.That(mission.MarkPropertyReached(), Is.True);
        Assert.That(mission.MarkPropertyCaptured(true), Is.True);
        Assert.That(mission.Status, Is.EqualTo(PropertyTakeoverMissionStatus.ResolvePoliceResponse));
        Assert.That(economy.Balance, Is.EqualTo(100));

        Assert.That(mission.MarkPoliceResponseResolved(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(PropertyTakeoverMissionStatus.Completed));
        Assert.That(economy.Balance, Is.EqualTo(250));
        Assert.That(mission.MarkPoliceResponseResolved(), Is.False);
        Assert.That(economy.Balance, Is.EqualTo(250));
    }

    [Test]
    public void FailedTakeoverMissionCanBeRetried()
    {
        PropertyTakeoverMission mission = new(new EconomyService(), 150);

        mission.Start();
        Assert.That(mission.Fail(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(PropertyTakeoverMissionStatus.Failed));
        Assert.That(mission.Start(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(PropertyTakeoverMissionStatus.ReachProperty));
    }

    [Test]
    public void PoliceDispatchCanEndByLosingVisualContact()
    {
        PoliceResponseState response = new(3.0f, 8.0f);

        response.ReportGunfire(1);
        Assert.That(response.Status, Is.EqualTo(PoliceResponseStatus.Dispatching));
        Assert.That(response.AdvanceDispatch(3.0f), Is.True);
        Assert.That(response.MarkArrived(), Is.True);
        response.AdvanceSearch(4.0f, false);
        Assert.That(response.RemainingEscapeSeconds, Is.EqualTo(4.0f));
        response.AdvanceSearch(1.0f, true);
        Assert.That(response.RemainingEscapeSeconds, Is.EqualTo(8.0f));

        Assert.That(response.AdvanceSearch(8.0f, false), Is.True);
        Assert.That(response.Status, Is.EqualTo(PoliceResponseStatus.Escaped));
        Assert.That(response.WantedLevel, Is.Zero);
    }

    [Test]
    public void BribeRequiresOneLowLevelWitnessAndEnoughMoney()
    {
        Assert.That(
            PoliceBribeRules.Evaluate(true, 1, 1, false, 100, 60),
            Is.EqualTo(PoliceBribeDecision.Accepted));
        Assert.That(
            PoliceBribeRules.Evaluate(true, 1, 1, false, 20, 60),
            Is.EqualTo(PoliceBribeDecision.InsufficientFunds));
        Assert.That(
            PoliceBribeRules.Evaluate(true, 2, 1, false, 100, 60),
            Is.EqualTo(PoliceBribeDecision.EscalatedAlert));
        Assert.That(
            PoliceBribeRules.Evaluate(true, 1, 2, false, 100, 60),
            Is.EqualTo(PoliceBribeDecision.MultipleWitnesses));
    }

    [Test]
    public void CounterattackSabotagesOperationsWithoutTransferringOwnership()
    {
        BusinessState business = new("business.el-faro", "Taller El Faro", Rival);
        BusinessOperationsState operations = new(business.Id);
        using BusinessCounterattack counterattack = new(
            business,
            operations,
            Player,
            12.0f,
            6.0f,
            1);

        business.Capture(Player);
        Assert.That(counterattack.Status, Is.EqualTo(BusinessCounterattackStatus.Warning));
        Assert.That(counterattack.Advance(12.0f), Is.True);
        Assert.That(counterattack.Status, Is.EqualTo(BusinessCounterattackStatus.InProgress));
        Assert.That(counterattack.SetAttackerAtObjective(0, true), Is.True);
        Assert.That(counterattack.Advance(6.0f), Is.True);

        Assert.That(counterattack.Status, Is.EqualTo(BusinessCounterattackStatus.Sabotaged));
        Assert.That(operations.IsOperational, Is.False);
        Assert.That(business.OwnerId, Is.EqualTo(Player));
    }

    [Test]
    public void DefeatingEveryCounterattackerDefendsTheBusiness()
    {
        BusinessState business = new("business.el-faro", "Taller El Faro", Rival);
        BusinessOperationsState operations = new(business.Id);
        using BusinessCounterattack counterattack = new(
            business,
            operations,
            Player,
            12.0f,
            6.0f,
            1);

        business.Capture(Player);
        counterattack.Advance(12.0f);

        Assert.That(counterattack.SetAttackerDefeated(0), Is.True);
        Assert.That(counterattack.Status, Is.EqualTo(BusinessCounterattackStatus.Defended));
        Assert.That(operations.IsOperational, Is.True);
        Assert.That(business.OwnerId, Is.EqualTo(Player));
    }
}
