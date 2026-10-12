using System.Collections.Generic;

namespace Omerta.Domain.Persistence;

public sealed class SaveGameData
{
    public const int CurrentVersion = 3;

    public int Version { get; set; } = CurrentVersion;

    public EconomySaveData Economy { get; set; } = new();

    public List<BusinessSaveData> Businesses { get; set; } = new();

    public List<CharacterCombatSaveData> CharacterCombatStates { get; set; } = new();

    public PlayerPoseSaveData PlayerPose { get; set; } = new();

    public PlayerWeaponsSaveData PlayerWeapons { get; set; } = new();

    public List<MissionProgressSaveData> Missions { get; set; } = new();
}

public sealed class EconomySaveData
{
    public int Balance { get; set; }
}

public sealed class BusinessSaveData
{
    public string BusinessId { get; set; } = string.Empty;

    public string OwnerFactionId { get; set; } = string.Empty;

    public bool IsCriticalAssetOperational { get; set; } = true;
}

public sealed class CharacterCombatSaveData
{
    public string CharacterId { get; set; } = string.Empty;

    public int CurrentHealth { get; set; }
}

public sealed class PlayerPoseSaveData
{
    public Vector3SaveData Position { get; set; } = new();

    public QuaternionSaveData Rotation { get; set; } = new();
}

public sealed class Vector3SaveData
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

public sealed class QuaternionSaveData
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float W { get; set; } = 1.0f;
}

public sealed class PlayerWeaponsSaveData
{
    public List<WeaponSaveData> Weapons { get; set; } = new();
}

public sealed class WeaponSaveData
{
    public string WeaponId { get; set; } = string.Empty;

    public int MagazineAmmo { get; set; }

    public int ReserveAmmo { get; set; }
}

public sealed class MissionProgressSaveData
{
    public string MissionId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool RewardGranted { get; set; }
}
