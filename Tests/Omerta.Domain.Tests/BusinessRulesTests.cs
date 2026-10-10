using Omerta.Domain.Businesses;
using Omerta.Domain.Economy;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class BusinessRulesTests
{
    private const string Player = "faction.player-family";
    private const string Rival = "faction.circle-ferro";

    [Test]
    public void EnvironmentalPressureStopsBeforeCaptureAndRequiresDirectDemand()
    {
        BusinessState business = CreateBusiness(Rival);
        ExtortionSession session = new(business, Player, 100);

        Assert.That(session.ApplyEnvironmentalPressure(120), Is.EqualTo(99));
        Assert.That(session.IsComplete, Is.False);
        Assert.That(business.OwnerId, Is.EqualTo(Rival));

        Assert.That(session.ApplyPressure(25), Is.True);
        Assert.That(business.OwnerId, Is.EqualTo(Player));
    }

    [Test]
    public void FourDirectThreatsCaptureElFaro()
    {
        BusinessState business = CreateBusiness(Rival);
        ExtortionSession session = new(business, Player, 100);

        Assert.That(session.ApplyPressure(25), Is.False);
        Assert.That(session.ApplyPressure(25), Is.False);
        Assert.That(session.ApplyPressure(25), Is.False);
        Assert.That(session.ApplyPressure(25), Is.True);
        Assert.That(session.Reaction, Is.EqualTo(MerchantExtortionReaction.Compliant));
    }

    [Test]
    public void SabotageStopsProductionWithoutChangingOwnership()
    {
        BusinessState business = CreateBusiness(Rival);
        BusinessOperationsState operations = new(business.Id);

        Assert.That(operations.SabotageCriticalAsset(), Is.True);
        Assert.That(operations.IsOperational, Is.False);
        Assert.That(business.OwnerId, Is.EqualTo(Rival));
    }

    [Test]
    public void ForcedTakeoverRequiresEveryDefenderAndAnExplicitDemand()
    {
        BusinessState business = CreateBusiness(Rival);
        ForcedTakeoverSession takeover = new(business, Player, 1);

        Assert.That(takeover.DemandSurrender(), Is.False);
        takeover.SetDefenderDefeated(0, true);
        Assert.That(business.OwnerId, Is.EqualTo(Rival));

        Assert.That(takeover.DemandSurrender(), Is.True);
        Assert.That(business.OwnerId, Is.EqualTo(Player));
    }

    [Test]
    public void IncomeRequiresPlayerOwnershipAndOperationalBusiness()
    {
        EconomyService economy = new(100);
        BusinessState business = CreateBusiness(Rival);
        BusinessOperationsState operations = new(business.Id);
        BusinessIncomeService income = new(Player, economy, business, operations, 50, 5.0f);

        income.Tick(10.0f);
        Assert.That(economy.Balance, Is.EqualTo(100));

        business.Capture(Player);
        operations.SabotageCriticalAsset();
        income.Tick(10.0f);
        Assert.That(economy.Balance, Is.EqualTo(100));

        operations.RepairCriticalAsset();
        income.Tick(5.0f);
        Assert.That(economy.Balance, Is.EqualTo(150));
    }

    [Test]
    public void MerchantDefeatStopsIncomeUntilOperatorReturns()
    {
        EconomyService economy = new();
        BusinessState business = CreateBusiness(Player);
        BusinessOperationsState operations = new(business.Id);
        BusinessIncomeService income = new(Player, economy, business, operations, 50, 5.0f);

        operations.SetOperatorAvailable(false);
        income.Tick(5.0f);
        Assert.That(economy.Balance, Is.Zero);

        operations.SetOperatorAvailable(true);
        income.Tick(5.0f);
        Assert.That(economy.Balance, Is.EqualTo(50));
    }

    private static BusinessState CreateBusiness(string owner) =>
        new("business.el-faro", "Taller El Faro", owner);
}
