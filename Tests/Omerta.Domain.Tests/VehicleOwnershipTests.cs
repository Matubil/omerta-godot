using Omerta.Domain.Economy;
using Omerta.Domain.Vehicles;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class VehicleOwnershipTests
{
    [Test]
    public void StolenVehicleRequiresAvailableGarageAndRegistrationPayment()
    {
        EconomyService economy = new(100);
        VehicleOwnership vehicle = new("vehicle.stolen.prototype", false);

        VehicleRegistrationResult unavailable = vehicle.TryRegister(false, economy, 75);
        VehicleRegistrationResult registered = vehicle.TryRegister(true, economy, 75);

        Assert.That(unavailable, Is.EqualTo(VehicleRegistrationResult.GarageUnavailable));
        Assert.That(registered, Is.EqualTo(VehicleRegistrationResult.Registered));
        Assert.That(vehicle.IsFamilyOwned, Is.True);
        Assert.That(economy.Balance, Is.EqualTo(25));
    }

    [Test]
    public void FailedPaymentDoesNotRegisterOrChargeVehicle()
    {
        EconomyService economy = new(50);
        VehicleOwnership vehicle = new("vehicle.stolen.prototype", false);

        VehicleRegistrationResult result = vehicle.TryRegister(true, economy, 75);

        Assert.That(result, Is.EqualTo(VehicleRegistrationResult.InsufficientFunds));
        Assert.That(vehicle.IsFamilyOwned, Is.False);
        Assert.That(economy.Balance, Is.EqualTo(50));
    }

    [Test]
    public void AlreadyOwnedVehicleCannotBeChargedTwice()
    {
        EconomyService economy = new(100);
        VehicleOwnership vehicle = new("vehicle.family.reinforcement", true);

        VehicleRegistrationResult result = vehicle.TryRegister(true, economy, 75);

        Assert.That(result, Is.EqualTo(VehicleRegistrationResult.AlreadyFamilyOwned));
        Assert.That(economy.Balance, Is.EqualTo(100));
    }

    [Test]
    public void RestoreAndResetPreserveOriginalOwnershipRule()
    {
        VehicleOwnership vehicle = new("vehicle.stolen.prototype", false);

        vehicle.Restore(true);
        Assert.That(vehicle.IsFamilyOwned, Is.True);

        vehicle.Reset();
        Assert.That(vehicle.IsFamilyOwned, Is.False);
    }
}
