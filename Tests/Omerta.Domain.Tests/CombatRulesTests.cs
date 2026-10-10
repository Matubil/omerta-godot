using Omerta.Domain.Combat.AI;
using Omerta.Domain.Combat.Damage;
using Omerta.Domain.Combat.Health;
using Omerta.Domain.Combat.Weapons;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class CombatRulesTests
{
    [Test]
    public void HealthClampsDamageAndRejectsDamageAfterDefeat()
    {
        Health health = new(100);

        Assert.That(health.ReceiveDamage(new DamageInfo(125)), Is.True);
        Assert.That(health.Current, Is.Zero);
        Assert.That(health.IsDefeated, Is.True);
        Assert.That(health.ReceiveDamage(new DamageInfo(1)), Is.False);
    }

    [Test]
    public void ReloadTransfersOnlyTheMissingRounds()
    {
        WeaponRuntime weapon = CreatePistol(28);
        weapon.TryFire(0.0);
        weapon.TryFire(0.35);

        Assert.That(weapon.TryStartReload(0.7), Is.True);
        Assert.That(weapon.Advance(2.1), Is.True);
        Assert.That(weapon.AmmoInMagazine, Is.EqualTo(7));
        Assert.That(weapon.ReserveAmmo, Is.EqualTo(26));
    }

    [Test]
    public void FireRateAndReloadBlockInvalidShots()
    {
        WeaponRuntime weapon = CreatePistol(28);

        Assert.That(weapon.TryFire(0.0), Is.True);
        Assert.That(weapon.TryFire(0.2), Is.False);
        Assert.That(weapon.TryStartReload(0.35), Is.True);
        Assert.That(weapon.TryFire(0.7), Is.False);
    }

    [Test]
    public void PerceptionRequiresContinuousConfirmation()
    {
        EnemyPerceptionState perception = new(1.2f, 0.75f);

        perception.Observe(true, 0.6f);
        Assert.That(perception.Awareness, Is.EqualTo(EnemyAwareness.Suspicious));
        perception.Observe(true, 0.6f);

        Assert.That(perception.IsAlert, Is.True);
    }

    private static WeaponRuntime CreatePistol(int reserve) =>
        new(
            new WeaponSpec(
                "weapon.prototype-pistol",
                "Pistola semiautomática",
                25,
                0.35,
                30.0f,
                7,
                1.4,
                3.5f,
                0.65f),
            reserve);
}
