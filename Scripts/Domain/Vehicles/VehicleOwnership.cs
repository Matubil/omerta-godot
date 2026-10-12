using System;
using Omerta.Domain.Economy;

namespace Omerta.Domain.Vehicles;

public enum VehicleRegistrationResult
{
    Registered,
    AlreadyFamilyOwned,
    GarageUnavailable,
    InsufficientFunds
}

public sealed class VehicleOwnership
{
    public VehicleOwnership(string vehicleId, bool startsFamilyOwned)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            throw new ArgumentException("A stable vehicle ID is required.", nameof(vehicleId));
        }

        VehicleId = vehicleId;
        StartsFamilyOwned = startsFamilyOwned;
        IsFamilyOwned = startsFamilyOwned;
    }

    public event Action? Changed;

    public string VehicleId { get; }
    public bool StartsFamilyOwned { get; }
    public bool IsFamilyOwned { get; private set; }

    public VehicleRegistrationResult TryRegister(
        bool garageAvailable,
        EconomyService economy,
        int registrationCost)
    {
        ArgumentNullException.ThrowIfNull(economy);
        if (registrationCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(registrationCost));
        }

        if (IsFamilyOwned)
        {
            return VehicleRegistrationResult.AlreadyFamilyOwned;
        }

        if (!garageAvailable)
        {
            return VehicleRegistrationResult.GarageUnavailable;
        }

        if (!economy.TrySpendMoney(registrationCost))
        {
            return VehicleRegistrationResult.InsufficientFunds;
        }

        IsFamilyOwned = true;
        Changed?.Invoke();
        return VehicleRegistrationResult.Registered;
    }

    public void Restore(bool familyOwned)
    {
        if (IsFamilyOwned == familyOwned)
        {
            return;
        }

        IsFamilyOwned = familyOwned;
        Changed?.Invoke();
    }

    public void Reset() => Restore(StartsFamilyOwned);
}
