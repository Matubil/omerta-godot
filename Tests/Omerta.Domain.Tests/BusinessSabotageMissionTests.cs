using Omerta.Domain.Economy;
using Omerta.Domain.Missions;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class BusinessSabotageMissionTests
{
    [Test]
    public void MissionCannotStartBeforePrerequisiteUnlocksIt()
    {
        BusinessSabotageMission mission = new(new EconomyService(100), 100);

        Assert.That(mission.Start(), Is.False);
        Assert.That(mission.Unlock(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(BusinessSabotageMissionStatus.Available));
        Assert.That(mission.Start(), Is.True);
    }

    [Test]
    public void SabotageCompletesAndRewardsOnlyOnce()
    {
        EconomyService economy = new(100);
        BusinessSabotageMission mission = new(economy, 100);
        mission.Unlock();
        mission.Start();
        mission.MarkPropertyReached();

        Assert.That(mission.MarkSabotaged(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(BusinessSabotageMissionStatus.Completed));
        Assert.That(mission.RewardGranted, Is.True);
        Assert.That(economy.Balance, Is.EqualTo(200));
        Assert.That(mission.MarkSabotaged(), Is.False);
        Assert.That(economy.Balance, Is.EqualTo(200));
    }

    [Test]
    public void DefeatFailsActiveMissionAndAllowsRetry()
    {
        BusinessSabotageMission mission = new(new EconomyService(100), 100);
        mission.Unlock();
        mission.Start();

        Assert.That(mission.Fail(), Is.True);
        Assert.That(mission.Status, Is.EqualTo(BusinessSabotageMissionStatus.Failed));
        Assert.That(mission.CanStart, Is.True);
        Assert.That(mission.Start(), Is.True);
    }

    [Test]
    public void LoadSynchronizationDoesNotDuplicateSavedReward()
    {
        EconomyService economy = new(350);
        BusinessSabotageMission mission = new(economy, 100);

        mission.SynchronizeAfterLoad(true, true);

        Assert.That(mission.Status, Is.EqualTo(BusinessSabotageMissionStatus.Completed));
        Assert.That(mission.RewardGranted, Is.True);
        Assert.That(economy.Balance, Is.EqualTo(350));
    }

    [Test]
    public void RestoreProgressKeepsExactPhaseWithoutPayingReward()
    {
        EconomyService economy = new(350);
        BusinessSabotageMission mission = new(economy, 100);

        mission.RestoreProgress(BusinessSabotageMissionStatus.SabotageTarget, false);

        Assert.That(mission.Status, Is.EqualTo(BusinessSabotageMissionStatus.SabotageTarget));
        Assert.That(mission.RewardGranted, Is.False);
        Assert.That(economy.Balance, Is.EqualTo(350));
    }
}
