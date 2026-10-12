using Godot;
using Omerta.Domain.Combat.Weapons;

namespace Omerta.GodotAdapters.Combat;

[GlobalClass]
public sealed partial class WeaponDefinitionResource : Resource
{
    [Export]
    public string Id { get; set; } = "weapon.prototype-pistol";

    [Export]
    public string DisplayName { get; set; } = "Pistola semiautomática";

    [Export]
    public int Damage { get; set; } = 25;

    [Export]
    public float FireIntervalSeconds { get; set; } = 0.35f;

    [Export]
    public float EffectiveRange { get; set; } = 30.0f;

    [Export]
    public int MagazineCapacity { get; set; } = 7;

    [Export]
    public float ReloadDurationSeconds { get; set; } = 1.4f;

    [Export]
    public float HipFireSpreadDegrees { get; set; } = 3.5f;

    [Export]
    public float AimSpreadDegrees { get; set; } = 0.65f;

    [Export]
    public int PelletCount { get; set; } = 1;

    [Export]
    public bool CanBeConcealed { get; set; } = true;

    [Export]
    public string CompatibleSlotId { get; set; } = WeaponSlotIds.Sidearm;

    public WeaponSpec CreateSpec() => new(
        Id,
        DisplayName,
        Damage,
        FireIntervalSeconds,
        EffectiveRange,
        MagazineCapacity,
        ReloadDurationSeconds,
        HipFireSpreadDegrees,
        AimSpreadDegrees,
        PelletCount,
        CanBeConcealed,
        new[] { CompatibleSlotId });
}
