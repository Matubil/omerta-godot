using Omerta.Domain.Law;
using Omerta.Domain.Vehicles;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class VehicleConditionTests
{
    [Test]
    public void MinorContactDoesNotDamageVehicle()
    {
        VehicleCondition condition = new(100, 4.0f, 10.0f);

        int damage = condition.ApplyImpact(4.0f);

        Assert.That(damage, Is.Zero);
        Assert.That(condition.Current, Is.EqualTo(100));
    }

    [Test]
    public void SevereImpactCanDisableAndRepairVehicle()
    {
        VehicleCondition condition = new(100, 4.0f, 10.0f);

        Assert.That(condition.ApplyImpact(14.0f), Is.EqualTo(100));
        Assert.That(condition.IsDisabled, Is.True);
        Assert.That(condition.RepairToFull(), Is.True);
        Assert.That(condition.Current, Is.EqualTo(100));
    }

    [Test]
    public void InterceptionRequiresPoliceRangeAndContinuousStop()
    {
        VehicleInterceptionState interception = new(2.5f, 0.75f);

        Assert.That(interception.Advance(1.5f, true, 0.2f, false), Is.False);
        Assert.That(interception.RemainingSeconds, Is.EqualTo(1.0f));
        Assert.That(interception.Advance(0.1f, false, 0.2f, false), Is.False);
        Assert.That(interception.RemainingSeconds, Is.EqualTo(2.5f));
        Assert.That(interception.Advance(2.5f, true, 0.0f, false), Is.True);
        Assert.That(interception.IsComplete, Is.True);
    }

    [Test]
    public void DisabledVehicleIsEligibleForImmediateInterceptionCountdown()
    {
        VehicleInterceptionState interception = new(2.5f, 0.75f);

        Assert.That(interception.Advance(2.5f, true, 8.0f, true), Is.True);
    }

    [Test]
    public void LowAlertWithFundsEndsInFineOtherwiseArrest()
    {
        Assert.That(
            PoliceInterceptionRules.Evaluate(1, 100, 90),
            Is.EqualTo(PoliceInterceptionOutcome.Fine));
        Assert.That(
            PoliceInterceptionRules.Evaluate(1, 20, 90),
            Is.EqualTo(PoliceInterceptionOutcome.Arrest));
        Assert.That(
            PoliceInterceptionRules.Evaluate(2, 100, 90),
            Is.EqualTo(PoliceInterceptionOutcome.Arrest));
    }
}
