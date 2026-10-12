using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.Domain.Missions;
using Omerta.Domain.Persistence;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.StageFive;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Missions;

public sealed partial class SabotageMissionController : Node3D
{
    public const string MissionId = "mission.sabotage.la-niebla";

    [Export]
    public int MissionReward { get; set; } = 100;

    [Export]
    public float ArrivalRadius { get; set; } = 7.0f;

    private BusinessSabotageMission? _mission;
    private StageFiveController? _prerequisite;
    private BarLaNieblaController? _bar;
    private EconomyController? _economy;
    private Node3D? _player;
    private Combatant? _playerCombatant;
    private HudController? _hud;

    public BusinessSabotageMissionStatus MissionStatus =>
        _mission?.Status ?? BusinessSabotageMissionStatus.Locked;

    public bool CanStartMission => _mission?.CanStart == true &&
        _bar?.Business.OwnerId != WorkshopController.PlayerFactionId &&
        _bar?.Operations.IsCriticalAssetOperational == true;

    public string MissionInteractionPrompt => MissionStatus == BusinessSabotageMissionStatus.Failed
        ? "Reintentar sabotaje: Bar La Niebla"
        : "Aceptar encargo: sabotear Bar La Niebla";

    public override void _Ready()
    {
        AddToGroup("sabotage_mission");
        _prerequisite = GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        _bar = GetTree().GetFirstNodeInGroup("la_niebla") as BarLaNieblaController;
        _economy = GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (_prerequisite is null || _bar is null || _economy is null || _player is null)
        {
            GD.PushError("The sabotage mission requires El Faro, La Niebla, economy and player nodes.");
            SetProcess(false);
            return;
        }

        _playerCombatant = _player.GetNode<Combatant>("Combatant");
        _mission = new BusinessSabotageMission(_economy.Economy, MissionReward);
        _mission.Changed += OnMissionChanged;
        _bar.Operations.Changed += OnOperationsChanged;
        _bar.Business.OwnerChanged += OnOwnerChanged;
        _playerCombatant.Defeated += OnPlayerDefeated;
        EvaluateUnlock();
        RefreshHud();
        GD.Print("Misión encadenada activa: sabotaje de Bar La Niebla.");
    }

    public override void _ExitTree()
    {
        if (_mission is not null)
        {
            _mission.Changed -= OnMissionChanged;
        }

        if (_bar?.Operations is not null)
        {
            _bar.Operations.Changed -= OnOperationsChanged;
            _bar.Business.OwnerChanged -= OnOwnerChanged;
        }

        if (_playerCombatant is not null)
        {
            _playerCombatant.Defeated -= OnPlayerDefeated;
        }
    }

    public override void _Process(double delta)
    {
        EvaluateUnlock();
        if (_mission?.Status == BusinessSabotageMissionStatus.ReachProperty &&
            _player is not null && _bar is not null)
        {
            Vector3 offset = _player.GlobalPosition - _bar.ExteriorEntrancePosition;
            offset.Y = 0.0f;
            if (offset.LengthSquared() <= ArrivalRadius * ArrivalRadius)
            {
                _mission.MarkPropertyReached();
            }
        }

        RefreshHud();
    }

    public void StartMission()
    {
        ResolveHud();
        if (!CanStartMission || _mission?.Start() != true)
        {
            return;
        }

        _hud?.ShowStatus("Informante: «La Niebla funciona gracias al equipo del fondo.»", 5.0f);
    }

    public MissionProgressSaveData CreateMissionSaveData()
    {
        if (_mission is null)
        {
            throw new InvalidOperationException("The sabotage mission has not been initialized.");
        }

        return new MissionProgressSaveData
        {
            MissionId = MissionId,
            Status = _mission.Status.ToString(),
            RewardGranted = _mission.RewardGranted
        };
    }

    public MissionProgressSaveData? ValidateMissionProgress(
        IReadOnlyList<MissionProgressSaveData> missions,
        bool targetOperational)
    {
        MissionProgressSaveData? match = null;
        foreach (MissionProgressSaveData savedMission in missions)
        {
            if (savedMission.MissionId != MissionId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"Mission '{MissionId}' is duplicated in the save.");
            }

            match = savedMission;
        }

        if (match is null)
        {
            return null;
        }

        BusinessSabotageMissionStatus status = ParseMissionStatus(match.Status);
        if ((status == BusinessSabotageMissionStatus.Completed) != match.RewardGranted)
        {
            throw new InvalidDataException("Sabotage mission completion and reward are inconsistent.");
        }

        if (status == BusinessSabotageMissionStatus.Completed && targetOperational)
        {
            throw new InvalidDataException("A completed sabotage mission requires a sabotaged target.");
        }

