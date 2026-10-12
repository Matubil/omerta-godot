using System;
using System.Collections.Generic;

namespace Omerta.Domain.Combat.Weapons;

/// <summary>
/// The two weapons the protagonist can physically carry. Arsenal storage lives outside
/// this loadout, so taking a long gun always requires a free compatible slot.
/// </summary>
public sealed class WeaponLoadout
{
    private readonly Dictionary<string, WeaponRuntime?> _slots;

    public WeaponLoadout(IEnumerable<string> availableSlots)
    {
        ArgumentNullException.ThrowIfNull(availableSlots);
        _slots = new Dictionary<string, WeaponRuntime?>(StringComparer.Ordinal);
        foreach (string slotId in availableSlots)
        {
            if (string.IsNullOrWhiteSpace(slotId) || !_slots.TryAdd(slotId, null))
            {
                throw new ArgumentException("Carried weapon slots must be unique and non-empty.", nameof(availableSlots));
            }
        }

        if (_slots.Count == 0)
        {
            throw new ArgumentException("A loadout needs at least one carried slot.", nameof(availableSlots));
        }
    }

    public event Action? Changed;

    public WeaponRuntime? EquippedRuntime { get; private set; }
    public bool IsHolstered { get; private set; }

    public bool TryCarry(WeaponRuntime runtime, string slotId)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!_slots.TryGetValue(slotId, out WeaponRuntime? occupant) || occupant is not null ||
            !runtime.Specification.CanUseSlot(slotId) || Contains(runtime.Specification.Id))
        {
            return false;
        }

        _slots[slotId] = runtime;
        EquippedRuntime ??= runtime;
        Changed?.Invoke();
        return true;
    }

    public bool TryStore(string slotId, out WeaponRuntime? runtime)
    {
        runtime = null;
        if (!_slots.TryGetValue(slotId, out WeaponRuntime? occupant) || occupant is null)
        {
            return false;
        }

        runtime = occupant;
        _slots[slotId] = null;
        occupant.CancelReload();
        if (EquippedRuntime == occupant)
        {
            EquippedRuntime = FirstCarriedWeapon();
        }

        Changed?.Invoke();
        return true;
    }

    public bool TryEquipSlot(string slotId)
    {
        if (!_slots.TryGetValue(slotId, out WeaponRuntime? runtime) || runtime is null)
        {
            return false;
        }

        if (EquippedRuntime != runtime)
        {
            EquippedRuntime?.CancelReload();
            EquippedRuntime = runtime;
        }

        SetHolstered(false);
        Changed?.Invoke();
        return true;
    }

    public bool TryEquipWeapon(string weaponId)
    {
        foreach ((string slotId, WeaponRuntime? runtime) in _slots)
        {
            if (runtime?.Specification.Id == weaponId)
            {
                return TryEquipSlot(slotId);
            }
        }

        return false;
    }

    public WeaponRuntime? GetWeaponInSlot(string slotId) =>
        _slots.TryGetValue(slotId, out WeaponRuntime? runtime) ? runtime : null;

    public bool IsCarried(string weaponId) => Contains(weaponId);

    public void SetHolstered(bool holstered)
    {
        if (IsHolstered == holstered)
        {
            return;
        }

        IsHolstered = holstered;
        if (holstered)
        {
            EquippedRuntime?.CancelReload();
        }

        Changed?.Invoke();
    }

    private bool Contains(string weaponId)
    {
        foreach (WeaponRuntime? runtime in _slots.Values)
        {
            if (runtime?.Specification.Id == weaponId)
            {
                return true;
            }
        }

        return false;
    }

    private WeaponRuntime? FirstCarriedWeapon()
    {
        foreach (WeaponRuntime? runtime in _slots.Values)
        {
            if (runtime is not null)
            {
                return runtime;
            }
        }

        return null;
    }
}
