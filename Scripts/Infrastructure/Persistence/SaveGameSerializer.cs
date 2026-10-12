using System;
using System.IO;
using System.Text.Json;
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

        if (data.Version == 1)
        {
            data.Missions = new();
            data.Version = SaveGameData.CurrentVersion;
        }
        else if (data.Version == 2)
        {
            data.Version = SaveGameData.CurrentVersion;
        }
        else if (data.Version != SaveGameData.CurrentVersion)
        {
            throw new SaveGameVersionException(data.Version, SaveGameData.CurrentVersion);
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
        return data;
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