        if (status is BusinessSabotageMissionStatus.ReachProperty or
            BusinessSabotageMissionStatus.SabotageTarget && !targetOperational)
        {
            throw new InvalidDataException("An active sabotage mission cannot target an already sabotaged asset.");
        }

        return match;
    }

    public void RestoreAfterLoad(MissionProgressSaveData? savedMission)
    {
        if (_mission is null || _bar is null || _prerequisite is null)
        {
            return;
        }

        if (savedMission is null)
        {
            _mission.SynchronizeAfterLoad(
                _prerequisite.MissionStatus == PropertyTakeoverMissionStatus.Completed,
                !_bar.Operations.IsCriticalAssetOperational);
        }
        else
        {
            _mission.RestoreProgress(
                ParseMissionStatus(savedMission.Status),
                savedMission.RewardGranted);
        }

        RefreshHud();
    }

    public void PrepareForLoad() => _mission?.ResetForLoad();

    public void ResetEncounter()
    {
        _mission?.ResetForLoad();
        EvaluateUnlock();
        RefreshHud();
    }

    public bool TryGetMissionObjective(out Vector3 position, out string label)
    {
        position = Vector3.Zero;
        label = string.Empty;
        if (_mission is null || _bar is null)
        {
            return false;
        }

        switch (_mission.Status)
        {
            case BusinessSabotageMissionStatus.Available:
            case BusinessSabotageMissionStatus.Failed:
                position = GetNode<Node3D>("Informant").GlobalPosition;
                label = "INFORMANTE";
                return true;
            case BusinessSabotageMissionStatus.ReachProperty:
                position = _bar.ExteriorEntrancePosition;
                label = "BAR LA NIEBLA";
                return true;
            case BusinessSabotageMissionStatus.SabotageTarget:
                position = _bar.OperationsPosition;
                label = "PUNTO OPERATIVO";
                return true;
            default:
                return false;
        }
    }

    private void EvaluateUnlock()
    {
        if (_mission?.Status != BusinessSabotageMissionStatus.Locked ||
            _prerequisite?.MissionStatus != PropertyTakeoverMissionStatus.Completed ||
            _bar is null)
        {
            return;
        }

        if (_bar.Operations.IsCriticalAssetOperational)
        {
            _mission.Unlock();
        }
        else
        {
            _mission.SynchronizeAfterLoad(true, true);
        }
    }

    private void OnOperationsChanged()
    {
        if (_bar?.Operations.IsCriticalAssetOperational == false && _mission?.IsActive == true)
        {
            _mission.MarkSabotaged();
        }
    }

    private void OnOwnerChanged(string previous, string current)
    {
        if (current == WorkshopController.PlayerFactionId && _mission?.IsActive == true)
        {
            _mission.Fail();
        }
    }

    private void OnPlayerDefeated(DamageInfo damage) => _mission?.Fail();

    private void OnMissionChanged()
    {
        if (_mission?.Status == BusinessSabotageMissionStatus.Available)
        {
            _hud?.ShowStatus("Nuevo encargo disponible con el informante.", 5.0f);
        }
        else if (_mission?.Status == BusinessSabotageMissionStatus.Completed)
        {
            _hud?.ShowStatus(
                $"Sabotaje completado. Recompensa: ${MissionReward}. La propiedad sigue siendo rival.",
                6.0f);
        }
        else if (_mission?.Status == BusinessSabotageMissionStatus.Failed)
        {
            _hud?.ShowStatus("Sabotaje fallido. Podés volver con el informante.", 5.0f);
        }

        RefreshHud();
    }

    private void RefreshHud()
    {
        ResolveHud();
        if (_mission is null || _mission.Status == BusinessSabotageMissionStatus.Locked ||
            _prerequisite?.HasActiveDefenseMission == true)
        {
            return;
        }

        string objective = _mission.Status switch
        {
            BusinessSabotageMissionStatus.Available => "Hablá con el informante",
            BusinessSabotageMissionStatus.ReachProperty => "Llegá a Bar La Niebla",
            BusinessSabotageMissionStatus.SabotageTarget =>
                "Saboteá el punto operativo · NO transfiere propiedad",
            BusinessSabotageMissionStatus.Completed => "Bar La Niebla quedó fuera de servicio",
            BusinessSabotageMissionStatus.Failed => "Volvé con el informante para reintentar",
            _ => string.Empty
        };
        _hud?.SetMission(objective, _mission.Status.ToString());
    }

    private void ResolveHud() =>
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;

    private static BusinessSabotageMissionStatus ParseMissionStatus(string value)
    {
        if (!Enum.TryParse(value, false, out BusinessSabotageMissionStatus status) ||
            !Enum.IsDefined(status))
        {
            throw new InvalidDataException($"Unknown sabotage mission status '{value}'.");
        }

        return status;
    }
}
