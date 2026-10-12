using System;
using System.IO;
using Godot;
using Omerta.Domain.Missions;
using Omerta.Domain.Persistence;
using Omerta.GodotAdapters.AI;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.Input;
using Omerta.GodotAdapters.Missions;
using Omerta.GodotAdapters.StageFive;
using Omerta.Infrastructure.Persistence;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Persistence;

public sealed partial class PersistenceController : Node
{
    [Export]
    public string SlotFileName { get; set; } = LocalSaveGameStorage.DefaultSlotFileName;

    private readonly SaveGameSerializer _serializer = new();
    private LocalSaveGameStorage? _storage;
    private SaveGameData? _initialState;
    private PlayerInputAdapter? _input;
    private PlayerController? _player;
    private PlayerCombatController? _playerCombat;
    private Combatant? _playerCombatant;
    private HitscanWeaponController? _playerWeapon;
    private RivalController? _rival;
    private Combatant? _rivalCombatant;
    private WorkshopController? _workshop;
    private BarLaNieblaController? _bar;
    private DepotSanTelmoController? _depot;
    private EconomyController? _economy;
    private HudController? _hud;
    private StageFiveController? _stageFive;
    private SabotageMissionController? _sabotageMission;
    private bool _recoveryScheduled;
    private bool _runSmokeTest;

    public bool HasSave => _storage?.Exists == true;

    public string SaveFilePath => _storage?.FilePath ?? string.Empty;

    public override void _Ready()
    {
        AddToGroup("persistence");
        _input = GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _rival = GetTree().GetFirstNodeInGroup("rival") as RivalController;
        _workshop = GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _bar = GetTree().GetFirstNodeInGroup("la_niebla") as BarLaNieblaController;
        _depot = GetTree().GetFirstNodeInGroup("san_telmo") as DepotSanTelmoController;
        _economy = GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _stageFive = GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        _sabotageMission = GetTree().GetFirstNodeInGroup("sabotage_mission") as SabotageMissionController;
        if (_player is null || _rival is null || _workshop is null || _bar is null ||
            _depot is null ||
            _economy is null || _stageFive is null || _sabotageMission is null)
        {
            GD.PushError(
                "Persistence requires player, rival, all three businesses, economy and mission nodes.");
            SetProcess(false);
            return;
        }

        _playerCombat = _player.GetNode<PlayerCombatController>("PlayerCombatController");
        _playerCombatant = _player.GetNode<Combatant>("Combatant");
        _playerWeapon = _player.GetNode<HitscanWeaponController>("WeaponController");
        _rivalCombatant = _rival.GetNode<Combatant>("Combatant");
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--persistence-smoke")
            {
                _runSmokeTest = true;
                SlotFileName = "save_persistence_smoke.json";
                break;
            }
        }

        string userDirectory = ProjectSettings.GlobalizePath("user://");
        _storage = new LocalSaveGameStorage(userDirectory, SlotFileName);
        _initialState = CaptureCurrentState();
        GD.Print($"Persistencia lista: {SaveFilePath}");
        if (_runSmokeTest)
        {
            Callable.From(RunPersistenceSmokeTest).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        if (_input?.SavePressed == true)
        {
            SaveNow();
        }

        if (_input?.LoadPressed == true)
        {
            LoadNow();
        }
    }

