using Omerta.Domain.Businesses;
using Omerta.Domain.Economy;
using Omerta.Domain.Missions;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class BusinessDefenseMissionTests
{
    [Test]
    public void CounterattackWarningAndAttackBecomeUrgentMissionStates()
    {
        BusinessDefenseMission mission = new(new EconomyService(100), 125);

        mission.Synchronize(BusinessCounterattackStatus.Warning);
        Assert.That(mission.Status, Is.EqualTo(BusinessDefenseMissionStatus.Warning));
        Assert.That(mission.IsActive, Is.True);

        mission.Synchronize(BusinessCounterattackStatus.InProgress);
        Assert.That(mission.Status, Is.EqualTo(BusinessDefenseMissionStatus.Defending));
        Assert.That(mission.IsActive, Is.True);
    }

    [Test]
    public void DefendedCounterattackRewardsOnlyOnce()
    {
        EconomyService economy = new(250);
        BusinessDefenseMission mission = new(economy, 125);

        mission.Synchronize(BusinessCounterattackStatus.Warning);
        mission.Synchronize(BusinessCounterattackStatus.InProgress);
        mission.Synchronize(BusinessCounterattackStatus.Defended);
        mission.Synchronize(BusinessCounterattackStatus.Defended);

        Assert.That(mission.Status, Is.EqualTo(BusinessDefenseMissionStatus.Succeeded));
        Assert.That(mission.RewardGranted, Is.True);
        Assert.That(economy.Balance, Is.EqualTo(375));
    }

    [Test]
    public void SuccessfulSabotageFailsDefenseWithoutReward()
    {
        EconomyService economy = new(250);
        BusinessDefenseMission mission = new(economy, 125);

        mission.Synchronize(BusinessCounterattackStatus.Warning);
        mission.Synchronize(BusinessCounterattackStatus.InProgress);
        mission.Synchronize(BusinessCounterattackStatus.Sabotaged);

        Assert.That(mission.Status, Is.EqualTo(BusinessDefenseMissionStatus.Failed));
        Assert.That(mission.RewardGranted, Is.False);
        Assert.That(economy.Balance, Is.EqualTo(250));
    }

    [Test]
    public void RestoredSuccessDoesNotPaySavedRewardAgain()
    {
        EconomyService economy = new(375);
        BusinessDefenseMission mission = new(economy, 125);

        mission.RestoreProgress(BusinessDefenseMissionStatus.Succeeded, true);

        Assert.That(mission.Status, Is.EqualTo(BusinessDefenseMissionStatus.Succeeded));
        Assert.That(mission.RewardGranted, Is.True);
        Assert.That(economy.Balance, Is.EqualTo(375));
    }
}
