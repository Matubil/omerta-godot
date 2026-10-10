using System;

namespace Omerta.Domain.Combat.Weapons;

public sealed class WeaponRuntime
{
    private double _nextAllowedFireTime;
    private double _reloadCompletionTime;

    public WeaponRuntime(WeaponSpec specification, int initialReserveAmmo)
    {
        Specification = specification ?? throw new ArgumentNullException(nameof(specification));
        if (initialReserveAmmo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialReserveAmmo));
        }

        InitialReserveAmmo = initialReserveAmmo;
        AmmoInMagazine = specification.MagazineCapacity;
        ReserveAmmo = initialReserveAmmo;
    }

    public event Action? Changed;

    public WeaponSpec Specification { get; }
    public int InitialReserveAmmo { get; }
    public int AmmoInMagazine { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }

    public bool TryFire(double currentTime)
    {
        ValidateTime(currentTime);
        if (IsReloading || AmmoInMagazine == 0 || currentTime < _nextAllowedFireTime)
        {
            return false;
        }

        AmmoInMagazine--;
        _nextAllowedFireTime = currentTime + Specification.FireIntervalSeconds;
        Changed?.Invoke();
        return true;
    }

    public bool TryStartReload(double currentTime)
    {
        ValidateTime(currentTime);
        if (IsReloading || AmmoInMagazine == Specification.MagazineCapacity || ReserveAmmo == 0)
        {
            return false;
        }

        IsReloading = true;
        _reloadCompletionTime = currentTime + Specification.ReloadDurationSeconds;
        Changed?.Invoke();
        return true;
    }

    public bool Advance(double currentTime)
    {
        ValidateTime(currentTime);
        if (!IsReloading || currentTime < _reloadCompletionTime)
        {
            return false;
        }

        int needed = Specification.MagazineCapacity - AmmoInMagazine;
        int transferred = Math.Min(needed, ReserveAmmo);
        AmmoInMagazine += transferred;
        ReserveAmmo -= transferred;
        IsReloading = false;
        Changed?.Invoke();
        return true;
    }

    public bool CancelReload()
    {
        if (!IsReloading)
        {
            return false;
        }

        IsReloading = false;
        Changed?.Invoke();
        return true;
    }

    public void Reset()
    {
        AmmoInMagazine = Specification.MagazineCapacity;
        ReserveAmmo = InitialReserveAmmo;
        IsReloading = false;
        _nextAllowedFireTime = 0;
        _reloadCompletionTime = 0;
        Changed?.Invoke();
    }

    private static void ValidateTime(double currentTime)
    {
        if (currentTime < 0 || double.IsNaN(currentTime))
        {
            throw new ArgumentOutOfRangeException(nameof(currentTime));
        }
    }
}
