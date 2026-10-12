using System;
using System.Collections.Generic;

namespace Omerta.Domain.Combat.Weapons;

public sealed record WeaponSpec
{
    public WeaponSpec(
        string id,
        string displayName,
        int damage,
        double fireIntervalSeconds,
        float effectiveRange,
        int magazineCapacity,
        double reloadDurationSeconds,
        float hipFireSpreadDegrees,
        float aimSpreadDegrees,
        int pelletCount = 1,
        bool canBeConcealed = true,
        IEnumerable<string>? compatibleSlotIds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A stable weapon ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (damage <= 0 || fireIntervalSeconds <= 0 || effectiveRange <= 0 ||
            magazineCapacity <= 0 || reloadDurationSeconds <= 0 ||
            hipFireSpreadDegrees < 0 || aimSpreadDegrees < 0 ||
            aimSpreadDegrees > hipFireSpreadDegrees || pelletCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "Weapon values are outside their valid range.");
        }

        Id = id;
        DisplayName = displayName;
        Damage = damage;
        FireIntervalSeconds = fireIntervalSeconds;
        EffectiveRange = effectiveRange;
        MagazineCapacity = magazineCapacity;
        ReloadDurationSeconds = reloadDurationSeconds;
        HipFireSpreadDegrees = hipFireSpreadDegrees;
        AimSpreadDegrees = aimSpreadDegrees;
        PelletCount = pelletCount;
        CanBeConcealed = canBeConcealed;
        CompatibleSlotIds = new HashSet<string>(
            compatibleSlotIds ?? new[] { WeaponSlotIds.Sidearm, WeaponSlotIds.LongGun },
            StringComparer.Ordinal);
        if (CompatibleSlotIds.Count == 0)
        {
            throw new ArgumentException(
                "A weapon needs at least one compatible carried slot.",
                nameof(compatibleSlotIds));
        }
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int Damage { get; }
    public double FireIntervalSeconds { get; }
    public float EffectiveRange { get; }
    public int MagazineCapacity { get; }
    public double ReloadDurationSeconds { get; }
    public float HipFireSpreadDegrees { get; }
    public float AimSpreadDegrees { get; }
    public int PelletCount { get; }
    public bool CanBeConcealed { get; }
    public IReadOnlySet<string> CompatibleSlotIds { get; }

    public bool CanUseSlot(string slotId) => CompatibleSlotIds.Contains(slotId);

    public float GetSpreadDegrees(bool isAiming) =>
        isAiming ? AimSpreadDegrees : HipFireSpreadDegrees;
}
