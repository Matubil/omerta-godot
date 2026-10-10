using System;
using System.IO;
using Omerta.Domain.Combat.Weapons;
using Omerta.Domain.Persistence;
using Omerta.Infrastructure.Persistence;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class PersistenceTests
{
    [Test]
    public void JsonRoundTripPreservesCurrentSliceState()
    {
        SaveGameData source = CreateSaveGame();
        SaveGameSerializer serializer = new();

        SaveGameData restored = serializer.Deserialize(serializer.Serialize(source));

        Assert.That(restored.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(restored.Economy.Balance, Is.EqualTo(350));
        Assert.That(restored.Businesses[0].OwnerFactionId, Is.EqualTo("faction.player-family"));
        Assert.That(restored.Businesses[0].IsCriticalAssetOperational, Is.False);
        Assert.That(restored.CharacterCombatStates[1].CurrentHealth, Is.Zero);
        Assert.That(restored.PlayerPose.Position.X, Is.EqualTo(48.5f));
        Assert.That(restored.PlayerWeapons.Weapons[0].MagazineAmmo, Is.EqualTo(3));
        Assert.That(restored.PlayerWeapons.Weapons[0].ReserveAmmo, Is.EqualTo(21));
    }

    [Test]
    public void IncompatibleVersionFailsExplicitly()
    {
        SaveGameData source = CreateSaveGame();
        source.Version = 99;
        SaveGameSerializer serializer = new();

        SaveGameVersionException? exception = Assert.Throws<SaveGameVersionException>(
            () => serializer.Deserialize(serializer.Serialize(source)));

        Assert.That(exception!.ActualVersion, Is.EqualTo(99));
        Assert.That(exception.SupportedVersion, Is.EqualTo(SaveGameData.CurrentVersion));
    }

    [Test]
    public void JsonExcludesTransientEncounterState()
    {
        string json = new SaveGameSerializer().Serialize(CreateSaveGame());

        Assert.That(json, Does.Not.Contain("DetectionProgress"));
        Assert.That(json, Does.Not.Contain("Awareness"));
        Assert.That(json, Does.Not.Contain("LastKnown"));
        Assert.That(json, Does.Not.Contain("IsAiming"));
        Assert.That(json, Does.Not.Contain("IsReloading"));
        Assert.That(json, Does.Not.Contain("EnvironmentalPressure"));
    }

    [Test]
    public void LocalStorageAtomicallyReplacesSlotAndRemovesTemporaryFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "omerta-godot-save-test-" + Guid.NewGuid());
        try
        {
            LocalSaveGameStorage storage = new(directory);
            storage.Write("first");
            storage.Write("second");

            Assert.That(storage.Read(), Is.EqualTo("second"));
            Assert.That(File.Exists(storage.TemporaryFilePath), Is.False);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Test]
    public void EmptySaveIsRejectedBeforeCreatingSlot()
    {
        string directory = Path.Combine(Path.GetTempPath(), "omerta-godot-empty-save-" + Guid.NewGuid());
        try
        {
            LocalSaveGameStorage storage = new(directory);

            Assert.Throws<InvalidDataException>(() => storage.Write("  "));
            Assert.That(storage.Exists, Is.False);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Test]
    public void WeaponRestoreCancelsTransientReloadAndRestoresAmmunition()
    {
        WeaponRuntime weapon = new(
            new WeaponSpec("weapon.prototype-pistol", "Pistola", 25, 0.35, 30, 7, 1.4, 3.5f, 0.65f),
            28);
        weapon.TryFire(0.0);
        weapon.TryStartReload(0.35);

        weapon.Restore(3, 21);

        Assert.That(weapon.AmmoInMagazine, Is.EqualTo(3));
        Assert.That(weapon.ReserveAmmo, Is.EqualTo(21));
        Assert.That(weapon.IsReloading, Is.False);
    }

    private static SaveGameData CreateSaveGame()
    {
        SaveGameData data = new()
        {
            Economy = new EconomySaveData { Balance = 350 },
            PlayerPose = new PlayerPoseSaveData
            {
                Position = new Vector3SaveData { X = 48.5f, Y = 0.25f, Z = -2.0f },
                Rotation = new QuaternionSaveData { W = 1.0f }
            }
        };
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = "business.el-faro",
            OwnerFactionId = "faction.player-family",
            IsCriticalAssetOperational = false
        });
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.player",
            CurrentHealth = 75
        });
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.rival-enforcer",
            CurrentHealth = 0
        });
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.rafael-merchant",
            CurrentHealth = 50
        });
        data.PlayerWeapons.Weapons.Add(new WeaponSaveData
        {
            WeaponId = "weapon.prototype-pistol",
            MagazineAmmo = 3,
            ReserveAmmo = 21
        });
        return data;
    }
}
