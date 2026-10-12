using Omerta.Domain.Combat.Weapons;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class WeaponLoadoutTests
{
    [Test]
    public void LongGunOnlyFitsItsSingleCarriedSlot()
    {
        WeaponRuntime shotgun = CreateShotgun();
        WeaponLoadout loadout = new(new[] { WeaponSlotIds.Sidearm, WeaponSlotIds.LongGun });

        Assert.That(loadout.TryCarry(shotgun, WeaponSlotIds.Sidearm), Is.False);
        Assert.That(loadout.TryCarry(shotgun, WeaponSlotIds.LongGun), Is.True);
        Assert.That(loadout.GetWeaponInSlot(WeaponSlotIds.LongGun), Is.SameAs(shotgun));
    }

    [Test]
    public void StoredLongGunKeepsAmmunitionAndCannotBeEquipped()
    {
        WeaponRuntime pistol = CreatePistol();
        WeaponRuntime shotgun = CreateShotgun();
        WeaponLoadout loadout = new(new[] { WeaponSlotIds.Sidearm, WeaponSlotIds.LongGun });
        loadout.TryCarry(pistol, WeaponSlotIds.Sidearm);
        loadout.TryCarry(shotgun, WeaponSlotIds.LongGun);
        shotgun.TryFire(0.0);
        loadout.TryEquipSlot(WeaponSlotIds.LongGun);

        bool stored = loadout.TryStore(WeaponSlotIds.LongGun, out WeaponRuntime? removed);

        Assert.That(stored, Is.True);
        Assert.That(removed, Is.SameAs(shotgun));
        Assert.That(shotgun.AmmoInMagazine, Is.EqualTo(4));
        Assert.That(loadout.EquippedRuntime, Is.SameAs(pistol));
        Assert.That(loadout.TryEquipSlot(WeaponSlotIds.LongGun), Is.False);
    }

    [Test]
    public void SelectingCarriedWeaponDrawsItAfterHolstering()
    {
        WeaponRuntime pistol = CreatePistol();
        WeaponLoadout loadout = new(new[] { WeaponSlotIds.Sidearm, WeaponSlotIds.LongGun });
        loadout.TryCarry(pistol, WeaponSlotIds.Sidearm);
        loadout.SetHolstered(true);

        bool equipped = loadout.TryEquipSlot(WeaponSlotIds.Sidearm);

        Assert.That(equipped, Is.True);
        Assert.That(loadout.IsHolstered, Is.False);
    }

    [Test]
    public void ShotgunPreservesUnityPelletAndConcealmentRules()
    {
        WeaponSpec shotgun = CreateShotgun().Specification;

        Assert.That(shotgun.PelletCount, Is.EqualTo(8));
        Assert.That(shotgun.CanBeConcealed, Is.False);
        Assert.That(shotgun.CanUseSlot(WeaponSlotIds.LongGun), Is.True);
        Assert.That(shotgun.CanUseSlot(WeaponSlotIds.Sidearm), Is.False);
    }

    private static WeaponRuntime CreatePistol() => new(
        new WeaponSpec(
            "weapon.prototype-pistol",
            "Pistola",
            25,
            0.35,
            30,
            7,
            1.4,
            3.5f,
            0.65f,
            1,
            true,
            new[] { WeaponSlotIds.Sidearm }),
        28);

    private static WeaponRuntime CreateShotgun() => new(
        new WeaponSpec(
            "weapon.prototype-shotgun",
            "Escopeta recortada",
            12,
            1.05,
            22,
            5,
            2.2,
            10.0f,
            6.0f,
            8,
            false,
            new[] { WeaponSlotIds.LongGun }),
        20);
}
