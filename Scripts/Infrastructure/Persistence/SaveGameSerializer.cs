using System;
using System.IO;
using System.Text.Json;
using Omerta.Domain.Combat.Weapons;
using Omerta.Domain.Persistence;

namespace Omerta.Infrastructure.Persistence;

public sealed class SaveGameSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public string Serialize(SaveGameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return JsonSerializer.Serialize(data, Options);
    }

    public SaveGameData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("The save file is empty.");
        }

        SaveGameData data;
        try
        {
            data = JsonSerializer.Deserialize<SaveGameData>(json, Options)
                ?? throw new InvalidDataException("The save file could not be read.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The save file is not valid JSON.", exception);
        }

        int sourceVersion = data.Version;
        if (sourceVersion == 1)
        {
            data.Missions = new();
        }
        else if (sourceVersion is not (2 or 3 or 4 or 5 or SaveGameData.CurrentVersion))
        {
            throw new SaveGameVersionException(sourceVersion, SaveGameData.CurrentVersion);
        }

        data.Economy ??= new EconomySaveData();
        data.Businesses ??= new();
        data.CharacterCombatStates ??= new();
        data.PlayerPose ??= new PlayerPoseSaveData();
        data.PlayerPose.Position ??= new Vector3SaveData();
        data.PlayerPose.Rotation ??= new QuaternionSaveData();
        data.PlayerWeapons ??= new PlayerWeaponsSaveData();
        data.PlayerWeapons.Weapons ??= new();
        data.Missions ??= new();
        if (sourceVersion <= 5)
        {
            MigrateWeaponLoadout(data.PlayerWeapons);
        }

        data.Version = SaveGameData.CurrentVersion;
        return data;
    }

    private static void MigrateWeaponLoadout(PlayerWeaponsSaveData weapons)
    {
        WeaponSaveData? pistol = weapons.Weapons.Find(
            weapon => weapon.WeaponId == "weapon.prototype-pistol");
        if (pistol is not null)
        {
            pistol.LocationId = WeaponSlotIds.Sidearm;
        }

        WeaponSaveData? shotgun = weapons.Weapons.Find(
            weapon => weapon.WeaponId == "weapon.prototype-shotgun");
        if (shotgun is null)
        {
            weapons.Weapons.Add(new WeaponSaveData
            {
                WeaponId = "weapon.prototype-shotgun",
                LocationId = WeaponSlotIds.FamilyVehicleTrunk,
                MagazineAmmo = 5,
                ReserveAmmo = 20
            });
        }
        else
        {
            shotgun.LocationId = WeaponSlotIds.FamilyVehicleTrunk;
        }

        weapons.EquippedWeaponId = pistol?.WeaponId ?? string.Empty;
        weapons.IsHolstered = false;
    }
}

public sealed class SaveGameVersionException : InvalidOperationException
{
    public SaveGameVersionException(int actualVersion, int supportedVersion)
        : base($"Save version {actualVersion} is incompatible. Supported version: {supportedVersion}.")
    {
        ActualVersion = actualVersion;
        SupportedVersion = supportedVersion;
    }

    public int ActualVersion { get; }

    public int SupportedVersion { get; }
}