    public SaveGameData CaptureCurrentState()
    {
        EnsureReady();
        Quaternion rotation = _player!.GlobalBasis.GetRotationQuaternion();
        SaveGameData data = new()
        {
            Economy = new EconomySaveData { Balance = _economy!.Economy.Balance },
            PlayerPose = new PlayerPoseSaveData
            {
                Position = new Vector3SaveData
                {
                    X = _player.GlobalPosition.X,
                    Y = _player.GlobalPosition.Y,
                    Z = _player.GlobalPosition.Z
                },
                Rotation = new QuaternionSaveData
                {
                    X = rotation.X,
                    Y = rotation.Y,
                    Z = rotation.Z,
                    W = rotation.W
                }
            }
        };
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = _workshop!.Business.Id,
            OwnerFactionId = _workshop.Business.OwnerId,
            IsCriticalAssetOperational = _workshop.Operations.IsCriticalAssetOperational
        });
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = _bar!.Business.Id,
            OwnerFactionId = _bar.Business.OwnerId,
            IsCriticalAssetOperational = _bar.Operations.IsCriticalAssetOperational
        });
        data.Businesses.Add(new BusinessSaveData
        {
            BusinessId = _depot!.Business.Id,
            OwnerFactionId = _depot.Business.OwnerId,
            IsCriticalAssetOperational = _depot.Operations.IsCriticalAssetOperational
        });
        data.CharacterCombatStates.Add(CreateCombatState(_playerCombatant!));
        data.CharacterCombatStates.Add(CreateCombatState(_rivalCombatant!));
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.rafael-merchant",
            CurrentHealth = _workshop.MerchantHealth
        });
        data.CharacterCombatStates.Add(new CharacterCombatSaveData
        {
            CharacterId = "character.merchant.san-telmo",
            CurrentHealth = _depot.MerchantHealth
        });
        data.PlayerWeapons.Weapons.Add(new WeaponSaveData
        {
            WeaponId = _playerWeapon!.Runtime.Specification.Id,
            MagazineAmmo = _playerWeapon.Runtime.AmmoInMagazine,
            ReserveAmmo = _playerWeapon.Runtime.ReserveAmmo
        });
        data.Missions.Add(_stageFive!.CreateMissionSaveData());
        data.Missions.Add(_sabotageMission!.CreateMissionSaveData());
        data.Missions.Add(_stageFive.CreateDefenseMissionSaveData());
        return data;
    }

    public void RestoreFromData(SaveGameData data)
    {
        EnsureReady();
        ValidatedRestore restore = Validate(data);
        _stageFive!.PrepareForLoad();
        _sabotageMission!.PrepareForLoad();
        _player!.RestorePersistentPose(restore.PlayerPosition, restore.PlayerRotation);
        _economy!.RestoreBalance(data.Economy.Balance);
        _rival!.RestorePersistentState(restore.RivalHealth);
        _workshop!.RestorePersistentState(
            restore.Business.OwnerFactionId,
            restore.Business.IsCriticalAssetOperational,
            restore.MerchantHealth,
            restore.RivalHealth == 0);
        _bar!.RestorePersistentState(
            restore.BarBusiness?.OwnerFactionId ?? _bar.Definition.InitialOwnerId,
            restore.BarBusiness?.IsCriticalAssetOperational ?? true);
        _depot!.RestorePersistentState(
            restore.DepotBusiness?.OwnerFactionId ?? _depot.Definition.InitialOwnerId,
            restore.DepotBusiness?.IsCriticalAssetOperational ?? true,
            restore.DepotMerchantHealth);
        _playerCombat!.RestoreCombat(
            restore.PlayerHealth,
            restore.Weapon.MagazineAmmo,
            restore.Weapon.ReserveAmmo);
        _stageFive.RestoreAfterLoad(restore.Mission, restore.DefenseMission);
        _sabotageMission!.RestoreAfterLoad(restore.SabotageMission);
    }

    public bool SaveNow()
    {
        try
        {
            EnsureReady();
            SaveGameData data = CaptureCurrentState();
            string json = _serializer.Serialize(data);
            _storage!.Write(json);
            _serializer.Deserialize(_storage.Read());
            _hud?.ShowStatus(
                $"Partida guardada · Pistola {data.PlayerWeapons.Weapons[0].MagazineAmmo}/" +
                $"{data.PlayerWeapons.Weapons[0].ReserveAmmo}",
                4.0f);
            GD.Print($"Partida guardada ({_storage.FileSize} bytes): {_storage.FilePath}");
            return true;
        }
        catch (Exception exception)
        {
            ShowFailure("No se pudo guardar", exception);
            return false;
        }
    }

    public bool LoadNow()
    {
        try
        {
            EnsureReady();
            if (!_storage!.Exists)
            {
                throw new FileNotFoundException("No manual save exists for slot 0.", _storage.FilePath);
            }

            SaveGameData data = _serializer.Deserialize(_storage.Read());
            RestoreFromData(data);
            _hud?.ShowStatus(
                $"Partida cargada · Pistola {data.PlayerWeapons.Weapons[0].MagazineAmmo}/" +
                $"{data.PlayerWeapons.Weapons[0].ReserveAmmo}",
                4.0f);
            GD.Print($"Partida cargada: {_storage.FilePath}");
            return true;
        }
        catch (Exception exception)
        {
            ShowFailure("No se pudo cargar", exception);
            return false;
        }
    }

    public void ScheduleRecoveryAfterDefeat()
    {
        if (_recoveryScheduled)
        {
            return;
        }

        _recoveryScheduled = true;
        Callable.From(RecoverAfterDefeat).CallDeferred();
    }

    private void RecoverAfterDefeat()
    {
        _recoveryScheduled = false;
        if (HasSave && LoadNow())
        {
            _hud?.ShowStatus("Derrota · último guardado cargado", 5.0f);
            return;
        }

        if (_initialState is not null)
        {
            RestoreFromData(_initialState);
            _hud?.ShowStatus("Derrota · estado inicial restaurado", 5.0f);
        }
    }

    private void RunPersistenceSmokeTest()
    {
        int exitCode = 1;
        try
        {
            SaveGameData expected = CaptureCurrentState();
            if (!SaveNow())
            {
                throw new InvalidOperationException("The smoke save operation failed.");
            }

            _economy!.Economy.AddMoney(17);
            _player!.GlobalPosition += new Vector3(3.0f, 0.0f, 3.0f);
            _playerCombat!.RestoreCombat(50, 1, 2);
            _rival!.RestorePersistentState(0);
            _workshop!.RestorePersistentState(
                WorkshopController.PlayerFactionId,
                false,
                50,
                true);
            _bar!.RestorePersistentState(
                _bar.Definition.InitialOwnerId,
                false);
            _depot!.RestorePersistentState(
                WorkshopController.PlayerFactionId,
                false,
                30);
            if (!LoadNow())
            {
                throw new InvalidOperationException("The smoke load operation failed.");
            }

            string expectedJson = _serializer.Serialize(expected);
            string actualJson = _serializer.Serialize(CaptureCurrentState());
            if (expectedJson != actualJson)
            {
                throw new InvalidOperationException(
                    "The restored runtime state differs from the saved state.");
            }

            _stageFive!.RestoreAfterLoad(
                new MissionProgressSaveData
                {
                    MissionId = StageFiveController.ElFaroMissionId,
                    Status = PropertyTakeoverMissionStatus.Completed.ToString(),
                    RewardGranted = true
                },
                new MissionProgressSaveData
                {
                    MissionId = StageFiveController.DefenseMissionId,
                    Status = BusinessDefenseMissionStatus.Succeeded.ToString(),
                    RewardGranted = true
                });
            _bar!.RestorePersistentState(_bar.Definition.InitialOwnerId, true);
            _sabotageMission!.RestoreAfterLoad(new MissionProgressSaveData
            {
                MissionId = SabotageMissionController.MissionId,
                Status = BusinessSabotageMissionStatus.SabotageTarget.ToString(),
                RewardGranted = false
            });
            _bar.InteractWithOperations();
            if (_sabotageMission.MissionStatus !=
                BusinessSabotageMissionStatus.Completed)
            {
                throw new InvalidOperationException("The integrated sabotage setup did not complete.");
            }

            SaveGameData completedSabotage = CaptureCurrentState();
            if (!SaveNow())
            {
                throw new InvalidOperationException("The completed sabotage save operation failed.");
            }

            _sabotageMission.PrepareForLoad();
            _bar.RestorePersistentState(WorkshopController.PlayerFactionId, true);
            _economy.Economy.AddMoney(31);
            if (!LoadNow())
            {
                throw new InvalidOperationException("The completed sabotage load operation failed.");
            }

            string completedExpectedJson = _serializer.Serialize(completedSabotage);
            string completedActualJson = _serializer.Serialize(CaptureCurrentState());
            if (completedExpectedJson != completedActualJson)
            {
                throw new InvalidOperationException(
                    "The restored completed sabotage differs from the saved state.");
            }

            GD.Print(
                "Persistence smoke passed: three businesses, merchant health and three " +
                "campaign missions save/mutation/exact restore verified.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Persistence smoke failed: {exception}");
        }
        finally
        {
            if (_storage is not null)
            {
                if (File.Exists(_storage.FilePath))
                {
                    File.Delete(_storage.FilePath);
                }

                if (File.Exists(_storage.TemporaryFilePath))
                {
                    File.Delete(_storage.TemporaryFilePath);
                }
            }

            GetTree().Quit(exitCode);
        }
    }

    private ValidatedRestore Validate(SaveGameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Version != SaveGameData.CurrentVersion)
        {
            throw new SaveGameVersionException(data.Version, SaveGameData.CurrentVersion);
        }

        if (data.Economy.Balance < 0)
        {
            throw new InvalidDataException("Saved balance cannot be negative.");
        }

        BusinessSaveData business = FindBusiness(data, _workshop!.Business.Id);
        if (business.OwnerFactionId != WorkshopController.PlayerFactionId &&
            business.OwnerFactionId != _workshop.Definition.InitialOwnerId)
        {
            throw new InvalidDataException($"Unknown business owner '{business.OwnerFactionId}'.");
        }

        BusinessSaveData? barBusiness = FindOptionalBusiness(data, _bar!.Business.Id);
        if (barBusiness is not null &&
            barBusiness.OwnerFactionId != WorkshopController.PlayerFactionId &&
            barBusiness.OwnerFactionId != _bar.Definition.InitialOwnerId)
        {
            throw new InvalidDataException(
                $"Unknown La Niebla owner '{barBusiness.OwnerFactionId}'.");
        }

        BusinessSaveData? depotBusiness = FindOptionalBusiness(data, _depot!.Business.Id);
        if (depotBusiness is not null &&
            depotBusiness.OwnerFactionId != WorkshopController.PlayerFactionId &&
            depotBusiness.OwnerFactionId != _depot.Definition.InitialOwnerId)
        {
            throw new InvalidDataException(
                $"Unknown San Telmo owner '{depotBusiness.OwnerFactionId}'.");
        }

        int playerHealth = FindHealth(data, _playerCombatant!);
        int rivalHealth = FindHealth(data, _rivalCombatant!);
        Combatant merchant = _workshop.GetNode<Combatant>("Interior/Merchant/Combatant");
        int merchantHealth = FindHealth(data, merchant);
        Combatant depotMerchant = _depot.GetNode<Combatant>("Interior/Merchant/Combatant");
        int depotMerchantHealth = FindOptionalHealth(data, depotMerchant);
        WeaponSaveData weapon = FindWeapon(data, _playerWeapon!.Runtime.Specification.Id);
        MissionProgressSaveData? mission = _stageFive!.ValidateMissionProgress(data.Missions);
        MissionProgressSaveData? defenseMission = _stageFive.ValidateDefenseMissionProgress(
            data.Missions,
            business.IsCriticalAssetOperational);
        MissionProgressSaveData? sabotageMission = _sabotageMission!.ValidateMissionProgress(
            data.Missions,
            barBusiness?.IsCriticalAssetOperational ?? true);
        if (weapon.MagazineAmmo < 0 ||
            weapon.MagazineAmmo > _playerWeapon.Runtime.Specification.MagazineCapacity ||
            weapon.ReserveAmmo < 0)
        {
            throw new InvalidDataException("Saved ammunition is outside the valid range.");
        }

        Vector3 position = new(
            data.PlayerPose.Position.X,
            data.PlayerPose.Position.Y,
            data.PlayerPose.Position.Z);
        Quaternion rotation = new(
            data.PlayerPose.Rotation.X,
            data.PlayerPose.Rotation.Y,
            data.PlayerPose.Rotation.Z,
            data.PlayerPose.Rotation.W);
        if (!position.IsFinite() || !rotation.IsFinite() || rotation.LengthSquared() < 0.0001f)
        {
            throw new InvalidDataException("Saved player pose is invalid.");
        }

        return new ValidatedRestore(
            business,
            barBusiness,
            depotBusiness,
            playerHealth,
            rivalHealth,
            merchantHealth,
            depotMerchantHealth,
            weapon,
            mission,
            defenseMission,
            sabotageMission,
            position,
            rotation.Normalized());
    }

    private static CharacterCombatSaveData CreateCombatState(Combatant combatant) => new()
    {
        CharacterId = combatant.CombatantId,
        CurrentHealth = combatant.Health.Current
    };

    private static BusinessSaveData FindBusiness(SaveGameData data, string businessId)
    {
        BusinessSaveData? match = null;
        foreach (BusinessSaveData business in data.Businesses)
        {
            if (business.BusinessId != businessId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"Business '{businessId}' is duplicated in the save.");
            }

            match = business;
        }

        return match ?? throw new InvalidDataException($"Business '{businessId}' is missing from the save.");
    }

    private static BusinessSaveData? FindOptionalBusiness(SaveGameData data, string businessId)
    {
        BusinessSaveData? match = null;
        foreach (BusinessSaveData business in data.Businesses)
        {
            if (business.BusinessId != businessId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"Business '{businessId}' is duplicated in the save.");
            }

            match = business;
        }

        return match;
    }

    private static int FindHealth(SaveGameData data, Combatant combatant)
    {
        CharacterCombatSaveData? match = null;
        foreach (CharacterCombatSaveData state in data.CharacterCombatStates)
        {
            if (state.CharacterId != combatant.CombatantId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException(
                    $"Character '{combatant.CombatantId}' is duplicated in the save.");
            }

            match = state;
        }

        if (match is null)
        {
            throw new InvalidDataException($"Character '{combatant.CombatantId}' is missing from the save.");
        }

        if (match.CurrentHealth < 0 || match.CurrentHealth > combatant.Health.Maximum)
        {
            throw new InvalidDataException(
                $"Health for '{combatant.CombatantId}' is outside the valid range.");
        }

        return match.CurrentHealth;
    }

    private static int FindOptionalHealth(SaveGameData data, Combatant combatant)
    {
        CharacterCombatSaveData? match = null;
        foreach (CharacterCombatSaveData state in data.CharacterCombatStates)
        {
            if (state.CharacterId != combatant.CombatantId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException(
                    $"Character '{combatant.CombatantId}' is duplicated in the save.");
            }

            match = state;
        }

        if (match is null)
        {
            return combatant.Health.Maximum;
        }

        if (match.CurrentHealth < 0 || match.CurrentHealth > combatant.Health.Maximum)
        {
            throw new InvalidDataException(
                $"Health for '{combatant.CombatantId}' is outside the valid range.");
        }

        return match.CurrentHealth;
    }

    private static WeaponSaveData FindWeapon(SaveGameData data, string weaponId)
    {
        WeaponSaveData? match = null;
        foreach (WeaponSaveData weapon in data.PlayerWeapons.Weapons)
        {
            if (weapon.WeaponId != weaponId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"Weapon '{weaponId}' is duplicated in the save.");
            }

            match = weapon;
        }

        return match ?? throw new InvalidDataException($"Weapon '{weaponId}' is missing from the save.");
    }

    private void EnsureReady()
    {
        if (_storage is null || _player is null || _playerCombat is null ||
            _playerCombatant is null || _playerWeapon is null || _rival is null ||
            _rivalCombatant is null || _workshop is null || _economy is null ||
            _bar is null || _depot is null || _stageFive is null || _sabotageMission is null)
        {
            throw new InvalidOperationException("Persistence has not been initialized.");
        }
    }

    private void ShowFailure(string message, Exception exception)
    {
        _hud?.ShowStatus(message, 5.0f);
        GD.PushError($"[Persistence] {message}: {exception}");
    }

    private sealed record ValidatedRestore(
        BusinessSaveData Business,
        BusinessSaveData? BarBusiness,
        BusinessSaveData? DepotBusiness,
        int PlayerHealth,
        int RivalHealth,
        int MerchantHealth,
        int DepotMerchantHealth,
        WeaponSaveData Weapon,
        MissionProgressSaveData? Mission,
        MissionProgressSaveData? DefenseMission,
        MissionProgressSaveData? SabotageMission,
        Vector3 PlayerPosition,
        Quaternion PlayerRotation);
}
