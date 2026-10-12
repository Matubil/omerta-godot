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
        Assert.That(restored.PlayerWeapons.EquippedWeaponId, Is.EqualTo("weapon.prototype-shotgun"));
        Assert.That(restored.PlayerWeapons.IsHolstered, Is.True);
        Assert.That(restored.PlayerWeapons.Weapons[1].LocationId, Is.EqualTo(WeaponSlotIds.LongGun));
        Assert.That(restored.Missions[0].MissionId, Is.EqualTo("mission.el-faro-takeover"));
        Assert.That(restored.Missions[0].Status, Is.EqualTo("TakeControl"));
        Assert.That(restored.Businesses[1].BusinessId, Is.EqualTo("business.la-niebla-bar"));
        Assert.That(restored.Businesses[2].BusinessId, Is.EqualTo("business.san-telmo-depot"));
        Assert.That(restored.CharacterCombatStates[3].CurrentHealth, Is.EqualTo(60));
        Assert.That(restored.Missions[1].MissionId, Is.EqualTo("mission.sabotage.la-niebla"));
        Assert.That(restored.Missions[2].MissionId, Is.EqualTo("mission.defense.el-faro"));
        Assert.That(restored.Missions[2].Status, Is.EqualTo("Succeeded"));
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
    public void VersionOneSaveMigratesWithNoInventedMissionProgress()
    {
        SaveGameSerializer serializer = new();
        string oldJson = serializer.Serialize(CreateSaveGame())
            .Replace("\"version\": 6", "\"version\": 1");

        SaveGameData migrated = serializer.Deserialize(oldJson);

        Assert.That(migrated.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(migrated.Missions, Is.Empty);
        Assert.That(migrated.Economy.Balance, Is.EqualTo(350));
        Assert.That(migrated.PlayerWeapons.EquippedWeaponId, Is.EqualTo("weapon.prototype-pistol"));
        Assert.That(migrated.PlayerWeapons.Weapons[1].LocationId, Is.EqualTo(WeaponSlotIds.FamilyVehicleTrunk));
    }

    [Test]
    public void VersionTwoSaveMigratesWithoutInventingSecondBusinessState()
    {
        SaveGameSerializer serializer = new();
        SaveGameData legacy = CreateSaveGame();
        legacy.Businesses.RemoveRange(1, 2);
        legacy.CharacterCombatStates.RemoveAt(3);
        legacy.Missions.RemoveRange(1, 2);
        string oldJson = serializer.Serialize(legacy)
            .Replace("\"version\": 6", "\"version\": 2");

        SaveGameData migrated = serializer.Deserialize(oldJson);

        Assert.That(migrated.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(migrated.Businesses, Has.Count.EqualTo(1));
        Assert.That(migrated.Missions, Has.Count.EqualTo(1));
    }

    [Test]
    public void VersionThreeSaveMigratesWithoutInventingSanTelmoState()
    {
        SaveGameSerializer serializer = new();
        SaveGameData legacy = CreateSaveGame();
        legacy.Businesses.RemoveAt(2);
        legacy.CharacterCombatStates.RemoveAt(3);
        legacy.Missions.RemoveAt(2);
        string oldJson = serializer.Serialize(legacy)
            .Replace("\"version\": 6", "\"version\": 3");

        SaveGameData migrated = serializer.Deserialize(oldJson);

        Assert.That(migrated.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(migrated.Businesses, Has.Count.EqualTo(2));
        Assert.That(migrated.CharacterCombatStates, Has.Count.EqualTo(3));
        Assert.That(migrated.Missions, Has.Count.EqualTo(2));
    }

    [Test]
    public void VersionFourSaveMigratesWithoutInventingDefenseProgress()
    {
        SaveGameSerializer serializer = new();
        SaveGameData legacy = CreateSaveGame();
        legacy.Missions.RemoveAt(2);
        string oldJson = serializer.Serialize(legacy)
            .Replace("\"version\": 6", "\"version\": 4");

        SaveGameData migrated = serializer.Deserialize(oldJson);

        Assert.That(migrated.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(migrated.Missions, Has.Count.EqualTo(2));
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
        Assert.That(json, Does.Not.Contain("PoliceResponse"));
        Assert.That(json, Does.Not.Contain("Companion"));
        Assert.That(json, Does.Not.Contain("Counterattack"));
        Assert.That(json, Does.Not.Contain("Territory"));
    }

    [Test]
    public void VersionFiveSaveMigratesShotgunIntoFamilyVehicleTrunk()
    {
        SaveGameSerializer serializer = new();
        SaveGameData legacy = CreateSaveGame();
        legacy.PlayerWeapons.Weapons.RemoveAt(1);
        legacy.PlayerWeapons.EquippedWeaponId = string.Empty;
        legacy.PlayerWeapons.IsHolstered = false;
        string oldJson = serializer.Serialize(legacy)
            .Replace("\"version\": 6", "\"version\": 5");

        SaveGameData migrated = serializer.Deserialize(oldJson);

        Assert.That(migrated.Version, Is.EqualTo(SaveGameData.CurrentVersion));
        Assert.That(migrated.PlayerWeapons.Weapons, Has.Count.EqualTo(2));
        Assert.That(migrated.PlayerWeapons.Weapons[1].WeaponId, Is.EqualTo("weapon.prototype-shotgun"));
        Assert.That(migrated.PlayerWeapons.Weapons[1].LocationId, Is.EqualTo(WeaponSlotIds.FamilyVehicleTrunk));
        Assert.That(migrated.PlayerWeapons.EquippedWeaponId, Is.EqualTo("weapon.prototype-pistol"));
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
            PlayerWeapons = new PlayerWeaponsSaveData
            {
                EquippedWeaponId = "weapon.prototype-shotgun",
                IsHolstered = true
            },
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
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = "business.la-niebla-bar",
            OwnerFactionId = "faction.circle-ferro",
            IsCriticalAssetOperational = false
        });
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = "business.san-telmo-depot",
            OwnerFactionId = "faction.circle-ferro",
            IsCriticalAssetOperational = true
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
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.merchant.san-telmo",
            CurrentHealth = 60
        });
        data.PlayerWeapons.Weapons.Add(new WeaponSaveData
        {
            WeaponId = "weapon.prototype-pistol",
            LocationId = WeaponSlotIds.Sidearm,
            MagazineAmmo = 3,
            ReserveAmmo = 21
        });
        data.PlayerWeapons.Weapons.Add(new WeaponSaveData
        {
            WeaponId = "weapon.prototype-shotgun",
            LocationId = WeaponSlotIds.LongGun,
            MagazineAmmo = 4,
            ReserveAmmo = 20
        });
        data.Missions.Add(new MissionProgressSaveData
        {
            MissionId = "mission.el-faro-takeover",
            Status = "TakeControl",
            RewardGranted = false
        });
        data.Missions.Add(new MissionProgressSaveData
        {
            MissionId = "mission.sabotage.la-niebla",
            Status = "Completed",
            RewardGranted = true
        });
        data.Missions.Add(new MissionProgressSaveData
        {
            MissionId = "mission.defense.el-faro",
            Status = "Succeeded",
            RewardGranted = true
        });
        return data;
    }
}
